using FluentAssertions;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Unit tests for <see cref="CliArgumentParser"/>.
/// </summary>
public class CliArgumentParserTests
{
    [Fact]
    public void Parse_WithPdfPathOnly_ReturnsSuccess()
    {
        CliParseResult result = CliArgumentParser.Parse(["report.pdf"]);

        result.IsSuccess.Should().BeTrue();
        result.PdfPath.Should().Be("report.pdf");
        result.ChunkByChapter.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithAbsolutePath_ReturnsSuccess()
    {
        CliParseResult result = CliArgumentParser.Parse([@"C:\docs\report.pdf"]);

        result.IsSuccess.Should().BeTrue();
        result.PdfPath.Should().Be(@"C:\docs\report.pdf");
    }

    [Fact]
    public void Parse_WithChunkFlag_ReturnsChunkEnabled()
    {
        CliParseResult result = CliArgumentParser.Parse(["report.pdf", "--chunk-by-chapter"]);

        result.IsSuccess.Should().BeTrue();
        result.PdfPath.Should().Be("report.pdf");
        result.ChunkByChapter.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithChunkFlagBeforePath_ReturnsChunkEnabled()
    {
        CliParseResult result = CliArgumentParser.Parse(["--chunk-by-chapter", "report.pdf"]);

        result.IsSuccess.Should().BeTrue();
        result.PdfPath.Should().Be("report.pdf");
        result.ChunkByChapter.Should().BeTrue();
    }

    [Fact]
    public void Parse_ChunkFlagCaseInsensitive_ReturnsChunkEnabled()
    {
        CliParseResult result = CliArgumentParser.Parse(["report.pdf", "--Chunk-By-Chapter"]);

        result.IsSuccess.Should().BeTrue();
        result.ChunkByChapter.Should().BeTrue();
    }

    [Fact]
    public void Parse_NoArguments_ReturnsFailure()
    {
        CliParseResult result = CliArgumentParser.Parse([]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Usage:");
    }

    [Fact]
    public void Parse_UnrecognizedFlag_ReturnsFailure()
    {
        CliParseResult result = CliArgumentParser.Parse(["report.pdf", "--unknown-flag"]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unrecognized option");
        result.Error.Should().Contain("--unknown-flag");
        result.Error.Should().Contain("Usage:");
    }

    [Fact]
    public void Parse_MultiplePositionalArgs_ReturnsFailure()
    {
        CliParseResult result = CliArgumentParser.Parse(["first.pdf", "second.pdf"]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unexpected argument");
        result.Error.Should().Contain("second.pdf");
    }

    [Fact]
    public void Parse_OnlyChunkFlag_ReturnsFailure()
    {
        CliParseResult result = CliArgumentParser.Parse(["--chunk-by-chapter"]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Usage:");
    }
}
