using RapidOCRSchedule.Core;

namespace RapidOCRSchedule.Tests;

public sealed class EmployeeNameResolverTests
{
    [Fact]
    public void Resolve_ConvertsTraditionalNameToConfiguredSimplifiedName()
    {
        EmployeeNameResolver resolver = new(["姚旭杰"]);

        string resolved = resolver.Resolve("姚旭傑");

        Assert.Equal("姚旭杰", resolved);
    }

    [Fact]
    public void Resolve_UsesUniqueOneEditRosterMatch()
    {
        EmployeeNameResolver resolver = new(["姚旭杰"]);

        string resolved = resolver.Resolve("姚旭桀");

        Assert.Equal("姚旭杰", resolved);
    }

    [Fact]
    public void Resolve_DoesNotGuessWhenOneEditMatchIsAmbiguous()
    {
        EmployeeNameResolver resolver = new(["姚旭杰", "姚旭洁"]);

        string resolved = resolver.Resolve("姚旭桀");

        Assert.Equal("姚旭桀", resolved);
    }

    [Fact]
    public void Resolve_DoesNotFuzzyMatchTwoCharacterName()
    {
        EmployeeNameResolver resolver = new(["张三"]);

        string resolved = resolver.Resolve("张山");

        Assert.Equal("张山", resolved);
    }

    [Fact]
    public void Resolve_KeepsTraditionalCharactersWhenConversionIsDisabled()
    {
        EmployeeNameResolver resolver = new([], convertTraditionalToSimplified: false);

        string resolved = resolver.Resolve("姚旭傑");

        Assert.Equal("姚旭傑", resolved);
    }

    [Fact]
    public void NormalizeEmployeeNames_SimplifiesTrimsAndRemovesDuplicates()
    {
        IReadOnlyList<string> names = EmployeeNameResolver.NormalizeEmployeeNames(
            [" 姚旭傑 ", "姚旭杰", "", " 张三 "]);

        Assert.Equal(["姚旭杰", "张三"], names);
    }

    [Fact]
    public void NormalizeEmployeeNames_KeepsTraditionalCharactersWhenConversionIsDisabled()
    {
        IReadOnlyList<string> names = EmployeeNameResolver.NormalizeEmployeeNames(
            ["姚旭傑", "姚旭杰"],
            convertTraditionalToSimplified: false);

        Assert.Equal(["姚旭傑", "姚旭杰"], names);
    }
}
