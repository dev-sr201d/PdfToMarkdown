using FluentAssertions;
using Xunit;

namespace PdfToMarkdown.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder_ShouldPass()
    {
        true.Should().BeTrue();
    }
}
