using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PdfToMarkdown.Services;
using PdfToMarkdown.Tools;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Unit tests for <see cref="PdfConvertTool"/>.
/// </summary>
public class PdfConvertToolTests
{
    private readonly IConversionOrchestrator _orchestrator = Substitute.For<IConversionOrchestrator>();

    [Fact]
    public async Task ConvertPdfToMarkdown_DelegatesToOrchestrator_ReturnsResult()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        string expected = @"Converted: C:\docs\report.md";
        _orchestrator.ConvertAsync(pdfPath, false, Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        string result = await PdfConvertTool.ConvertPdfToMarkdown(_orchestrator, pdfPath, false);

        // Assert
        result.Should().Be(expected);
        await _orchestrator.Received(1).ConvertAsync(pdfPath, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertPdfToMarkdown_ChunkedMode_PassesFlag()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        string expected = "Converted into 2 files:\nreport_1.md\nreport_2.md";
        _orchestrator.ConvertAsync(pdfPath, true, Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        string result = await PdfConvertTool.ConvertPdfToMarkdown(_orchestrator, pdfPath, true);

        // Assert
        result.Should().Be(expected);
        await _orchestrator.Received(1).ConvertAsync(pdfPath, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertPdfToMarkdown_OrchestratorThrowsMcpException_PropagatesUnchanged()
    {
        // Arrange
        var exception = new ModelContextProtocol.McpException("PDF file not found");
        _orchestrator.ConvertAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        // Act
        Func<Task> act = () => PdfConvertTool.ConvertPdfToMarkdown(_orchestrator, @"C:\missing.pdf");

        // Assert
        (await act.Should().ThrowAsync<ModelContextProtocol.McpException>())
            .Which.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task ConvertPdfToMarkdown_OrchestratorThrowsOperationCanceled_PropagatesUnchanged()
    {
        // Arrange
        _orchestrator.ConvertAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task> act = () => PdfConvertTool.ConvertPdfToMarkdown(_orchestrator, @"C:\docs\report.pdf");

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ConvertPdfToMarkdown_PassesCancellationToken()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
        _orchestrator.ConvertAsync(Arg.Any<string>(), Arg.Any<bool>(), token).Returns("ok");

        // Act
        await PdfConvertTool.ConvertPdfToMarkdown(_orchestrator, @"C:\docs\report.pdf", false, token);

        // Assert
        await _orchestrator.Received(1).ConvertAsync(@"C:\docs\report.pdf", false, token);
    }
}
