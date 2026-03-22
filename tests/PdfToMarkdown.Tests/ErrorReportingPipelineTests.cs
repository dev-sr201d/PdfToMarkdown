using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Tests that verify every FRD-010 error scenario produces the correct
/// <see cref="McpException"/> with a well-formed, user-facing error message
/// through the full conversion pipeline (validator → converter → writer).
/// All tests use <c>chunkByChapter: false</c> — no dependency on FRD-009.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ErrorReportingPipelineTests : IAsyncLifetime
{
    private readonly string _testDir;

    public ErrorReportingPipelineTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ErrorPipeline_" + Guid.NewGuid().ToString("N")[..8]);
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDir);
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
    //  Input Validation Errors (through orchestrator)
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_EmptyPath_ThrowsMcpExceptionWithActionableMessage()
    {
        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync("", chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: null);
        ex.Message.Should().Contain("path", "should indicate that a path is required");
    }

    [Fact]
    public async Task ConvertAsync_NullPath_ThrowsMcpExceptionWithActionableMessage()
    {
        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync(null!, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: null);
        ex.Message.Should().Contain("path", "should indicate that a path is required");
    }

    [Fact]
    public async Task ConvertAsync_WrongExtension_ThrowsMcpExceptionWithPath()
    {
        string txtPath = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(txtPath, "not a pdf");

        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync(txtPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: txtPath);
        ex.Message.Should().Contain(".pdf", "should mention the expected extension");
    }

    [Fact]
    public async Task ConvertAsync_NonExistentFile_ThrowsMcpExceptionWithPath()
    {
        string missingPath = Path.Combine(_testDir, "does-not-exist.pdf");

        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync(missingPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: missingPath);
        ex.Message.Should().Contain("not found", "should state the file was not found");
    }

    // ────────────────────────────────────────────────────────────────────
    //  Runtime Errors (converter-level, through orchestrator)
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_NoTextPdf_ThrowsMcpExceptionWithPath()
    {
        string pdfPath = Path.Combine(_testDir, "no-text.pdf");

        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync(pdfPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: pdfPath);
        ex.Message.Should().Contain("no extractable text", "should describe the problem");
    }

    [Fact]
    public async Task ConvertAsync_EncryptedPdf_ThrowsMcpExceptionWithPath()
    {
        // Create a minimal encrypted PDF fixture.
        // PdfPig cannot generate encrypted PDFs programmatically, so we use
        // a hand-crafted minimal PDF with an Encrypt dictionary entry that
        // causes PdfPig to throw an exception containing "password" or "encrypt".
        string encryptedPath = Path.Combine(_testDir, "encrypted.pdf");
        CreateMinimalEncryptedPdf(encryptedPath);

        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync(encryptedPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: encryptedPath);
        ex.Message.Should().ContainAny("encrypted", "password-protected",
            "should indicate the file is encrypted or password-protected");
    }

    // ────────────────────────────────────────────────────────────────────
    //  Write Failure Errors
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_LockedOutputFile_ThrowsMcpExceptionWithPath()
    {
        // Simulate a write failure by locking the output .md file with an exclusive
        // handle before the writer tries to create it.
        string pdfPath = Path.Combine(_testDir, "locked-output.pdf");
        File.Copy(Path.Combine(_testDir, "simple.pdf"), pdfPath);

        string mdPath = Path.Combine(_testDir, "locked-output.md");

        // Lock the output file exclusively so the writer cannot open it
        await using FileStream lockHandle = new(mdPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync(pdfPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: mdPath);
        ex.Message.Should().ContainAny("Cannot create output file", "Cannot write",
            "should describe the write failure");
    }

    [Fact]
    public async Task ConvertAsync_NonExistentOutputDirectory_ThrowsMcpExceptionWithPath()
    {
        // Create a PDF path in a directory that does not exist.
        // The validator checks that the file exists, so we need a file that exists
        // but whose parent directory is then deleted before the writer runs.
        // Instead, test the writer directly since we can control initialization.
        string nonExistentDir = Path.Combine(_testDir, "does_not_exist");
        string pdfPath = Path.Combine(nonExistentDir, "report.pdf");

        MarkdownWriter writer = new(NullLoggerFactory.Instance.CreateLogger<MarkdownWriter>());

        Func<Task> act = () => writer.InitializeAsync(pdfPath, chunkByChapter: false);

        McpException ex = (await act.Should().ThrowAsync<McpException>()).Which;
        AssertErrorMessageQuality(ex.Message, expectedPathSubstring: nonExistentDir);
        ex.Message.Should().Contain("directory", "should mention the directory issue");
    }

    // ────────────────────────────────────────────────────────────────────
    //  Cancellation
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        string pdfPath = Path.Combine(_testDir, "simple.pdf");
        using CancellationTokenSource cts = new();
        cts.Cancel();

        ConversionOrchestrator orch = CreateOrchestrator();

        Func<Task> act = () => orch.ConvertAsync(pdfPath, chunkByChapter: false, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>(
            "cancellation should propagate as OperationCanceledException, not McpException");
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
    /// Asserts that an error message meets FRD-010 quality requirements:
    /// actionable, includes file path (when provided), no stack traces, no exception type names.
    /// </summary>
    private static void AssertErrorMessageQuality(string message, string? expectedPathSubstring)
    {
        message.Should().NotBeNullOrWhiteSpace("error message must not be empty");

        // Must include the file path when one is expected
        if (expectedPathSubstring is not null)
        {
            message.Should().Contain(expectedPathSubstring, "error message must include the relevant file path");
        }

        // Must not contain stack traces
        message.Should().NotContain("StackTrace", "error message must not contain stack traces");
        message.Should().NotContain("at System.", "error message must not contain stack trace frames");
        message.Should().NotContain("at PdfToMarkdown.", "error message must not contain stack trace frames");
        message.Should().NotMatchRegex(@"in\s+\S+\.cs:line\s+\d+", "error message must not contain source file references");

        // Must not contain raw exception type names
        message.Should().NotContain("IOException", "error message must not expose internal exception types");
        message.Should().NotContain("UnauthorizedAccessException", "error message must not expose internal exception types");
        message.Should().NotContain("NullReferenceException", "error message must not expose internal exception types");
        message.Should().NotContain("InvalidOperationException", "error message must not expose internal exception types");
        message.Should().NotContain("FileNotFoundException", "error message must not expose internal exception types");

        // Must be actionable (not a generic "an error occurred" with no detail)
        message.Length.Should().BeGreaterThan(10, "error message should be descriptive enough to be actionable");
    }

    /// <summary>
    /// Creates a minimal PDF file that triggers PdfPig's encrypted-document exception.
    /// This produces a syntactically valid PDF with an /Encrypt dictionary entry.
    /// </summary>
    private static void CreateMinimalEncryptedPdf(string path)
    {
        // Minimal PDF with Encrypt dictionary that causes PdfPig to throw
        // an exception containing "password" or "encrypt" when opened.
        string pdfContent = """
            %PDF-1.4
            1 0 obj
            << /Type /Catalog /Pages 2 0 R >>
            endobj
            2 0 obj
            << /Type /Pages /Kids [3 0 R] /Count 1 >>
            endobj
            3 0 obj
            << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>
            endobj
            4 0 obj
            << /Type /XRef /Size 5 /W [1 2 1] /Root 1 0 R /Encrypt 5 0 R >>
            endobj
            5 0 obj
            << /Filter /Standard /V 1 /R 2 /O (12345678901234567890123456789012) /U (12345678901234567890123456789012) /P -4 >>
            endobj
            xref
            0 6
            0000000000 65535 f 
            0000000009 00000 n 
            0000000058 00000 n 
            0000000115 00000 n 
            0000000190 00000 n 
            0000000287 00000 n 
            trailer
            << /Size 6 /Root 1 0 R /Encrypt 5 0 R >>
            startxref
            420
            %%EOF
            """;

        File.WriteAllText(path, pdfContent);
    }
}
