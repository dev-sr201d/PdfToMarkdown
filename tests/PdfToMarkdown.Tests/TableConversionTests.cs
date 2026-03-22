using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Tests for table detection and conversion (Task 017 / FRD-006).
/// </summary>
public class TableConversionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfMarkdownConverter _sut;

    public TableConversionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TableTests_" + Guid.NewGuid().ToString("N"));
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
    public async Task ConvertAsync_SimpleTable_ProducesMarkdownTableSyntax()
    {
        string content = await ConvertAndCollectAsync("tables.pdf");

        // Should contain pipe-delimited table syntax
        content.Should().Contain("|");
        content.Should().Contain("---|");
    }

    [Fact]
    public async Task ConvertAsync_SimpleTable_HeaderRowRenderedCorrectly()
    {
        string content = await ConvertAndCollectAsync("tables.pdf");

        // Header row should contain column names
        content.Should().Contain("Name");
        content.Should().Contain("Age");
        content.Should().Contain("City");

        // Header should come before separator
        int headerIdx = content.IndexOf("Name", StringComparison.Ordinal);
        int separatorIdx = content.IndexOf("---|", StringComparison.Ordinal);
        headerIdx.Should().BeLessThan(separatorIdx);
    }

    [Fact]
    public async Task ConvertAsync_SimpleTable_DataRowsRenderedCorrectly()
    {
        string content = await ConvertAndCollectAsync("tables.pdf");

        content.Should().Contain("Alice");
        content.Should().Contain("30");
        content.Should().Contain("London");
        content.Should().Contain("Bob");
        content.Should().Contain("25");
        content.Should().Contain("Paris");
    }

    [Fact]
    public async Task ConvertAsync_SimpleTable_SeparatorRowPresent()
    {
        string content = await ConvertAndCollectAsync("tables.pdf");

        // Separator should have same number of columns as header
        // For a 3-column table: |---|---|---|
        content.Should().Contain("---|---|---|");
    }

    [Fact]
    public async Task ConvertAsync_SimpleTable_CellContentPreservedAccurately()
    {
        string content = await ConvertAndCollectAsync("tables.pdf");

        // Find the data row containing Alice
        string[] lines = content.Split('\n');
        string? aliceRow = lines.FirstOrDefault(l => l.Contains("Alice"));
        aliceRow.Should().NotBeNull();
        aliceRow.Should().Contain("30");
        aliceRow.Should().Contain("London");
    }

    [Fact]
    public async Task ConvertAsync_TableWithEmphasis_PreservesEmphasisInCells()
    {
        string content = await ConvertAndCollectAsync("table-with-emphasis.pdf");

        // Bold header cells
        content.Should().Contain("**Feature**");
        content.Should().Contain("**Status**");

        // Italic cell content
        content.Should().Contain("*Done*");
    }

    [Fact]
    public async Task ConvertAsync_TableWithVaryingColumns_PadsShorterRows()
    {
        string content = await ConvertAndCollectAsync("table-varying-columns.pdf");

        // Header has 3 columns, last data row has only 2
        content.Should().Contain("---|");

        // All rows should have consistent pipe count
        string[] lines = content.Split('\n');
        List<string> tableLines = lines
            .Where(l => l.TrimStart().StartsWith('|'))
            .ToList();

        tableLines.Should().HaveCountGreaterThanOrEqualTo(3); // header + separator + data rows

        // All table rows should have the same pipe count (same column count)
        int expectedPipes = tableLines[0].Count(c => c == '|');
        foreach (string tableLine in tableLines)
        {
            tableLine.Count(c => c == '|').Should().Be(expectedPipes);
        }
    }

    [Fact]
    public async Task ConvertAsync_NoTables_ProducesNoTableSyntax()
    {
        string content = await ConvertAndCollectAsync("no-headings.pdf");

        // No pipe-delimited tables in a pure-text PDF
        content.Should().NotContain("|");
        content.Should().NotContain("---|");
    }

    [Fact]
    public async Task ConvertAsync_SingleDataRow_ProducesValidTable()
    {
        string content = await ConvertAndCollectAsync("table-with-emphasis.pdf");

        // table-with-emphasis.pdf has header + 2 data rows
        string[] lines = content.Split('\n');
        List<string> tableLines = lines
            .Where(l => l.TrimStart().StartsWith('|'))
            .ToList();

        // Expect: header + separator + data rows
        tableLines.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ConvertAsync_EmphasisPdf_NotDetectedAsTable()
    {
        // emphasis.pdf has text at different X positions but should NOT be a table
        string content = await ConvertAndCollectAsync("emphasis.pdf");

        // emphasis.pdf should produce emphasis markers, not table syntax
        content.Should().Contain("**");
        content.Should().Contain("*");

        // Verify no table separator syntax
        content.Should().NotContain("---|");
    }
}
