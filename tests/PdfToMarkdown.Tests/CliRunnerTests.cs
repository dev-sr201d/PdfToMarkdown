using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Unit tests for <see cref="CliRunner"/>.
/// </summary>
public class CliRunnerTests
{
    private readonly IConversionOrchestrator _orchestrator = Substitute.For<IConversionOrchestrator>();
    private readonly ILogger<CliRunner> _logger = NullLogger<CliRunner>.Instance;
    private readonly CliRunner _sut;

    public CliRunnerTests()
    {
        _sut = new CliRunner(_orchestrator, _logger);
    }

    [Fact]
    public async Task RunAsync_ValidPdfPath_ReturnsZeroExitCode()
    {
        // Arrange
        _orchestrator.ConvertAsync(@"C:\docs\report.pdf", false, Arg.Any<CancellationToken>())
            .Returns(@"Converted: C:\docs\report.md");

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        int exitCode = await _sut.RunAsync([@"C:\docs\report.pdf"], stdout, stderr);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_ValidPdfPath_PrintsFilePathToStdout()
    {
        // Arrange
        _orchestrator.ConvertAsync(@"C:\docs\report.pdf", false, Arg.Any<CancellationToken>())
            .Returns(@"Converted: C:\docs\report.md");

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        await _sut.RunAsync([@"C:\docs\report.pdf"], stdout, stderr);

        // Assert
        string output = stdout.ToString().Trim();
        output.Should().Be(@"C:\docs\report.md");
    }

    [Fact]
    public async Task RunAsync_ChunkedMode_PrintsMultiplePathsToStdout()
    {
        // Arrange
        string confirmation = "Converted into 3 files:\nC:\\docs\\report_1.md\nC:\\docs\\report_2.md\nC:\\docs\\report_3.md";
        _orchestrator.ConvertAsync(@"C:\docs\report.pdf", true, Arg.Any<CancellationToken>())
            .Returns(confirmation);

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        int exitCode = await _sut.RunAsync([@"C:\docs\report.pdf", "--chunk-by-chapter"], stdout, stderr);

        // Assert
        exitCode.Should().Be(0);
        string[] outputLines = stdout.ToString().Trim().Split(Environment.NewLine);
        outputLines.Should().HaveCount(3);
        outputLines[0].Should().Be(@"C:\docs\report_1.md");
        outputLines[1].Should().Be(@"C:\docs\report_2.md");
        outputLines[2].Should().Be(@"C:\docs\report_3.md");
    }

    [Fact]
    public async Task RunAsync_InvalidArgs_ReturnsNonZeroExitCode()
    {
        // Arrange
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        int exitCode = await _sut.RunAsync(["--unknown-flag"], stdout, stderr);

        // Assert
        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_InvalidArgs_WritesUsageToStderr()
    {
        // Arrange
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        await _sut.RunAsync(["--unknown-flag"], stdout, stderr);

        // Assert
        string errorOutput = stderr.ToString();
        errorOutput.Should().Contain("Usage:");
        stdout.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_NoArgs_WritesUsageToStderr()
    {
        // Arrange
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        int exitCode = await _sut.RunAsync([], stdout, stderr);

        // Assert
        exitCode.Should().Be(1);
        stderr.ToString().Should().Contain("Usage:");
    }

    [Fact]
    public async Task RunAsync_McpException_WritesMessageToStderrAndReturnsOne()
    {
        // Arrange
        _orchestrator.ConvertAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new McpException("PDF file not found: C:\\missing.pdf"));

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        int exitCode = await _sut.RunAsync([@"C:\missing.pdf"], stdout, stderr);

        // Assert
        exitCode.Should().Be(1);
        stderr.ToString().Should().Contain("PDF file not found");
        stdout.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_OperationCanceled_ReturnsNonZeroExitCode()
    {
        // Arrange
        _orchestrator.ConvertAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        int exitCode = await _sut.RunAsync([@"C:\docs\report.pdf"], stdout, stderr);

        // Assert
        exitCode.Should().Be(1);
        stderr.ToString().Should().Contain("cancelled");
    }

    [Fact]
    public async Task RunAsync_UnexpectedException_WritesGenericMessageToStderr()
    {
        // Arrange
        _orchestrator.ConvertAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("internal error details"));

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        int exitCode = await _sut.RunAsync([@"C:\docs\report.pdf"], stdout, stderr);

        // Assert
        exitCode.Should().Be(1);
        string errorOutput = stderr.ToString();
        errorOutput.Should().Contain("unexpected error");
        errorOutput.Should().NotContain("internal error details");
        stdout.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_PassesChunkFlagToOrchestrator()
    {
        // Arrange
        _orchestrator.ConvertAsync(Arg.Any<string>(), true, Arg.Any<CancellationToken>())
            .Returns(@"Converted: C:\docs\report_1.md");

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        await _sut.RunAsync([@"C:\docs\report.pdf", "--chunk-by-chapter"], stdout, stderr);

        // Assert
        await _orchestrator.Received(1).ConvertAsync(@"C:\docs\report.pdf", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_NoChunkFlag_PassesFalseToOrchestrator()
    {
        // Arrange
        _orchestrator.ConvertAsync(Arg.Any<string>(), false, Arg.Any<CancellationToken>())
            .Returns(@"Converted: C:\docs\report.md");

        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        await _sut.RunAsync([@"C:\docs\report.pdf"], stdout, stderr);

        // Assert
        await _orchestrator.Received(1).ConvertAsync(@"C:\docs\report.pdf", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_InvalidArgs_DoesNotCallOrchestrator()
    {
        // Arrange
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        // Act
        await _sut.RunAsync(["--bad-flag"], stdout, stderr);

        // Assert
        await _orchestrator.DidNotReceive().ConvertAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
