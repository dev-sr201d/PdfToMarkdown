namespace PdfToMarkdown.Services;

/// <summary>
/// Validates input parameters before the conversion pipeline begins processing.
/// Throws a descriptive exception on validation failure.
/// </summary>
public interface IInputValidator
{
    /// <summary>
    /// Validates that the specified PDF file path meets all preconditions for processing.
    /// </summary>
    /// <param name="pdfPath">The absolute path to the PDF file to validate.</param>
    /// <param name="cancellationToken">A token to cancel the validation operation.</param>
    /// <exception cref="ModelContextProtocol.McpException">
    /// Thrown when the path is empty, the file does not exist, does not have a .pdf extension,
    /// or is not readable.
    /// </exception>
    Task ValidateAsync(string pdfPath, CancellationToken cancellationToken = default);
}
