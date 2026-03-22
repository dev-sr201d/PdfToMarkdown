using ModelContextProtocol;

namespace PdfToMarkdown.Services;

/// <summary>
/// Validates that a PDF file path meets all preconditions for processing.
/// </summary>
internal sealed class InputValidator : IInputValidator
{
    /// <inheritdoc />
    public Task ValidateAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new McpException("No file path was provided. Please specify an absolute path to a PDF file.");
        }

        string extension = Path.GetExtension(pdfPath);
        if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new McpException($"Expected a file with .pdf extension, but received '{extension}'. Path: {pdfPath}");
        }

        if (!File.Exists(pdfPath))
        {
            throw new McpException($"PDF file not found: {pdfPath}");
        }

        try
        {
            using FileStream stream = File.OpenRead(pdfPath);
        }
        catch (UnauthorizedAccessException)
        {
            throw new McpException($"PDF file exists but cannot be read (access denied): {pdfPath}");
        }
        catch (IOException ex)
        {
            throw new McpException($"PDF file exists but cannot be read: {pdfPath} — {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
