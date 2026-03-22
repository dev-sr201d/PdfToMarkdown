namespace PdfToMarkdown.Services;

/// <summary>
/// Orchestrates the full PDF-to-Markdown conversion pipeline:
/// validate → initialize writer → convert with incremental writes → finalize → return confirmation.
/// </summary>
public interface IConversionOrchestrator
{
    /// <summary>
    /// Executes the conversion pipeline for the specified PDF file and returns
    /// a confirmation message listing all output file paths written.
    /// </summary>
    /// <param name="pdfPath">The absolute path to the PDF file to convert.</param>
    /// <param name="chunkByChapter">
    /// When <see langword="true"/>, the output is split into separate files per chapter.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the conversion operation.</param>
    /// <returns>A confirmation message listing the absolute path(s) of all files written.</returns>
    Task<string> ConvertAsync(string pdfPath, bool chunkByChapter, CancellationToken cancellationToken = default);
}
