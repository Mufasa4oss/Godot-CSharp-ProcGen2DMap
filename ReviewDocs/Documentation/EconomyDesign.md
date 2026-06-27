# Economy Design — Principles

> Status: **design intent / lens.** How to think about this game's economy so it produces *decisions*, not
> just numbers. The trade system ([`TradeDesign.md`](TradeDesign.md)) is one application of these principles.
> Goods today are Grain / Ore / Tools; deeper goods/chains are future. Locked with the user 2026-06-27.

## The triad: resources, transformations, scarcity

A sim economy is the interplay of three things, and the *ratio* between them is the game's rhythm:

- **Resources** — what the player holds: treasury (gold), per-good stockpiles (Grain/Ore/Tools), deposits +
  their finite **reserves**, **population** (pops), territory/tiles, and network/route access.
- **Transformations** — mechanics that convert one resource into another: **extract** (deposit → raw, via a
  built Mine/Farm), **refine** (Ore → Tools, via a Workshop), **sell** (goods → gold), **tax** (pops → gold),
  **trade** (surplus → others' gold via arbitrage), **build** (gold → infrastructure), and — future —
  **develop a specialty** and **diplomacy** (relations → better trade terms).
- **Scarcity** — the limiters that turn a transformation into a *decision*: build-gating (must build to
  extract), the extraction throttle (1 unit / 2 ticks), **finite deposit reserves**, a treasury that can't
  afford everything, **pop grain demand** (feed vs. sell), **edge capacity + route access** (who you can
  trade with), **AI/NPC demand** (what others will actually buy), and **diplomatic relations** (on what terms).

The test: every session should pose a fresh **"what first?"** — feed pops or sell grain? extract more raw or
refine it? build a route to a buyer or a new extractor? court a rival with diplomacy or blockade them? *That*
is the economy — decisions, not numbers.

## Money flow: income vs. sinks, and the curve

The most concrete face of the economy is gold in vs. gold out. Designed too generously or too tightly, the
player gets bored either way.

**Income (gold in):** selling goods (scales with production × scarcity price), the **pop tax** (steady base),
**trade income** (selling to AI buyers — scales with monopoly/pricing power), **tariffs/tolls** (route income).

**Money sinks (gold out) — the part this game still needs.** Today the only sink is *one-time* build costs
(Extractor 50, Workshop 100, Railroad 10/tile). Without **recurring** sinks the treasury bloats and gold
loses meaning. Planned recurring sinks:
- **Upkeep / maintenance** — buildings and routes degrade or cost per-tick to run.
- **Tariffs paid** — gold out when your goods cross another nation's territory.
- **Guild / certification dues** — recurring cost to reach premium (noble-equivalent) buyers / markets.
- **Diplomacy upkeep** — maintaining friendly relations costs something.
- **Reinvestment** — capacity, specialty, better buildings (the productive sink).

**The curve target: logarithmic.** Fast rise early (first extractor → sell → reinvest), steady growth mid
(refine + trade + specialize), a slowing late game **refreshed by new objectives** (monopolize the strategic
good, war, expansion). Avoid **linear** (monotonous) and **exponential** (late gold becomes meaningless).

**The threshold loop.** Keep the player permanently one step from affording the next meaningful thing (next
route, building, guild dues, breaking a monopoly); the moment they cross a threshold, a new one appears. A
periodic obligation (à la Recettear's debt deadline) can supply the pressure. **Money must never feel
meaningless** — neither so scarce it's a grind nor so abundant it doesn't matter. Tune via sinks + scarcity.

## How this guides the build
- Every new mechanic should add to *one* of the triad in a way that creates a decision against the other two.
- Before adding an income source, check there's a matching sink, or the curve bloats.
- Scarcity is the design surface: build-gating, throttle, finite reserves, capacity, demand, and diplomacy are
  the knobs that keep "what first?" alive.

## Related
- [`TradeDesign.md`](TradeDesign.md) — the trade system (the largest application of these principles).
- [`../../DesignNotes.md`](../../DesignNotes.md) — the spatial/MST model.
- [`../M-Doc/L1-Milestone-Roadmap.md`](../M-Doc/L1-Milestone-Roadmap.md) — the milestone ladder.
