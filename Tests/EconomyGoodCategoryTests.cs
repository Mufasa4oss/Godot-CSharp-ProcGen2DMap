using System;
using Xunit;

// M4.5 sub-rung 1: every good reports a raw/refined category. Raw goods (Grain, Ore) are
// extraction-gated; refined goods (Tools) are build-gated. Later sub-rungs diverge on this
// category — here we just assert the mapping holds. Plain .NET (no engine).
public class EconomyGoodCategoryTests
{
    [Fact]
    public void Grain_and_Ore_are_raw()
    {
        Assert.Equal(GoodCategory.Raw, Good.Grain.Category());
        Assert.Equal(GoodCategory.Raw, Good.Ore.Category());
    }

    [Fact]
    public void Tools_is_refined()
    {
        Assert.Equal(GoodCategory.Refined, Good.Tools.Category());
    }

    [Fact]
    public void Every_good_reports_a_defined_category()
    {
        foreach (Good good in Enum.GetValues<Good>())
            Assert.True(Enum.IsDefined(good.Category()),
                $"{good} must map to a defined GoodCategory");
    }
}
