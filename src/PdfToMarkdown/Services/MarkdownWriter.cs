using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace PdfToMarkdown.Services;

/// <summary>
/// Manages incremental Markdown output to disk, supporting standard mode
/// (single file) and chunked mode (chapter-based splitting at H1 boundaries).
/// </summary>
internal sealed partial class MarkdownWriter(ILogger<MarkdownWriter> logger) : IMarkdownWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly ILogger<MarkdownWriter> _logger = logger;
    private string _basePath = "";
    private string _baseNameWithoutExtension = "";
    private string _outputDirectory = "";
    private bool _chunkByChapter;
    private bool _finalized;

    // Standard mode
    private StreamWriter? _singleWriter;
    private string? _singleOutputPath;

    // Chunked mode
    private int _currentChapter;
    private StreamWriter? _currentChapterWriter;
    private readonly List<string> _chapterPaths = [];

    [LoggerMessage(Level = LogLevel.Information, Message = "Initialized Markdown writer: mode={Mode}, basePath={BasePath}")]
    private partial void LogInitialized(string mode, string basePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Started new chapter file: {ChapterPath}")]
    private partial void LogNewChapter(string chapterPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finalized writer: {FileCount} file(s) written")]
    private partial void LogFinalized(int fileCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cleaning up partial output file: {FilePath}")]
    private partial void LogPartialCleanup(string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to clean up partial output file: {FilePath}")]
    private partial void LogCleanupFailed(string filePath);

    /// <inheritdoc />
    public Task InitializeAsync(string pdfPath, bool chunkByChapter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _outputDirectory = Path.GetDirectoryName(pdfPath) ?? ".";
        _baseNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfPath);
        _basePath = pdfPath;
        _chunkByChapter = chunkByChapter;

        if (!Directory.Exists(_outputDirectory))
        {
            throw new McpException($"Output directory does not exist: {_outputDirectory}");
        }

        if (!chunkByChapter)
        {
            _singleOutputPath = Path.Combine(_outputDirectory, _baseNameWithoutExtension + ".md");
            try
            {
                _singleWriter = CreateSharedStreamWriter(_singleOutputPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new McpException($"Cannot create output file (access denied): {_singleOutputPath}", ex);
            }
            catch (IOException ex)
            {
                throw new McpException($"Cannot create output file: {_singleOutputPath} \u2014 {ex.Message}", ex);
            }
        }
        else
        {
            // In chunked mode, we start chapter 1 immediately for any pre-heading content
            _currentChapter = 1;
            StartNewChapterFile();
        }

        LogInitialized(chunkByChapter ? "chunked" : "standard", _basePath);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task WritePageAsync(string markdownContent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!_chunkByChapter)
            {
                await _singleWriter!.WriteAsync(markdownContent).ConfigureAwait(false);
                await _singleWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteChunkedContentAsync(markdownContent, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            string path = _chunkByChapter
                ? (_chapterPaths.Count > 0 ? _chapterPaths[^1] : _outputDirectory)
                : (_singleOutputPath ?? _outputDirectory);
            throw new McpException($"Cannot write to output file: {path} \u2014 {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> FinalizeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_chunkByChapter)
        {
            if (_singleWriter is not null)
            {
                await _singleWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                await _singleWriter.DisposeAsync().ConfigureAwait(false);
                _singleWriter = null;
            }

            List<string> paths = _singleOutputPath is not null
                ? [Path.GetFullPath(_singleOutputPath)]
                : [];
            _finalized = true;
            LogFinalized(paths.Count);
            return paths;
        }

        // Chunked mode
        if (_currentChapterWriter is not null)
        {
            await _currentChapterWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            await _currentChapterWriter.DisposeAsync().ConfigureAwait(false);
            _currentChapterWriter = null;
        }

        List<string> fullPaths = _chapterPaths.Select(Path.GetFullPath).ToList();
        _finalized = true;
        LogFinalized(fullPaths.Count);
        return fullPaths;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_singleWriter is not null)
        {
            await _singleWriter.DisposeAsync().ConfigureAwait(false);
            _singleWriter = null;
        }

        if (_currentChapterWriter is not null)
        {
            await _currentChapterWriter.DisposeAsync().ConfigureAwait(false);
            _currentChapterWriter = null;
        }

        if (!_finalized)
        {
            CleanupPartialFiles();
        }
    }

    /// <summary>
    /// Deletes any output files created during the current session. Best-effort — never throws.
    /// </summary>
    private void CleanupPartialFiles()
    {
        if (!_chunkByChapter)
        {
            TryDeleteFile(_singleOutputPath);
        }
        else
        {
            foreach (string path in _chapterPaths)
            {
                TryDeleteFile(path);
            }
        }
    }

    /// <summary>
    /// Attempts to delete a single file. Logs a warning on success or failure. Never throws.
    /// </summary>
    private void TryDeleteFile(string? path)
    {
        if (path is null || !File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
            LogPartialCleanup(path);
        }
        catch
        {
            LogCleanupFailed(path);
        }
    }

    /// <summary>
    /// Writes content in chunked mode, splitting at H1 boundaries.
    /// </summary>
    private async Task WriteChunkedContentAsync(string content, CancellationToken cancellationToken)
    {
        using StringReader reader = new(content);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsH1Heading(line) && _currentChapter > 1 || (IsH1Heading(line) && HasCurrentChapterContent()))
            {
                // Start a new chapter
                if (_currentChapterWriter is not null)
                {
                    await _currentChapterWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                    await _currentChapterWriter.DisposeAsync().ConfigureAwait(false);
                    _currentChapterWriter = null;
                }

                _currentChapter++;
                StartNewChapterFile();
            }

            await _currentChapterWriter!.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _currentChapterWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks if the current chapter has had any content written to it.
    /// </summary>
    private bool HasCurrentChapterContent()
    {
        if (_currentChapterWriter is null || _chapterPaths.Count == 0)
        {
            return false;
        }

        string currentPath = _chapterPaths[^1];
        return new FileInfo(currentPath).Length > 0;
    }

    /// <summary>
    /// Determines whether a line is a top-level heading (starts with "# ").
    /// </summary>
    private static bool IsH1Heading(string line)
    {
        return line.StartsWith("# ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Opens a new chapter file for writing.
    /// </summary>
    private void StartNewChapterFile()
    {
        string chapterPath = Path.Combine(_outputDirectory, $"{_baseNameWithoutExtension}_{_currentChapter}.md");
        _currentChapterWriter = CreateSharedStreamWriter(chapterPath);
        _chapterPaths.Add(chapterPath);
        LogNewChapter(chapterPath);
    }

    /// <summary>
    /// Creates a <see cref="StreamWriter"/> that allows concurrent reads from the file.
    /// </summary>
    private static StreamWriter CreateSharedStreamWriter(string path)
    {
        FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        return new StreamWriter(fs, Utf8NoBom);
    }
}
