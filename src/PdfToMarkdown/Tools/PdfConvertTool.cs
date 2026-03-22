using System.ComponentModel;
using ModelContextProtocol.Server;
using PdfToMarkdown.Services;

namespace PdfToMarkdown.Tools;

/// <summary>
/// MCP tool that converts a PDF file to Markdown and writes the output alongside the source file.
/// </summary>
[McpServerToolType]
public static class PdfConvertTool
{
    /// <summary>
    /// Converts a PDF file to Markdown and writes the output alongside the source file.
    /// Returns a confirmation message listing all output file paths written.
    /// </summary>
    [McpServerTool, Description("Converts a PDF file to Markdown and writes the output alongside the source file. Returns a confirmation message listing the output file path(s) written.")]
    public static async Task<string> ConvertPdfToMarkdown(
        IConversionOrchestrator orchestrator,
        [Description("Absolute path to the PDF file on the local file system")] string pdfPath,
        [Description("When true, splits the output into separate Markdown files per chapter (one file per top-level heading). Defaults to false.")] bool chunkByChapter = false,
        CancellationToken cancellationToken = default)
    {
        return await orchestrator.ConvertAsync(pdfPath, chunkByChapter, cancellationToken).ConfigureAwait(false);
    }
}
