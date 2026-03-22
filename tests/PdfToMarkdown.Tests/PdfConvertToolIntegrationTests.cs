using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Integration tests that verify the PdfConvertTool is correctly wired
/// into the MCP server, discoverable by clients, and exposes the expected
/// parameter schema (FRD-002).
/// Each test uses its own server process to avoid shared state issues.
/// </summary>
[Trait("Category", "Integration")]
public class PdfConvertToolIntegrationTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _fixture = fixture;

    [Fact]
    public async Task ToolsList_WhenQueried_ContainsPdfConvertTool()
    {
        // Arrange — start a fresh server process for this test
        using Process process = await _fixture.StartIndependentServerProcessAsync();
        try
        {
            using JsonDocument _ = await InitializeServerAsync(process);

            // Act — request tools list
            using JsonDocument toolsResponse = await SendRequestAsync(process, new
            {
                jsonrpc = "2.0",
                id = 10,
                method = "tools/list",
                @params = new { }
            });

            // Assert
            JsonElement result = toolsResponse.RootElement.GetProperty("result");
            result.TryGetProperty("tools", out JsonElement tools).Should().BeTrue("result must contain tools array");
            tools.GetArrayLength().Should().BeGreaterThan(0, "at least one tool should be registered");

            JsonElement? convertTool = FindConvertTool(tools);
            convertTool.Should().NotBeNull("the PDF conversion tool must be registered");
            convertTool!.Value.GetProperty("description").GetString().Should().NotBeNullOrWhiteSpace(
                "the tool must have a meaningful description");
        }
        finally
        {
            ShutdownProcess(process);
        }
    }

    [Fact]
    public async Task ToolsList_PdfConvertTool_HasCorrectParameterSchema()
    {
        // Arrange
        using Process process = await _fixture.StartIndependentServerProcessAsync();
        try
        {
            using JsonDocument _ = await InitializeServerAsync(process);

            // Act
            using JsonDocument toolsResponse = await SendRequestAsync(process, new
            {
                jsonrpc = "2.0",
                id = 11,
                method = "tools/list",
                @params = new { }
            });

            // Assert — find our tool
            JsonElement tools = toolsResponse.RootElement.GetProperty("result").GetProperty("tools");
            JsonElement? convertTool = FindConvertTool(tools);
            convertTool.Should().NotBeNull();

            JsonElement inputSchema = convertTool!.Value.GetProperty("inputSchema");

            // Verify pdfPath parameter
            JsonElement properties = inputSchema.GetProperty("properties");
            properties.TryGetProperty("pdfPath", out JsonElement pdfPathProp).Should().BeTrue(
                "pdfPath parameter must exist");
            pdfPathProp.GetProperty("type").GetString().Should().Be("string");
            pdfPathProp.TryGetProperty("description", out JsonElement pdfPathDesc).Should().BeTrue();
            pdfPathDesc.GetString().Should().NotBeNullOrWhiteSpace();

            // Verify chunkByChapter parameter
            properties.TryGetProperty("chunkByChapter", out JsonElement chunkProp).Should().BeTrue(
                "chunkByChapter parameter must exist");
            chunkProp.GetProperty("type").GetString().Should().Be("boolean");
            chunkProp.TryGetProperty("description", out JsonElement chunkDesc).Should().BeTrue();
            chunkDesc.GetString().Should().NotBeNullOrWhiteSpace();

            // Verify pdfPath is required
            inputSchema.TryGetProperty("required", out JsonElement required).Should().BeTrue(
                "required array must exist");
            bool pdfPathRequired = required.EnumerateArray().Any(r => r.GetString() == "pdfPath");
            pdfPathRequired.Should().BeTrue("pdfPath must be a required parameter");

            // Verify chunkByChapter is NOT required
            bool chunkRequired = required.EnumerateArray().Any(r => r.GetString() == "chunkByChapter");
            chunkRequired.Should().BeFalse("chunkByChapter must be optional");
        }
        finally
        {
            ShutdownProcess(process);
        }
    }

    [Fact]
    public async Task ToolsCall_WithNonExistentPath_ReturnsErrorResult()
    {
        // Arrange
        using Process process = await _fixture.StartIndependentServerProcessAsync();
        try
        {
            using JsonDocument initResponse = await InitializeServerAsync(process);

            // Send initialized notification (required before calling tools)
            await SendNotificationAsync(process, new
            {
                jsonrpc = "2.0",
                method = "notifications/initialized"
            });

            // Get tool name from tools/list first
            using JsonDocument toolsResponse = await SendRequestAsync(process, new
            {
                jsonrpc = "2.0",
                id = 20,
                method = "tools/list",
                @params = new { }
            });

            JsonElement tools = toolsResponse.RootElement.GetProperty("result").GetProperty("tools");
            JsonElement? convertTool = FindConvertTool(tools);
            convertTool.Should().NotBeNull();
            string toolName = convertTool!.Value.GetProperty("name").GetString()!;

            // Act — call the tool with a path that does not exist
            using JsonDocument callResponse = await SendRequestAsync(process, new
            {
                jsonrpc = "2.0",
                id = 12,
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments = new
                    {
                        pdfPath = @"C:\nonexistent\path\fake.pdf"
                    }
                }
            });

            // Assert — the response should indicate an error
            JsonElement root = callResponse.RootElement;

            if (root.TryGetProperty("result", out JsonElement result))
            {
                // MCP SDK wraps tool errors in result with isError=true
                result.TryGetProperty("isError", out JsonElement isError).Should().BeTrue();
                isError.GetBoolean().Should().BeTrue("the tool should report an error for a non-existent path");

                result.TryGetProperty("content", out JsonElement content).Should().BeTrue();
                content.GetArrayLength().Should().BeGreaterThan(0);

                string? errorText = content[0].GetProperty("text").GetString();
                errorText.Should().NotBeNullOrWhiteSpace("error message must be non-empty");
                errorText.Should().NotContain("StackTrace", "error should not expose stack traces");
            }
            else if (root.TryGetProperty("error", out JsonElement error))
            {
                // JSON-RPC error response — also acceptable
                error.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                true.Should().BeFalse("response must contain either 'result' or 'error'");
            }
        }
        finally
        {
            ShutdownProcess(process);
        }
    }

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
        // Brief delay to let the server process the notification
        await Task.Delay(100);
    }

    private static async Task<JsonDocument> SendRequestAsync(Process process, object request)
    {
        string json = JsonSerializer.Serialize(request);
        await process.StandardInput.WriteLineAsync(json);
        await process.StandardInput.FlushAsync();

        string? response = await ReadJsonPayloadWithTimeoutAsync(
            process.StandardOutput.BaseStream,
            TimeSpan.FromSeconds(10));

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
