namespace PdfToMarkdown.Services;

/// <summary>
/// Parses command-line arguments for CLI mode.
/// </summary>
internal static class CliArgumentParser
{
    private const string ChunkFlag = "--chunk-by-chapter";

    private const string UsageMessage = "Usage: PdfToMarkdown <pdfPath> [--chunk-by-chapter]";

    /// <summary>
    /// Parses the given command-line arguments into a <see cref="CliParseResult"/>.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <returns>A <see cref="CliParseResult"/> representing the parsed arguments or an error.</returns>
    public static CliParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return CliParseResult.Failure(UsageMessage);
        }

        string? pdfPath = null;
        bool chunkByChapter = false;

        foreach (string arg in args)
        {
            if (string.Equals(arg, ChunkFlag, StringComparison.OrdinalIgnoreCase))
            {
                chunkByChapter = true;
            }
            else if (arg.StartsWith('-'))
            {
                return CliParseResult.Failure($"Unrecognized option: {arg}\n{UsageMessage}");
            }
            else if (pdfPath is null)
            {
                pdfPath = arg;
            }
            else
            {
                return CliParseResult.Failure($"Unexpected argument: {arg}\n{UsageMessage}");
            }
        }

        if (pdfPath is null)
        {
            return CliParseResult.Failure(UsageMessage);
        }

        return CliParseResult.Success(pdfPath, chunkByChapter);
    }
}
