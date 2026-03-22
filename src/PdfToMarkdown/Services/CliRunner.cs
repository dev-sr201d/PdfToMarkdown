using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace PdfToMarkdown.Services;

/// <summary>
/// Executes the CLI mode workflow: parses arguments, runs the conversion
/// pipeline, and returns an exit code.
/// </summary>
internal sealed partial class CliRunner(
    IConversionOrchestrator orchestrator,
    ILogger<CliRunner> logger)
{
    private readonly IConversionOrchestrator _orchestrator = orchestrator;
    private readonly ILogger<CliRunner> _logger = logger;

    /// <summary>
    /// Runs the CLI conversion using the provided arguments.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="stdout">The writer for standard output (file paths on success).</param>
    /// <param name="stderr">The writer for standard error (error messages on failure).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The process exit code: 0 on success, non-zero on failure.</returns>
    public async Task<int> RunAsync(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        CliParseResult parseResult = CliArgumentParser.Parse(args);

        if (!parseResult.IsSuccess)
        {
            await stderr.WriteLineAsync(parseResult.Error).ConfigureAwait(false);
            return 1;
        }

        try
        {
            string confirmation = await _orchestrator.ConvertAsync(
                parseResult.PdfPath!,
                parseResult.ChunkByChapter,
                cancellationToken).ConfigureAwait(false);

            IEnumerable<string> paths = ExtractPaths(confirmation);
            foreach (string path in paths)
            {
                await stdout.WriteLineAsync(path).ConfigureAwait(false);
            }

            return 0;
        }
        catch (McpException ex)
        {
            LogConversionFailed(ex);
            await stderr.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return 1;
        }
        catch (OperationCanceledException)
        {
            LogConversionCancelled();
            await stderr.WriteLineAsync("Operation cancelled.").ConfigureAwait(false);
            return 1;
        }
#pragma warning disable CA1031 // Catch a more specific exception type — intentional catch-all for CLI safety
        catch (Exception ex)
        {
            LogUnexpectedError(ex);
            await stderr.WriteLineAsync("An unexpected error occurred while processing the file.").ConfigureAwait(false);
            return 1;
        }
#pragma warning restore CA1031
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Conversion failed")]
    private partial void LogConversionFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Conversion cancelled")]
    private partial void LogConversionCancelled();

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error during conversion")]
    private partial void LogUnexpectedError(Exception ex);

    private static IEnumerable<string> ExtractPaths(string confirmation)
    {
        // The orchestrator returns either:
        //   "Converted: <path>"                    (single file)
        //   "Converted into N files:\n<path>\n..." (multiple files)
        string[] lines = confirmation.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 1 && lines[0].StartsWith("Converted:", StringComparison.Ordinal))
        {
            yield return lines[0]["Converted:".Length..].Trim();
            yield break;
        }

        // Skip the header line ("Converted into N files:")
        foreach (string line in lines.Skip(1))
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                yield return trimmed;
            }
        }
    }
}
