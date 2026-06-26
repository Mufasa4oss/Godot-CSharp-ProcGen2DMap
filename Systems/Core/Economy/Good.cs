using System;

// The L1 economy runs on three goods. More are added in later layers.
public enum Good
{
    Grain,
    Ore,
    Tools
}

// M4.5: every good is either Raw or Refined, and the category carries behaviour the later
// sub-rungs diverge on:
//   Raw     = extraction-gated — only flows from a Mine/Farm built on a deposit (Grain, Ore).
//   Refined = build-gated      — only produced by a player-built Workshop (Tools).
// Sub-rung 1 just defines and reports the category; build-gating/throttle land in sub-rungs 2–3.
public enum GoodCategory
{
    Raw,
    Refined
}

public static class GoodExtensions
{
    public static GoodCategory Category(this Good good) => good switch
    {
        Good.Grain => GoodCategory.Raw,
        Good.Ore   => GoodCategory.Raw,
        Good.Tools => GoodCategory.Refined,
        _ => throw new ArgumentOutOfRangeException(nameof(good), good, null)
    };
}
