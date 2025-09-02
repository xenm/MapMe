using FluentAssertions;
using MapMe.Utils;
using Xunit;

namespace MapMe.Tests.Unit;

public class NormalizationTests
{
    [Fact]
    public void ToNorm_Removes_Diacritics_And_Punctuation_And_Lowercases()
    {
        var input = new[] { " Café ", "CAFE", "café", "cafe!" };
        var result = Normalization.ToNorm(input);
        result.Should().BeEquivalentTo(new[] { "cafe" });
    }

    [Fact]
    public void ToNorm_Filters_Empty_And_Duplicates()
    {
        var input = new[] { "  ", "tag", "Tag", "TAG" };
        var result = Normalization.ToNorm(input);
        result.Should().BeEquivalentTo(new[] { "tag" });
    }
}