using FluentAssertions;
using ModelContextProtocol;
using PdfToMarkdown.Services;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Unit tests for <see cref="InputValidator"/>.
/// </summary>
public class InputValidatorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly InputValidator _sut = new();

    public InputValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "InputValidatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateAsync_ValidPdfPath_DoesNotThrow()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "valid.pdf");
        await File.WriteAllBytesAsync(path, [0x25, 0x50, 0x44, 0x46]); // %PDF header

        // Act
        Func<Task> act = () => _sut.ValidateAsync(path);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAsync_NullPath_ThrowsMcpException()
    {
        // Act
        Func<Task> act = () => _sut.ValidateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*No file path*");
    }

    [Fact]
    public async Task ValidateAsync_EmptyPath_ThrowsMcpException()
    {
        // Act
        Func<Task> act = () => _sut.ValidateAsync("");

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*No file path*");
    }

    [Fact]
    public async Task ValidateAsync_WhitespacePath_ThrowsMcpException()
    {
        // Act
        Func<Task> act = () => _sut.ValidateAsync("   ");

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*No file path*");
    }

    [Fact]
    public async Task ValidateAsync_WrongExtension_ThrowsMcpException()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "file.txt");
        await File.WriteAllTextAsync(path, "content");

        // Act
        Func<Task> act = () => _sut.ValidateAsync(path);

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*Expected*pdf*");
    }

    [Fact]
    public async Task ValidateAsync_NoExtension_ThrowsMcpException()
    {
        // Act
        Func<Task> act = () => _sut.ValidateAsync(Path.Combine(_tempDir, "noextension"));

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*Expected*pdf*");
    }

    [Theory]
    [InlineData(".PDF")]
    [InlineData(".Pdf")]
    [InlineData(".pDf")]
    public async Task ValidateAsync_CaseInsensitiveExtension_Passes(string extension)
    {
        // Arrange
        string path = Path.Combine(_tempDir, $"file{extension}");
        await File.WriteAllBytesAsync(path, [0x25, 0x50, 0x44, 0x46]);

        // Act
        Func<Task> act = () => _sut.ValidateAsync(path);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAsync_NonexistentFile_ThrowsMcpException()
    {
        // Arrange
        string path = Path.Combine(_tempDir, "nonexistent.pdf");

        // Act
        Func<Task> act = () => _sut.ValidateAsync(path);

        // Assert
        (await act.Should().ThrowAsync<McpException>())
            .Which.Message.Should().Contain(path);
    }

    [Fact]
    public async Task ValidateAsync_EmptyPathStopsBeforeFileCheck_NoFileCreated()
    {
        // Act — empty path should fail on the first check, never reaching file existence
        Func<Task> act = () => _sut.ValidateAsync("");

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*No file path*");
    }
}
