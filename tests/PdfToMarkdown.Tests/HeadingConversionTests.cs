using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Tests for heading conversion enhancement (Task 015 / FRD-004).
/// </summary>
public class HeadingConversionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfMarkdownConverter _sut;

    public HeadingConversionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HeadingTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        TestPdfGenerator.GenerateAll(_tempDir);

        _sut = new PdfMarkdownConverter(NullLoggerFactory.Instance.CreateLogger<PdfMarkdownConverter>());
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
        GC.SuppressFinalize(this);
    }

    private async Task<string> ConvertAndCollectAsync(string filename)
    {
        string path = Path.Combine(_tempDir, filename);
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _sut.ConvertAsync(path, writer);
        return string.Join("", pages);
    }

    [Fact]
    public async Task ConvertAsync_SixHeadingLevels_ProducesAllSixMarkdownLevels()
    {
        string content = await ConvertAndCollectAsync("six-heading-levels.pdf");

        content.Should().Contain("# Heading Level 1");
        content.Should().Contain("## Heading Level 2");
        content.Should().Contain("### Heading Level 3");
        content.Should().Contain("#### Heading Level 4");
        content.Should().Contain("##### Heading Level 5");
        content.Should().Contain("###### Heading Level 6");
    }

    [Fact]
    public async Task ConvertAsync_HeadingWithBoldFont_SuppressesBoldMarkers()
    {
        string content = await ConvertAndCollectAsync("heading-with-emphasis.pdf");

        // The heading should NOT have bold markers — headings are inherently bold
        content.Should().Contain("# Bold Heading Only");
        content.Should().NotContain("# **Bold Heading Only**");
        content.Should().NotContain("**Bold Heading Only**");
    }

    [Fact]
    public async Task ConvertAsync_HeadingWithItalicWord_PreservesItalicMarkers()
    {
        string content = await ConvertAndCollectAsync("heading-with-emphasis.pdf");

        // The H2 heading should have italic word preserved
        content.Should().Contain("*italic*");
    }

    [Fact]
    public async Task ConvertAsync_HeadingWithBoldItalic_RendersAsItalicOnly()
    {
        string content = await ConvertAndCollectAsync("heading-with-emphasis.pdf");

        // Bold-italic in heading context renders as italic only (bold suppressed)
        // The heading text should contain *italic* but NOT ***italic***
        string[] lines = content.Split('\n');
        string? headingLine = lines.FirstOrDefault(l => l.Contains("italic") && l.TrimStart().StartsWith("##", StringComparison.Ordinal));
        headingLine.Should().NotBeNull();
        headingLine.Should().NotContain("***");
        headingLine.Should().Contain("*italic*");
    }

    [Fact]
    public async Task ConvertAsync_HeadingsAndBody_PreservesCorrectOrder()
    {
        string content = await ConvertAndCollectAsync("headings.pdf");

        int mainIdx = content.IndexOf("Main Heading", StringComparison.Ordinal);
        int bodyAfterMain = content.IndexOf("Body text under main heading", StringComparison.Ordinal);
        int subIdx = content.IndexOf("Sub Heading", StringComparison.Ordinal);
        int bodyAfterSub = content.IndexOf("Body text under sub heading", StringComparison.Ordinal);
        int minorIdx = content.IndexOf("Minor Heading", StringComparison.Ordinal);

        mainIdx.Should().BeLessThan(bodyAfterMain);
        bodyAfterMain.Should().BeLessThan(subIdx);
        subIdx.Should().BeLessThan(bodyAfterSub);
        bodyAfterSub.Should().BeLessThan(minorIdx);
    }

    [Fact]
    public async Task ConvertAsync_NoHeadings_ProducesBodyTextOnly()
    {
        string content = await ConvertAndCollectAsync("no-headings.pdf");

        content.Should().NotContain("# ");
        content.Should().Contain("First line of text");
        content.Should().Contain("Second line of text");
        content.Should().Contain("Third line of text");
    }

    [Fact]
    public async Task ConvertAsync_HeadingTextContent_IsPreservedVerbatim()
    {
        string content = await ConvertAndCollectAsync("headings.pdf");

        content.Should().Contain("Main Heading");
        content.Should().Contain("Sub Heading");
        content.Should().Contain("Minor Heading");
    }

    [Fact]
    public async Task ConvertAsync_UntaggedPdf_UsesBodyFontSizeHeuristics()
    {
        // headings.pdf has body at 12pt and headings at 28/20/16pt
        string content = await ConvertAndCollectAsync("headings.pdf");

        // All heading levels should be detected based on font size
        content.Should().Contain("# ");
        content.Should().Contain("## ");
        content.Should().Contain("### ");

        // Body text should NOT start with #
        string[] lines = content.Split('\n');
        foreach (string line in lines)
        {
            if (line.Contains("Body text", StringComparison.Ordinal))
            {
                line.TrimStart().Should().NotStartWith("#");
            }
        }
    }
}
