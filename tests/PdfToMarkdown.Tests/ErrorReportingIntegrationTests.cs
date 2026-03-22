using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// MCP protocol-level integration tests that verify FRD-010 error scenarios
/// return properly formatted error results through the JSON-RPC protocol.
/// Each test starts an independent server process and validates the response format.
/// All tests use standard mode — no dependency on FRD-009 (chapter-based chunking).
/// </summary>
[Trait("Category", "Integration")]
public class ErrorReportingIntegrationTests(ServerFixture fixture) : IClassFixture<ServerFixture>, IAsyncLifetime
{
    private readonly ServerFixture _fixture = fixture;
    private string _testDir = "";

    public Task InitializeAsync()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ErrorMcp_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); } catch { /* best-effort */ }
        }

        return Task.CompletedTask;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Empty / Missing Path
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_EmptyPath_ReturnsErrorResultWithPathMessage()
    {
        using Process process = await _fixture.StartIndependentServerProcessAsync();
        try
        {
            string errorText = await CallToolAndGetErrorTextAsync(process, pdfPath: "");

            errorText.Should().NotBeNullOrWhiteSpace();
            errorText.Should().Contain("path", "should mention that a path is required");
            AssertNoInternalDetailsLeaked(errorText);
        }
        finally
        {
            ShutdownProcess(process);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    //  Wrong File Extension
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_WrongExtension_ReturnsErrorResultMentioningPdf()
    {
        // Create a .txt file on disk so it passes the "file exists" check
        string txtPath = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(txtPath, "not a pdf");

        using Process process = await _fixture.StartIndependentServerProcessAsync();
        try
        {
            string errorText = await CallToolAndGetErrorTextAsync(process, pdfPath: txtPath);

            errorText.Should().NotBeNullOrWhiteSpace();
            errorText.Should().Contain(".pdf", "should mention the expected .pdf extension");
            errorText.Should().Contain(txtPath, "should include the file path");
            AssertNoInternalDetailsLeaked(errorText);
        }
        finally
        {
            ShutdownProcess(process);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    //  Non-Existent File
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_NonExistentFile_ReturnsErrorResultWithPathAndNotFound()
    {
        string fakePath = Path.Combine(_testDir, "nonexistent.pdf");

        using Process process = await _fixture.StartIndependentServerProcessAsync();
        try
        {
            string errorText = await CallToolAndGetErrorTextAsync(process, pdfPath: fakePath);

            errorText.Should().NotBeNullOrWhiteSpace();
            errorText.Should().Contain(fakePath, "should include the file path");
            errorText.Should().Contain("not found", "should state the file was not found");
            AssertNoInternalDetailsLeaked(errorText);
        }
        finally
        {
            ShutdownProcess(process);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    //  No-Text PDF
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_NoTextPdf_ReturnsErrorResultMentioningNoText()
    {
        // Generate a no-text PDF fixture
        string noTextPath = Path.Combine(_testDir, "no-text.pdf");
        GenerateNoTextPdf(noTextPath);

        using Process process = await _fixture.StartIndependentServerProcessAsync();
        try
        {
            string errorText = await CallToolAndGetErrorTextAsync(process, pdfPath: noTextPath);

            errorText.Should().NotBeNullOrWhiteSpace();
            errorText.Should().Contain("no extractable text", "should describe the no-text problem");
            errorText.Should().Contain(noTextPath, "should include the file path");
            AssertNoInternalDetailsLeaked(errorText);
        }
        finally
        {
            ShutdownProcess(process);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers — MCP Protocol Communication
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the server, discovers the tool, calls it with the given pdfPath,
    /// and returns the error text from the response. Fails the test if the response
    /// is not an error result.
    /// </summary>
    private static async Task<string> CallToolAndGetErrorTextAsync(Process process, string pdfPath)
    {
        using JsonDocument _ = await InitializeServerAsync(process);

        await SendNotificationAsync(process, new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized"
        });

        // Discover tool name
        using JsonDocument toolsResponse = await SendRequestAsync(process, new
        {
            jsonrpc = "2.0",
            id = 10,
            method = "tools/list",
            @params = new { }
        });

        JsonElement tools = toolsResponse.RootElement.GetProperty("result").GetProperty("tools");
        JsonElement? convertTool = FindConvertTool(tools);
        convertTool.Should().NotBeNull("the PDF conversion tool must be registered");
        string toolName = convertTool!.Value.GetProperty("name").GetString()!;

        // Call the tool
        using JsonDocument callResponse = await SendRequestAsync(process, new
        {
            jsonrpc = "2.0",
            id = 20,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments = new
                {
                    pdfPath,
                    chunkByChapter = false
                }
            }
        });

        // Extract and return error text
        JsonElement root = callResponse.RootElement;

        if (root.TryGetProperty("result", out JsonElement result))
        {
            result.TryGetProperty("isError", out JsonElement isError).Should().BeTrue(
                "the result must have isError property");
            isError.GetBoolean().Should().BeTrue("the tool should report an error");

            result.TryGetProperty("content", out JsonElement content).Should().BeTrue(
                "the result must have content");
            content.GetArrayLength().Should().BeGreaterThan(0,
                "the content array must have at least one element");

            string? errorText = content[0].GetProperty("text").GetString();
            errorText.Should().NotBeNullOrWhiteSpace("error text must be non-empty");
            return errorText!;
        }

        if (root.TryGetProperty("error", out JsonElement error))
        {
            string? message = error.GetProperty("message").GetString();
            message.Should().NotBeNullOrWhiteSpace();
            return message!;
        }

        throw new InvalidOperationException("Response must contain either 'result' or 'error'");
    }

    /// <summary>
    /// Asserts that the error text does not leak internal implementation details.
    /// </summary>
    private static void AssertNoInternalDetailsLeaked(string errorText)
    {
        errorText.Should().NotContain("StackTrace", "must not contain stack trace property names");
        errorText.Should().NotContain("at System.", "must not contain .NET stack trace frames");
        errorText.Should().NotContain("at PdfToMarkdown.", "must not contain project stack trace frames");
        errorText.Should().NotMatchRegex(@"\.cs:line\s+\d+", "must not reference source file locations");
        errorText.Should().NotContain("IOException", "must not expose internal exception types");
        errorText.Should().NotContain("UnauthorizedAccessException", "must not expose internal exception types");
        errorText.Should().NotContain("NullReferenceException", "must not expose internal exception types");
        errorText.Should().NotContain("FileNotFoundException", "must not expose internal exception types");
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers — Test Fixtures
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a PDF with no text layer (only a drawn shape).
    /// </summary>
    private static void GenerateNoTextPdf(string path)
    {
        PdfDocumentBuilder builder = new();
        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        page.DrawRectangle(new PdfPoint(50, 400), 200, 200);
        File.WriteAllBytes(path, builder.Build());
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers — MCP Protocol (reused from PdfConvertToolIntegrationTests)
    // ────────────────────────────────────────────────────────────────────

    private static JsonElement? FindConvertTool(JsonElement tools)
    {
        foreach (JsonElement tool in tools.EnumerateArray())
        {
            string? name = tool.GetProperty("name").GetString();
            if (name is not null && name.Contains("onvert", StringComparison.OrdinalIgnoreCase))
            {
                return tool;
            }
        }

        return null;
    }

    private static void ShutdownProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.StandardInput.Close();
                if (!process.WaitForExit(2000))
                {
                    process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
    }

    private static async Task<JsonDocument> InitializeServerAsync(Process process)
    {
        return await SendRequestAsync(process, new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "test-client",
                    version = "1.0.0"
                }
            }
        });
    }

    private static async Task SendNotificationAsync(Process process, object notification)
    {
        string json = JsonSerializer.Serialize(notification);
        await process.StandardInput.WriteLineAsync(json);
        await process.StandardInput.FlushAsync();
        await Task.Delay(100);
    }

    private static async Task<JsonDocument> SendRequestAsync(Process process, object request)
    {
        string json = JsonSerializer.Serialize(request);
        await process.StandardInput.WriteLineAsync(json);
        await process.StandardInput.FlushAsync();

        string? response = await ReadJsonPayloadWithTimeoutAsync(
            process.StandardOutput.BaseStream,
            TimeSpan.FromSeconds(15));

        response.Should().NotBeNullOrEmpty("the server should respond to a request");
        return JsonDocument.Parse(response!);
    }

    private static async Task<string?> ReadJsonPayloadWithTimeoutAsync(Stream stream, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        byte[] buffer = new byte[8192];
        using MemoryStream bytes = new();

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, cts.Token);
                if (bytesRead == 0)
                {
                    return null;
                }

                bytes.Write(buffer, 0, bytesRead);
                if (TryExtractJsonPayload(bytes.ToArray(), out string? payload))
                {
                    return payload;
                }
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private static bool TryExtractJsonPayload(byte[] responseBytes, out string? payload)
    {
        payload = null;
        string text = Encoding.UTF8.GetString(responseBytes);
        int headerSeparatorIndex = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);

        if (headerSeparatorIndex < 0)
        {
            return TryParseDirectJson(text, out payload);
        }

        string headers = text[..headerSeparatorIndex];
        int? contentLength = TryReadContentLength(headers);
        if (contentLength is null)
        {
            return false;
        }

        int bodyStartIndex = headerSeparatorIndex + 4;
        if (responseBytes.Length < bodyStartIndex + contentLength.Value)
        {
            return false;
        }

        payload = Encoding.UTF8.GetString(responseBytes, bodyStartIndex, contentLength.Value);
        return true;
    }

    private static bool TryParseDirectJson(string text, out string? payload)
    {
        payload = text.Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            payload = null;
            return false;
        }

        try
        {
            using JsonDocument _ = JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            payload = null;
            return false;
        }
    }

    private static int? TryReadContentLength(string headers)
    {
        foreach (string header in headers.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            if (!header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = header["Content-Length:".Length..].Trim();
            if (int.TryParse(value, out int contentLength) && contentLength >= 0)
            {
                return contentLength;
            }
        }

        return null;
    }
}
