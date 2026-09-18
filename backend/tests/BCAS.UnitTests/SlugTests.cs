using BCAS.Application.Common;
using Xunit;

namespace BCAS.UnitTests;

public class SlugTests
{
    [Theory]
    [InlineData("BCAS Enrollment is Open!", "bcas-enrollment-is-open")]
    [InlineData("  Spaces   everywhere  ", "spaces-everywhere")]
    [InlineData("Señor Núñez", "senor-nunez")]
    [InlineData("A/B & C", "a-b-c")]
    [InlineData("!!!", "")]
    [InlineData("", "")]
    public void From_produces_a_url_safe_slug(string input, string expected)
    {
        Assert.Equal(expected, Slug.From(input));
    }
}
