using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Tests for list conversion enhancement (Task 016 / FRD-005).
/// </summary>
public class ListConversionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfMarkdownConverter _sut;

    public ListConversionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ListTests_" + Guid.NewGuid().ToString("N"));
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
    public async Task ConvertAsync_UnorderedList_ProducesCorrectMarkdownSyntax()
    {
        string content = await ConvertAndCollectAsync("lists.pdf");

        content.Should().Contain("- First bullet item");
        content.Should().Contain("- Second bullet item");
        content.Should().Contain("- Third bullet item");
    }

    [Fact]
    public async Task ConvertAsync_OrderedList_ProducesSequentialNumbering()
    {
        string content = await ConvertAndCollectAsync("lists.pdf");

        content.Should().Contain("1. First numbered item");
        content.Should().Contain("2. Second numbered item");
        content.Should().Contain("3. Third numbered item");
    }

    [Fact]
    public async Task ConvertAsync_OrderedList_PreservesItemOrder()
    {
        string content = await ConvertAndCollectAsync("lists.pdf");

        int first = content.IndexOf("1. First numbered item", StringComparison.Ordinal);
        int second = content.IndexOf("2. Second numbered item", StringComparison.Ordinal);
        int third = content.IndexOf("3. Third numbered item", StringComparison.Ordinal);

        first.Should().BeLessThan(second);
        second.Should().BeLessThan(third);
    }

    [Fact]
    public async Task ConvertAsync_NestedLists_ProducesIndentedItems()
    {
        string content = await ConvertAndCollectAsync("nested-lists.pdf");

        // Top-level items should not be indented
        content.Should().Contain("- Top item one");
        content.Should().Contain("- Top item two");

        // Nested items should be indented (2 spaces per level)
        content.Should().Contain("  - Nested item A");
        content.Should().Contain("  - Nested item B");
    }

    [Fact]
    public async Task ConvertAsync_MultiLineListItem_MergesContinuationLines()
    {
        string content = await ConvertAndCollectAsync("multi-line-list.pdf");

        // The first item and its continuation should be merged
        content.Should().Contain("First item start continuation of first");
        // Second item should be separate
        content.Should().Contain("- Second item");
    }

    [Fact]
    public async Task ConvertAsync_ListWithEmphasis_PreservesBoldAndItalic()
    {
        string content = await ConvertAndCollectAsync("list-with-emphasis.pdf");

        // List items should contain emphasis markers
        content.Should().Contain("**bold**");
        content.Should().Contain("*italic*");
    }

    [Fact]
    public async Task ConvertAsync_NoLists_ProducesNoListMarkers()
    {
        string content = await ConvertAndCollectAsync("no-headings.pdf");

        // no-headings.pdf has only body text — no lists
        content.Should().NotContain("- ");
        content.Should().NotMatchRegex(@"^\d+\.\s");
    }

    [Fact]
    public async Task ConvertAsync_ListsAndParagraphs_PreservesDocumentOrder()
    {
        string content = await ConvertAndCollectAsync("mixed-content.pdf");

        // In mixed-content: heading → paragraph → list → sub-heading → body
        int paragraphIdx = content.IndexOf("introductory paragraph", StringComparison.Ordinal);
        int listIdx = content.IndexOf("- First item", StringComparison.Ordinal);
        int subHeadingIdx = content.IndexOf("Section 1.1", StringComparison.Ordinal);

        paragraphIdx.Should().BeLessThan(listIdx);
        listIdx.Should().BeLessThan(subHeadingIdx);
    }

    [Fact]
    public async Task ConvertAsync_ConsecutiveSeparateLists_AreRenderedSeparately()
    {
        // lists.pdf has unordered list followed by ordered list
        string content = await ConvertAndCollectAsync("lists.pdf");

        // Both types should appear
        content.Should().Contain("- First bullet item");
        content.Should().Contain("1. First numbered item");
    }
}
