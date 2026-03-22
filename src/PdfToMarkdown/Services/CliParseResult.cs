namespace PdfToMarkdown.Services;

/// <summary>
/// Represents the result of parsing CLI arguments.
/// </summary>
internal sealed class CliParseResult
{
    private CliParseResult()
    {
    }

    /// <summary>
    /// Gets the absolute path to the PDF file, or <see langword="null"/> if parsing failed.
    /// </summary>
    public string? PdfPath { get; private init; }

    /// <summary>
    /// Gets a value indicating whether chapter-based chunking is enabled.
    /// </summary>
    public bool ChunkByChapter { get; private init; }

    /// <summary>
    /// Gets the error message if parsing failed, or <see langword="null"/> on success.
    /// </summary>
    public string? Error { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the parse was successful.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    public static CliParseResult Success(string pdfPath, bool chunkByChapter) =>
        new() { PdfPath = pdfPath, ChunkByChapter = chunkByChapter };

    /// <summary>
    /// Creates a failed parse result with an error message.
    /// </summary>
    public static CliParseResult Failure(string error) =>
        new() { Error = error };
}
