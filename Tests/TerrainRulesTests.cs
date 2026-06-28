using Xunit;

// Locks the water/beach/desert/grass bands in TerrainRules (pure classification from height +
// moisture). FastNoiseLite.GetNoise2D returns ~[-1, 1].
public class TerrainRulesTests
{
    [Theory]
    [InlineData(-1.0f)]
    [InlineData(-0.5f)]
    [InlineData(0.0f)]
    [InlineData(TerrainRules.WaterLevel)] // boundary: <= WaterLevel is water
    public void At_or_below_water_level_is_water_for_any_moisture(float h)
    {
        // Below the waterline is water even when bone-dry — water is never a beach.
        Assert.Equal(TerrainType.Water, TerrainRules.Classify(h, -1.0f));
        Assert.Equal(TerrainType.Water, TerrainRules.Classify(h, 1.0f));
        Assert.False(TerrainRules.IsBeach(h));
    }

    [Theory]
    [InlineData(0.11f)]
    [InlineData(0.13f)]
    public void Low_coast_band_is_beach_sand_for_any_moisture(float h)
    {
        Assert.Equal(TerrainType.Sand, TerrainRules.Classify(h, 1.0f));  // wet coast still sandy
        Assert.Equal(TerrainType.Sand, TerrainRules.Classify(h, -1.0f)); // dry coast still sandy
        Assert.True(TerrainRules.IsBeach(h));
    }

    [Fact]
    public void Dry_inland_is_desert_sand_not_beach()
    {
        const float inland = 0.5f; // well above BeachLine
        Assert.Equal(TerrainType.Sand, TerrainRules.Classify(inland, TerrainRules.DryThreshold - 0.01f));
        Assert.False(TerrainRules.IsBeach(inland));
    }

    [Theory]
    [InlineData(1.0f)]
    [InlineData(0.0f)]
    public void Wet_inland_is_grass(float moisture)
    {
        const float inland = 0.5f;
        Assert.Equal(TerrainType.Grass, TerrainRules.Classify(inland, moisture));
    }

    [Fact]
    public void IsBeach_only_within_the_low_band()
    {
        Assert.False(TerrainRules.IsBeach(TerrainRules.WaterLevel)); // water edge, not beach
        Assert.True(TerrainRules.IsBeach((TerrainRules.WaterLevel + TerrainRules.BeachLine) / 2f));
        Assert.False(TerrainRules.IsBeach(TerrainRules.BeachLine));  // top of band is out
        Assert.False(TerrainRules.IsBeach(0.9f));
    }

    [Fact]
    public void Band_ordering_is_sane()
    {
        Assert.True(TerrainRules.WaterLevel < TerrainRules.BeachLine);
        Assert.InRange(TerrainRules.DryThreshold, -1.0f, 1.0f);
    }
}
