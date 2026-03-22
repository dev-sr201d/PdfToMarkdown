using System.Globalization;
using System.Text;

namespace PdfToMarkdown.Services;

/// <summary>
/// Orchestrates the full PDF-to-Markdown conversion pipeline:
/// validate → initialize writer → convert with incremental writes → finalize → return confirmation.
/// </summary>
internal sealed class ConversionOrchestrator(
    IInputValidator validator,
    IPdfMarkdownConverter converter,
    IMarkdownWriter writer) : IConversionOrchestrator
{
    private readonly IInputValidator _validator = validator;
    private readonly IPdfMarkdownConverter _converter = converter;
    private readonly IMarkdownWriter _writer = writer;

    /// <inheritdoc />
    public async Task<string> ConvertAsync(string pdfPath, bool chunkByChapter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _validator.ValidateAsync(pdfPath, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        await using IMarkdownWriter writerInstance = _writer;

        await writerInstance.InitializeAsync(pdfPath, chunkByChapter, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        await _converter.ConvertAsync(pdfPath, writerInstance, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> writtenPaths = await writerInstance.FinalizeAsync(cancellationToken).ConfigureAwait(false);

        return FormatConfirmation(writtenPaths);
    }

    private static string FormatConfirmation(IReadOnlyList<string> paths)
    {
        if (paths.Count == 1)
        {
            return $"Converted: {paths[0]}";
        }

        StringBuilder sb = new();
        sb.Append(CultureInfo.InvariantCulture, $"Converted into {paths.Count} files:");
        sb.AppendLine();
        foreach (string path in paths)
        {
            sb.AppendLine(path);
        }

        return sb.ToString().TrimEnd();
    }
}
