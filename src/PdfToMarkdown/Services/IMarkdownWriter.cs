namespace PdfToMarkdown.Services;

/// <summary>
/// Manages incremental Markdown output to disk, supporting both standard mode
/// (single file) and chunked mode (chapter-based splitting).
/// </summary>
public interface IMarkdownWriter : IAsyncDisposable
{
    /// <summary>
    /// Initializes the writer for output, preparing the target file(s).
    /// </summary>
    /// <param name="pdfPath">The absolute path of the source PDF file.</param>
    /// <param name="chunkByChapter">
    /// When <see langword="true"/>, output is split into separate files at top-level heading boundaries.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the initialization.</param>
    Task InitializeAsync(string pdfPath, bool chunkByChapter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a page of converted Markdown content to the output file(s).
    /// Flushes content to disk after each call.
    /// </summary>
    /// <param name="markdownContent">The Markdown content for a single page.</param>
    /// <param name="cancellationToken">A token to cancel the write operation.</param>
    Task WritePageAsync(string markdownContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the writer by closing all open files and returning the list
    /// of absolute file paths written.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the finalization.</param>
    /// <returns>The absolute paths of all Markdown files written.</returns>
    Task<IReadOnlyList<string>> FinalizeAsync(CancellationToken cancellationToken = default);
}
