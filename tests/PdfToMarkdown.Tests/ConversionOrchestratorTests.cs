using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Unit tests for <see cref="ConversionOrchestrator"/>.
/// </summary>
public class ConversionOrchestratorTests
{
    private readonly IInputValidator _validator = Substitute.For<IInputValidator>();
    private readonly IPdfMarkdownConverter _converter = Substitute.For<IPdfMarkdownConverter>();
    private readonly IMarkdownWriter _writer = Substitute.For<IMarkdownWriter>();
    private readonly ConversionOrchestrator _sut;

    public ConversionOrchestratorTests()
    {
        _sut = new ConversionOrchestrator(_validator, _converter, _writer);
    }

    [Fact]
    public async Task ConvertAsync_StandardMode_CallsServicesInOrderAndReturnsConfirmation()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        string[] writtenPaths = [@"C:\docs\report.md"];
        _writer.FinalizeAsync(Arg.Any<CancellationToken>()).Returns(writtenPaths);

        // Act
        string result = await _sut.ConvertAsync(pdfPath, chunkByChapter: false);

        // Assert
        result.Should().Be(@"Converted: C:\docs\report.md");

        Received.InOrder(() =>
        {
            _validator.ValidateAsync(pdfPath, Arg.Any<CancellationToken>());
            _writer.InitializeAsync(pdfPath, false, Arg.Any<CancellationToken>());
            _converter.ConvertAsync(pdfPath, _writer, Arg.Any<CancellationToken>());
            _writer.FinalizeAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ConvertAsync_ChunkedMode_InitializesWriterInChunkedMode()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        string[] writtenPaths = [@"C:\docs\report_1.md", @"C:\docs\report_2.md"];
        _writer.FinalizeAsync(Arg.Any<CancellationToken>()).Returns(writtenPaths);

        // Act
        string result = await _sut.ConvertAsync(pdfPath, chunkByChapter: true);

        // Assert
        result.Should().StartWith("Converted into 2 files:");
        result.Should().Contain(@"C:\docs\report_1.md");
        result.Should().Contain(@"C:\docs\report_2.md");

        await _writer.Received(1).InitializeAsync(pdfPath, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task> act = () => _sut.ConvertAsync(@"C:\docs\report.pdf", false, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        await _validator.DidNotReceive().ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertAsync_ValidationFails_DoesNotCallConverter()
    {
        // Arrange
        string pdfPath = @"C:\docs\missing.pdf";
        _validator.ValidateAsync(pdfPath, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ModelContextProtocol.McpException("PDF file not found: " + pdfPath));

        // Act
        Func<Task> act = () => _sut.ConvertAsync(pdfPath, false);

        // Assert
        await act.Should().ThrowAsync<ModelContextProtocol.McpException>()
            .WithMessage("*not found*");
        await _converter.DidNotReceive().ConvertAsync(Arg.Any<string>(), Arg.Any<IMarkdownWriter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertAsync_ConverterThrows_WriterStillDisposed()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        _converter.ConvertAsync(pdfPath, _writer, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ModelContextProtocol.McpException("Conversion failed"));

        // Act
        Func<Task> act = () => _sut.ConvertAsync(pdfPath, false);

        // Assert
        await act.Should().ThrowAsync<ModelContextProtocol.McpException>();
        await _writer.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ConvertAsync_SingleFile_ReturnsCorrectFormat()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        string expectedPath = @"C:\docs\report.md";
        string[] expectedPaths = [expectedPath];
        _writer.FinalizeAsync(Arg.Any<CancellationToken>()).Returns(expectedPaths);

        // Act
        string result = await _sut.ConvertAsync(pdfPath, false);

        // Assert
        result.Should().Be($"Converted: {expectedPath}");
    }

    [Fact]
    public async Task ConvertAsync_MultipleFiles_ReturnsCorrectFormat()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        string[] writtenPaths = [@"C:\docs\report_1.md", @"C:\docs\report_2.md", @"C:\docs\report_3.md"];
        _writer.FinalizeAsync(Arg.Any<CancellationToken>()).Returns(writtenPaths);

        // Act
        string result = await _sut.ConvertAsync(pdfPath, true);

        // Assert
        string[] resultLines = result.Split(Environment.NewLine);
        resultLines[0].Should().Be("Converted into 3 files:");
        resultLines[1].Should().Be(@"C:\docs\report_1.md");
        resultLines[2].Should().Be(@"C:\docs\report_2.md");
        resultLines[3].Should().Be(@"C:\docs\report_3.md");
    }

    [Fact]
    public async Task ConvertAsync_ServiceCallOrder_CorrectSequence()
    {
        // Arrange
        string pdfPath = @"C:\docs\report.pdf";
        string[] scheduledPaths = [@"C:\docs\report.md"];
        _writer.FinalizeAsync(Arg.Any<CancellationToken>()).Returns(scheduledPaths);

        // Act
        await _sut.ConvertAsync(pdfPath, false);

        // Assert — validate → writer.init → convert → finalize
        Received.InOrder(() =>
        {
            _validator.ValidateAsync(pdfPath, Arg.Any<CancellationToken>());
            _writer.InitializeAsync(pdfPath, false, Arg.Any<CancellationToken>());
            _converter.ConvertAsync(pdfPath, _writer, Arg.Any<CancellationToken>());
            _writer.FinalizeAsync(Arg.Any<CancellationToken>());
        });
    }
}
