using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using NSubstitute;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Unit tests for <see cref="PdfMarkdownConverter"/>.
/// </summary>
public class PdfMarkdownConverterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfMarkdownConverter _sut;

    public PdfMarkdownConverterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PdfMarkdownConverterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        TestPdfGenerator.GenerateAll(_tempDir);

        _sut = new PdfMarkdownConverter(NullLoggerFactory.Instance.CreateLogger<PdfMarkdownConverter>());
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ConvertAsync_SimplePdf_WritesMarkdownToWriter()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "simple.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert
        pages.Should().NotBeEmpty();
        string allContent = string.Join("", pages);
        allContent.Should().Contain("paragraph");
    }

    [Fact]
    public async Task ConvertAsync_SimplePdf_EmitsHeadingForTitle()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "simple.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — the title (24pt bold) should be emitted as a Markdown heading
        string allContent = string.Join("", pages);
        allContent.Should().Contain("# ");
        allContent.Should().Contain("Document Title");
    }

    [Fact]
    public async Task ConvertAsync_HeadingsPdf_EmitsCorrectHeadingLevels()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "headings.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert
        string allContent = string.Join("", pages);
        allContent.Should().Contain("# ");
        allContent.Should().Contain("## ");
        allContent.Should().Contain("### ");
        allContent.Should().Contain("Main Heading");
        allContent.Should().Contain("Sub Heading");
        allContent.Should().Contain("Minor Heading");
    }

    [Fact]
    public async Task ConvertAsync_MultiPagePdf_WritesOneCallPerPage()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "multi-page.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — should have 3 separate writer calls (one per page)
        pages.Should().HaveCount(3);
    }

    [Fact]
    public async Task ConvertAsync_ListsPdf_EmitsUnorderedListSyntax()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "lists.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert
        string allContent = string.Join("", pages);
        allContent.Should().Contain("- First bullet item");
        allContent.Should().Contain("- Second bullet item");
        allContent.Should().Contain("- Third bullet item");
    }

    [Fact]
    public async Task ConvertAsync_ListsPdf_EmitsOrderedListSyntax()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "lists.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert
        string allContent = string.Join("", pages);
        allContent.Should().Contain("1. First numbered item");
        allContent.Should().Contain("2. Second numbered item");
        allContent.Should().Contain("3. Third numbered item");
    }

    [Fact]
    public async Task ConvertAsync_EmphasisPdf_EmitsBoldAndItalicSyntax()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "emphasis.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert
        string allContent = string.Join("", pages);
        allContent.Should().Contain("**"); // bold markers
        allContent.Should().Contain("*");   // italic markers
    }

    [Fact]
    public async Task ConvertAsync_CancelledDuringPreAnalysis_ThrowsOperationCanceled()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "multi-page.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task> act = () => _sut.ConvertAsync(path, writer, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ConvertAsync_CancelledDuringConversion_ThrowsOperationCanceled()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "multi-page.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        using CancellationTokenSource cts = new();

        int writeCallCount = 0;
        writer.WritePageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                writeCallCount++;
                if (writeCallCount >= 2)
                {
                    cts.Cancel();
                }
                return Task.CompletedTask;
            });

        // Act
        Func<Task> act = () => _sut.ConvertAsync(path, writer, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ConvertAsync_NonexistentFile_ThrowsMcpException()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "nonexistent.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();

        // Act
        Func<Task> act = () => _sut.ConvertAsync(path, writer);

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ConvertAsync_NoTextPdf_ThrowsMcpException()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "no-text.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();

        // Act
        Func<Task> act = () => _sut.ConvertAsync(path, writer);

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*no extractable text*");
    }

    [Fact]
    public async Task ConvertAsync_EmptyPdf_ThrowsMcpException()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "empty.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();

        // Act
        Func<Task> act = () => _sut.ConvertAsync(path, writer);

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*no extractable text*");
    }

    [Fact]
    public async Task ConvertAsync_PreAnalysis_UsesBodyFontForHeadingThreshold()
    {
        // Arrange — headings.pdf has body at 12pt and headings at 28/20/16pt
        string path = Path.Combine(_tempDir, "headings.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — body text should NOT have heading markers
        string allContent = string.Join("", pages);
        allContent.Should().Contain("Body text under");
        // Body text lines shouldn't start with #
        string[] lines = allContent.Split('\n');
        foreach (string line in lines)
        {
            if (line.Contains("Body text", StringComparison.Ordinal))
            {
                line.TrimStart().Should().NotStartWith("#");
            }
        }
    }

    [Fact]
    public async Task ConvertAsync_IncrementalOutput_WriterReceivesPageByPage()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "multi-page.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        int writeCallCount = 0;
        writer.WritePageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                writeCallCount++;
                return Task.CompletedTask;
            });

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — should have received exactly one call per page (3 pages)
        writeCallCount.Should().Be(3);
    }

    [Fact]
    public async Task ConvertAsync_TwoColumnPdf_OutputsLeftColumnBeforeRightColumn()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "two-column.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — left column content should appear before right column content
        string allContent = string.Join("", pages);
        int leftPos = allContent.IndexOf("Left column line 1", StringComparison.Ordinal);
        int rightPos = allContent.IndexOf("Right column line 1", StringComparison.Ordinal);

        leftPos.Should().BeGreaterThanOrEqualTo(0, "left column content should be present");
        rightPos.Should().BeGreaterThanOrEqualTo(0, "right column content should be present");
        leftPos.Should().BeLessThan(rightPos, "left column should appear before right column");
    }

    [Fact]
    public async Task ConvertAsync_TwoColumnPdf_LeftColumnLinesAreContiguous()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "two-column.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — all left column lines should appear before any right column line
        string allContent = string.Join("", pages);
        int lastLeft = allContent.LastIndexOf("Left column line 8", StringComparison.Ordinal);
        int firstRight = allContent.IndexOf("Right column line 1", StringComparison.Ordinal);

        lastLeft.Should().BeLessThan(firstRight, "all left column lines should precede right column lines");
    }

    [Fact]
    public async Task ConvertAsync_TwoColumnWithHeader_HeaderAppearsBeforeColumnContent()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "two-column-with-header.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — heading should appear before both column contents
        string allContent = string.Join("", pages);
        int headingPos = allContent.IndexOf("Full Width Heading", StringComparison.Ordinal);
        int leftPos = allContent.IndexOf("Left body line 1", StringComparison.Ordinal);
        int rightPos = allContent.IndexOf("Right body line 1", StringComparison.Ordinal);

        headingPos.Should().BeGreaterThanOrEqualTo(0, "heading should be present");
        headingPos.Should().BeLessThan(leftPos, "heading should appear before left column");
        headingPos.Should().BeLessThan(rightPos, "heading should appear before right column");
        leftPos.Should().BeLessThan(rightPos, "left column should appear before right column");
    }

    [Fact]
    public async Task ConvertAsync_BoldHeadings_DetectedAsHeadings()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "bold-headings.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — bold headings should have # prefix
        string allContent = string.Join("", pages);
        allContent.Should().Contain("# The First Section");
        allContent.Should().Contain("# The Second Section");
    }

    [Fact]
    public async Task ConvertAsync_MixedFontHeadings_DetectedAsHeadings()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "mixed-font-headings.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — different-font headings should have # prefix
        string allContent = string.Join("", pages);
        allContent.Should().Contain("# Chapter Overview");
        allContent.Should().Contain("# Key Principles");
    }

    [Fact]
    public async Task ConvertAsync_ThreeColumnPdf_OutputsColumnsInOrder()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "three-column.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — all three columns should be present and in left-to-right order
        string allContent = string.Join("", pages);
        int col1Pos = allContent.IndexOf("Col1 line 1", StringComparison.Ordinal);
        int col2Pos = allContent.IndexOf("Col2 line 1", StringComparison.Ordinal);
        int col3Pos = allContent.IndexOf("Col3 line 1", StringComparison.Ordinal);

        col1Pos.Should().BeGreaterThanOrEqualTo(0, "column 1 should be present");
        col2Pos.Should().BeGreaterThanOrEqualTo(0, "column 2 should be present");
        col3Pos.Should().BeGreaterThanOrEqualTo(0, "column 3 should be present");
        col1Pos.Should().BeLessThan(col2Pos, "column 1 should appear before column 2");
        col2Pos.Should().BeLessThan(col3Pos, "column 2 should appear before column 3");
    }

    [Fact]
    public async Task ConvertAsync_ThreeColumnPdf_EachColumnIsContiguous()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "three-column.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — all col1 lines should precede all col2 lines
        string allContent = string.Join("", pages);
        int lastCol1 = allContent.LastIndexOf("Col1 line 10", StringComparison.Ordinal);
        int firstCol2 = allContent.IndexOf("Col2 line 1", StringComparison.Ordinal);
        int lastCol2 = allContent.LastIndexOf("Col2 line 10", StringComparison.Ordinal);
        int firstCol3 = allContent.IndexOf("Col3 line 1", StringComparison.Ordinal);

        lastCol1.Should().BeLessThan(firstCol2, "all col1 lines should precede col2");
        lastCol2.Should().BeLessThan(firstCol3, "all col2 lines should precede col3");
    }

    [Fact]
    public async Task ConvertAsync_TwoColumnNarrowGap_DetectsColumnsCorrectly()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "two-column-narrow-gap.pdf");
        IMarkdownWriter writer = Substitute.For<IMarkdownWriter>();
        List<string> pages = [];
        writer.WritePageAsync(Arg.Do<string>(s => pages.Add(s)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ConvertAsync(path, writer);

        // Assert — left column content should appear before right column content
        string allContent = string.Join("", pages);
        int leftPos = allContent.IndexOf("Left col line 1", StringComparison.Ordinal);
        int rightPos = allContent.IndexOf("Right col line 1", StringComparison.Ordinal);

        leftPos.Should().BeGreaterThanOrEqualTo(0, "left column should be present");
        rightPos.Should().BeGreaterThanOrEqualTo(0, "right column should be present");
        leftPos.Should().BeLessThan(rightPos, "left column should appear before right column");

        // All left column lines should be contiguous (before any right column line)
        int lastLeft = allContent.LastIndexOf("Left col line 12", StringComparison.Ordinal);
        lastLeft.Should().BeLessThan(rightPos, "all left column lines should precede right column");
    }
}
