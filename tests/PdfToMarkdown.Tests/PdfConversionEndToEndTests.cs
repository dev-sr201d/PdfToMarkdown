using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// End-to-end integration tests that exercise the full PDF-to-Markdown conversion
/// pipeline with real implementations: validator → writer → converter → orchestrator.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PdfConversionEndToEndTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly ConversionOrchestrator _orchestrator;

    public PdfConversionEndToEndTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "PdfE2E_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);

        InputValidator validator = new();
        PdfMarkdownConverter converter = new(
            NullLoggerFactory.Instance.CreateLogger<PdfMarkdownConverter>());
        MarkdownWriter writer = new(
            NullLoggerFactory.Instance.CreateLogger<MarkdownWriter>());

        _orchestrator = new ConversionOrchestrator(validator, converter, writer);
    }

    public Task InitializeAsync()
    {
        TestPdfGenerator.GenerateAll(_testDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Markdown Output Correctness
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_SimpleDocument_ProducesMdFileWithContent()
    {
        string pdfPath = Path.Combine(_testDir, "simple.pdf");

        string result = await _orchestrator.ConvertAsync(pdfPath, chunkByChapter: false);

        string mdPath = Path.Combine(_testDir, "simple.md");
        File.Exists(mdPath).Should().BeTrue("output .md file must be created alongside the PDF");
        result.Should().Contain(Path.GetFullPath(mdPath));

        string content = await File.ReadAllTextAsync(mdPath);
        content.Should().Contain("Document Title");
        content.Should().Contain("first paragraph");
        content.Should().Contain("second paragraph");
    }

    [Fact]
    public async Task ConvertAsync_MultiPageDocument_ContainsAllPagesInOrder()
    {
        string pdfPath = Path.Combine(_testDir, "multi-page.pdf");

        await _orchestrator.ConvertAsync(pdfPath, chunkByChapter: false);

        string mdPath = Path.Combine(_testDir, "multi-page.md");
        string content = await File.ReadAllTextAsync(mdPath);

        // All three pages should appear in order
        int pos1 = content.IndexOf("page 1", StringComparison.OrdinalIgnoreCase);
        int pos2 = content.IndexOf("page 2", StringComparison.OrdinalIgnoreCase);
        int pos3 = content.IndexOf("page 3", StringComparison.OrdinalIgnoreCase);

        pos1.Should().BeGreaterThanOrEqualTo(0, "page 1 content must appear");
        pos2.Should().BeGreaterThan(pos1, "page 2 content must appear after page 1");
        pos3.Should().BeGreaterThan(pos2, "page 3 content must appear after page 2");
    }

    [Fact]
    public async Task ConvertAsync_HeadingsDocument_ContainsMarkdownHeadingSyntax()
    {
        string pdfPath = Path.Combine(_testDir, "headings.pdf");

        await _orchestrator.ConvertAsync(pdfPath, chunkByChapter: false);

        string mdPath = Path.Combine(_testDir, "headings.md");
        string content = await File.ReadAllTextAsync(mdPath);

        // Should contain heading markers — exact levels depend on font size analysis
        content.Should().Contain("# ", "at least one H1-level heading should be present");
        content.Should().Contain("Main Heading");
        content.Should().Contain("Sub Heading");
        content.Should().Contain("Minor Heading");
        content.Should().Contain("Body text under main heading");
    }

    [Fact]
    public async Task ConvertAsync_ListsDocument_ContainsMarkdownListSyntax()
    {
        string pdfPath = Path.Combine(_testDir, "lists.pdf");

        await _orchestrator.ConvertAsync(pdfPath, chunkByChapter: false);

        string mdPath = Path.Combine(_testDir, "lists.md");
        string content = await File.ReadAllTextAsync(mdPath);

        // Unordered list items
        content.Should().Contain("- ", "unordered list should use '- ' syntax");
        content.Should().Contain("First bullet item");
        content.Should().Contain("Second bullet item");
        content.Should().Contain("Third bullet item");

        // Ordered list items
        content.Should().Contain("1. ", "ordered list should use '1. ' syntax");
        content.Should().Contain("First numbered item");
        content.Should().Contain("Second numbered item");
        content.Should().Contain("Third numbered item");
    }

    [Fact]
    public async Task ConvertAsync_EmphasisDocument_ContainsMarkdownEmphasisSyntax()
    {
        string pdfPath = Path.Combine(_testDir, "emphasis.pdf");

        await _orchestrator.ConvertAsync(pdfPath, chunkByChapter: false);

        string mdPath = Path.Combine(_testDir, "emphasis.md");
        string content = await File.ReadAllTextAsync(mdPath);

        content.Should().Contain("**bold text**", "bold text should be wrapped in **");
        content.Should().Contain("*italic text*", "italic text should be wrapped in *");
        content.Should().Contain("***bold italic text***", "bold-italic text should be wrapped in ***");
        content.Should().Contain("regular text", "regular text should appear unformatted");
    }

    [Fact]
    public async Task ConvertAsync_MixedContentDocument_ContainsAllElementTypes()
    {
        string pdfPath = Path.Combine(_testDir, "mixed-content.pdf");

        await _orchestrator.ConvertAsync(pdfPath, chunkByChapter: false);

        string mdPath = Path.Combine(_testDir, "mixed-content.md");
        string content = await File.ReadAllTextAsync(mdPath);

        // Heading
        content.Should().Contain("# ", "should contain heading syntax");
        content.Should().Contain("Chapter One");

        // Paragraph text with emphasis
        content.Should().Contain("introductory paragraph");
        content.Should().Contain("*emphasized*", "italic emphasis should be preserved");

        // List items
        content.Should().Contain("- First item");
        content.Should().Contain("- Second item");

        // Sub-heading
        content.Should().Contain("Section 1.1");

        // Body text under sub-heading
        content.Should().Contain("Body text under the sub-section heading");
    }

    // ────────────────────────────────────────────────────────────────────
    //  Incremental Writing Verification
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_StandardMode_OutputFileExistsDuringConversion()
    {
        string pdfPath = Path.Combine(_testDir, "multi-page.pdf");
        string mdPath = Path.Combine(_testDir, "multi-page.md");

        InputValidator validator = new();
        PdfMarkdownConverter converter = new(
            NullLoggerFactory.Instance.CreateLogger<PdfMarkdownConverter>());
        MarkdownWriter writer = new(NullLoggerFactory.Instance.CreateLogger<MarkdownWriter>());

        await validator.ValidateAsync(pdfPath);
        await writer.InitializeAsync(pdfPath, chunkByChapter: false);

        // The output file should already exist (created during initialization)
        File.Exists(mdPath).Should().BeTrue("output file should be created at initialization");

        await converter.ConvertAsync(pdfPath, writer);

        // After writing, content should be flushed incrementally
        using FileStream fs = new(mdPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new(fs);
        string beforeFinalize = await reader.ReadToEndAsync();
        beforeFinalize.Should().NotBeEmpty("content should be on disk before finalization");

        await writer.FinalizeAsync();
        await writer.DisposeAsync();
    }

    // ────────────────────────────────────────────────────────────────────
    //  Chunked Output
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_ChunkedMode_ProducesMultipleFiles()
    {
        // Generate a multi-chapter PDF (multiple H1 headings)
        string pdfPath = Path.Combine(_testDir, "chapters.pdf");
        GenerateMultiChapterPdf(pdfPath);

        // Need a fresh orchestrator with a fresh writer for each test
        ConversionOrchestrator orch = CreateOrchestrator();
        string result = await orch.ConvertAsync(pdfPath, chunkByChapter: true);

        // Verify multiple chapter files exist
        string[] chapterFiles = Directory.GetFiles(_testDir, "chapters_*.md");
        chapterFiles.Length.Should().BeGreaterThanOrEqualTo(2, "at least 2 chapter files should be created");

        // Verify confirmation message lists all paths
        foreach (string file in chapterFiles)
        {
            result.Should().Contain(Path.GetFullPath(file));
        }
    }

    [Fact]
    public async Task ConvertAsync_ChunkedMode_ChapterBoundariesCorrect()
    {
        string pdfPath = Path.Combine(_testDir, "chapters2.pdf");
        GenerateMultiChapterPdf(pdfPath);

        ConversionOrchestrator orch = CreateOrchestrator();
        await orch.ConvertAsync(pdfPath, chunkByChapter: true);

        string[] chapterFiles = Directory.GetFiles(_testDir, "chapters2_*.md")
            .OrderBy(f => f)
            .ToArray();

        chapterFiles.Length.Should().BeGreaterThanOrEqualTo(2);

        // Each chapter file (after the first potentially) should start with an H1
        for (int i = 1; i < chapterFiles.Length; i++)
        {
            string chapterContent = await File.ReadAllTextAsync(chapterFiles[i]);
            string[] lines = chapterContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string firstNonEmpty = lines.FirstOrDefault(l => l.Trim().Length > 0) ?? "";
            firstNonEmpty.TrimStart().Should().StartWith("# ", $"chapter file {i + 1} should start with an H1 heading");
        }
    }

    [Fact]
    public async Task ConvertAsync_ChunkedMode_ConfirmationListsAllPaths()
    {
        string pdfPath = Path.Combine(_testDir, "chapters3.pdf");
        GenerateMultiChapterPdf(pdfPath);

        ConversionOrchestrator orch = CreateOrchestrator();
        string result = await orch.ConvertAsync(pdfPath, chunkByChapter: true);

        result.Should().Contain("Converted into");
        string[] chapterFiles = Directory.GetFiles(_testDir, "chapters3_*.md");
        result.Should().Contain($"{chapterFiles.Length} files:");
    }

    // ────────────────────────────────────────────────────────────────────
    //  Full Pipeline via Orchestrator
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Orchestrator_StandardConversion_WritesSingleMdFile()
    {
        string pdfPath = Path.Combine(_testDir, "simple.pdf");

        ConversionOrchestrator orch = CreateOrchestrator();
        string result = await orch.ConvertAsync(pdfPath, chunkByChapter: false);

        result.Should().StartWith("Converted:");
        string mdPath = Path.Combine(_testDir, "simple.md");
        File.Exists(mdPath).Should().BeTrue();
        result.Should().Contain(Path.GetFullPath(mdPath));
    }

    [Fact]
    public async Task Orchestrator_ChunkedConversion_WritesMultipleMdFiles()
    {
        string pdfPath = Path.Combine(_testDir, "chunked_orch.pdf");
        GenerateMultiChapterPdf(pdfPath);

        ConversionOrchestrator orch = CreateOrchestrator();
        string result = await orch.ConvertAsync(pdfPath, chunkByChapter: true);

        result.Should().Contain("Converted into");
        string[] mdFiles = Directory.GetFiles(_testDir, "chunked_orch_*.md");
        mdFiles.Length.Should().BeGreaterThanOrEqualTo(2);
    }

    // ────────────────────────────────────────────────────────────────────
    //  Error Scenarios
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_NonexistentFile_ThrowsMcpException()
    {
        string pdfPath = Path.Combine(_testDir, "does-not-exist.pdf");

        ConversionOrchestrator orch = CreateOrchestrator();
        Func<Task> act = () => orch.ConvertAsync(pdfPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        ex.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ConvertAsync_NoTextPdf_ThrowsMcpException()
    {
        string pdfPath = Path.Combine(_testDir, "no-text.pdf");

        ConversionOrchestrator orch = CreateOrchestrator();
        Func<Task> act = () => orch.ConvertAsync(pdfPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        ex.Message.Should().Contain("no extractable text", "should indicate no text content found");
    }

    [Fact]
    public async Task ConvertAsync_EmptyPdf_ThrowsMcpException()
    {
        string pdfPath = Path.Combine(_testDir, "empty.pdf");

        ConversionOrchestrator orch = CreateOrchestrator();
        Func<Task> act = () => orch.ConvertAsync(pdfPath, chunkByChapter: false);

        // Empty PDF has no text — expect an exception about no content
        await act.Should().ThrowAsync<McpException>();
    }

    [Fact]
    public async Task ConvertAsync_InvalidExtension_ThrowsMcpException()
    {
        string txtPath = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(txtPath, "not a pdf");

        ConversionOrchestrator orch = CreateOrchestrator();
        Func<Task> act = () => orch.ConvertAsync(txtPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        ex.Message.Should().Contain(".pdf", "should mention expected .pdf extension");
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private static ConversionOrchestrator CreateOrchestrator()
    {
        InputValidator validator = new();
        PdfMarkdownConverter converter = new(
            NullLoggerFactory.Instance.CreateLogger<PdfMarkdownConverter>());
        MarkdownWriter writer = new(
            NullLoggerFactory.Instance.CreateLogger<MarkdownWriter>());

        return new ConversionOrchestrator(validator, converter, writer);
    }

    /// <summary>
    /// Generates a PDF with two H1-level chapters, each with body text,
    /// to test chunked mode.
    /// </summary>
    private static void GenerateMultiChapterPdf(string pdfPath)
    {
        using UglyToad.PdfPig.Writer.PdfDocumentBuilder builder = new();
        UglyToad.PdfPig.Writer.PdfDocumentBuilder.AddedFont regular =
            builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        UglyToad.PdfPig.Writer.PdfDocumentBuilder.AddedFont bold =
            builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.HelveticaBold);

        // Page 1: Chapter 1 with body text
        UglyToad.PdfPig.Writer.PdfPageBuilder page1 = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        double top1 = page1.PageSize.Top - 50;
        page1.AddText("Chapter One Title", 24, new UglyToad.PdfPig.Core.PdfPoint(50, top1), bold);
        page1.AddText("Body text under chapter one.", 12, new UglyToad.PdfPig.Core.PdfPoint(50, top1 - 40), regular);
        page1.AddText("More content in chapter one.", 12, new UglyToad.PdfPig.Core.PdfPoint(50, top1 - 60), regular);

        // Page 2: Chapter 2 with body text
        UglyToad.PdfPig.Writer.PdfPageBuilder page2 = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        double top2 = page2.PageSize.Top - 50;
        page2.AddText("Chapter Two Title", 24, new UglyToad.PdfPig.Core.PdfPoint(50, top2), bold);
        page2.AddText("Body text under chapter two.", 12, new UglyToad.PdfPig.Core.PdfPoint(50, top2 - 40), regular);

        File.WriteAllBytes(pdfPath, builder.Build());
    }
}
