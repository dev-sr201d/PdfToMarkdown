namespace PdfToMarkdown.Services;

/// <summary>
/// Reads a text-based PDF file, extracts its content, classifies structural
/// elements, and converts them directly to Markdown syntax during parsing.
/// Writes converted content incrementally through the provided
/// <see cref="IMarkdownWriter"/>.
/// </summary>
public interface IPdfMarkdownConverter
{
    /// <summary>
    /// Converts the specified PDF file directly to Markdown, writing output
    /// through the provided writer page-by-page.
    /// </summary>
    /// <param name="pdfPath">The absolute path to the PDF file to convert.</param>
    /// <param name="writer">The writer that receives incremental Markdown output.</param>
    /// <param name="cancellationToken">A token to cancel the conversion operation.</param>
    /// <returns>A task representing the asynchronous conversion operation.</returns>
    Task ConvertAsync(string pdfPath, IMarkdownWriter writer, CancellationToken cancellationToken = default);
}
