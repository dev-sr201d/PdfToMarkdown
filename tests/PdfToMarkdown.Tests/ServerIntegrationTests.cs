using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Integration tests that verify the MCP server starts correctly, responds
/// to protocol messages, and shuts down gracefully (FRD-001).
/// All tests can execute independently without relying on test order.
/// </summary>
[Trait("Category", "Integration")]
public class ServerIntegrationTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _fixture = fixture;

    [Fact]
    public void Server_WhenLaunched_StartsSuccessfullyAndRemainsRunning()
    {
        _fixture.Process.HasExited.Should().BeFalse(
            "the server should remain running after launch");
    }

    [Fact]
    public async Task Server_WhenLaunched_StdoutContainsNoDiagnosticOutput()
    {
        string? startupPayload = await ReadJsonPayloadWithTimeoutAsync(
            _fixture.Process.StandardOutput.BaseStream,
            TimeSpan.FromMilliseconds(250));

        startupPayload.Should().BeNull(
            "stdout is reserved for MCP JSON-RPC messages and should contain no diagnostic output on startup");
    }

    [Fact]
    public async Task Server_WhenSentInitializeRequest_ReturnsValidJsonRpcResponse()
    {
        var initializeRequest = new
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
        };

        string requestJson = JsonSerializer.Serialize(initializeRequest);

        await _fixture.Process.StandardInput.WriteLineAsync(requestJson);
        await _fixture.Process.StandardInput.FlushAsync();

        string? responsePayload = await ReadJsonPayloadWithTimeoutAsync(
            _fixture.Process.StandardOutput.BaseStream,
            TimeSpan.FromSeconds(10));

        responsePayload.Should().NotBeNullOrEmpty("the server should respond to an initialize request");

        using JsonDocument responseDoc = JsonDocument.Parse(responsePayload!);
        JsonElement root = responseDoc.RootElement;

        root.GetProperty("jsonrpc").GetString().Should().Be("2.0", "response must be JSON-RPC 2.0");
        root.GetProperty("id").GetInt32().Should().Be(1, "response id must match request id");
        root.TryGetProperty("result", out JsonElement result).Should().BeTrue("response must contain a result");
        result.TryGetProperty("serverInfo", out _).Should().BeTrue("result must contain serverInfo");
        result.TryGetProperty("capabilities", out _).Should().BeTrue("result must contain capabilities");
    }

    [Fact]
    public async Task Server_WhenStdinClosed_ShutsDownGracefullyWithExitCodeZero()
    {
        using Process process = await _fixture.StartIndependentServerProcessAsync();

        process.StandardInput.Close();

        bool exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(10));
        exited.Should().BeTrue("the server should exit within 10 seconds after stdin is closed");
        process.ExitCode.Should().Be(0, "the server should exit gracefully with code 0");
    }

    private static async Task<string?> ReadJsonPayloadWithTimeoutAsync(Stream stream, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        byte[] buffer = new byte[4096];
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
        string text = System.Text.Encoding.UTF8.GetString(responseBytes);
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

        payload = System.Text.Encoding.UTF8.GetString(responseBytes, bodyStartIndex, contentLength.Value);
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

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        try
        {
            using CancellationTokenSource cts = new(timeout);
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
