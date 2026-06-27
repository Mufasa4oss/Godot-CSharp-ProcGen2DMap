# Trade System — Design Vision

> Status: **design intent**, not yet implemented — the macro vision behind the M6 logistics milestone.
> Sits on the principles in [`EconomyDesign.md`](EconomyDesign.md). Goods are Grain / Ore / Tools. Locked with
> the user 2026-06-27.

## The problem this answers

A nation can spawn lacking a good. That should be a *strategic pressure* — trade for it, or be squeezed by
whoever controls it — not a dead end. For that, a monopoly needs a **victim** (someone who needs the good), a
**channel** (a way to acquire it), and a controller who can **set the price or withhold it**. Today none exist
(AI are inert; selling is to an infinite abstract market). This system makes scarcity and monopoly matter —
**without the tedium that kills trade systems.**

## How real trade works (the model we borrow)

Trade is **arbitrage under friction**: goods flow abundant/cheap → scarce/expensive; the price gap is the
engine and merchants chase it automatically (nobody orders each shipment). The brake is **transport cost +
capacity + risk** — a good moves only if `price_gap > cost`, which is the entire spatial game. Regions
specialize by **comparative advantage**; **chokepoints + infrastructure** are where leverage lives (tolls,
blockades); **monopoly** = control of a scarce resource *or the only route to it*. Crucially: trade is
**automatic at the micro level** but **political and spatial at the macro level** — governments shape the
*conditions* of trade (ports, tariffs, blockades, diplomacy, monopolies); they don't ship the crates.

## Reference points (take / avoid)

- **EU4 — take the spatial-control puzzle:** a contest over a network of nodes/chokepoints; goods abstracted so
  there's *no per-commodity micro*; fun = dominating routes. **Avoid:** trade is divorced from production
  (never models "I have ore, you need ore"); monopolies are over *routes*, not *resources*.
- **Victoria 3 — take real commodities + supply/demand prices. Avoid:** trade so automated you "set a route and
  watch" — low agency.

Target the gap: **Vicky's real commodities + self-clearing prices, made into an EU4-style contested *spatial*
puzzle, with agency kept high because the network and policy are player-built.**

## The model: automate the micro, make the macro yours

The market clears itself — goods auto-flow high → low price along the MST trunk + connector roads, throttled by
**edge capacity** and **transport cost**. **No buy/sell orders, ever.** Agency lives in durable strategic acts
(the levers below). *You play the government that shapes trade, not the merchant who executes it.*

### Player levers (priority order)
1. **Specialty / development — TOP PRIORITY.** Choose what your nation produces and develops; lean into or
   against your geographic hand (comparative advantage). The #1 driver of the trade system — *what you make*
   decides what you sell, what you must import, and where your leverage is.
2. **Chokepoints & blockades.** Own the tile a flow must cross → leverage: tax it, or cut it to **starve a
   rival**. The EU4 chokepoint contest and the basis for an **economic casus belli**.
3. **Build / upgrade routes.** Connector roads + edge capacity decide *where* trade can flow and how much; a
   deposit far from the network is worth less (geometry, per [`../../DesignNotes.md`](../../DesignNotes.md)).
4. **Tariffs / tolls.** Tax goods crossing your land — steady income plus a policy lever short of war.
5. **Diplomacy / relations.** Diplomats improve relations with another nation; once friendly, **economic
   benefits flow** (better prices, lower/zero tariffs, route access), while neutral or hostile relations
   **raise prices and restrict trade**. Relations modulate the *terms* of the auto-flow — a non-military way to
   open or choke an economy.

### Anti-tedium principles
- **Never per-transaction micro** — the market resolves itself.
- **Decisions are durable acts** (develop a specialty, build a road, set a tariff, send a diplomat), not
  per-tick fiddling.
- **Invest in legibility** — a glowing flow/price map *is* half the fun. The player sets conditions; the
  simulation resolves them.

## How monopoly emerges
With **mixed-by-good** specialization, common goods flow cheaply everywhere (no dead-ends), while the **rare
strategic good** is the contested prize. With **AI producers**, supply is multi-source, so controlling the
strategic good means everyone's demand pulls *your* price up (pricing power) and you can **withhold it** (cut
the route, or freeze a hostile nation out diplomatically) — a monopoly with teeth, and a reason to break one by
force.

## Why this fits *this* game
The same physical network that auto-flows trade is what armies march on and what nations fight over —
EU4 chokepoints + Vicky commodities + Factorio "you build the network", unified on one map (the locked
differentiator: supply lines carry econ *and* army; cut the road, the army starves).

## Build order (phased; M6, enriched)
1. **Connected auto-arbitrage** — goods flow high → low price between network-connected nations; disconnected =
   no trade. (Needs per-nation stockpiles + AI producers + AI demand.) — ✅ **shipped 2026-06-27** (M6
   phase-1, Build-History): AI come alive (pops + greedy extractor auto-build + tax), per-nation scarcity
   prices, one-unit/route/tick arbitrage along the MST at the midpoint price with a distance-scaled transport
   cost; a sole supplier earns gold from connected consumers.
2. **Capacity + connector roads** — edge capacity + player/AI-built connectors; transport cost = path length.
   *(next)*
3. **Political + diplomatic levers** — tariffs/tolls, then chokepoint control + blockades, plus diplomacy
   modulating terms. Specialty/development runs throughout (and leads).

## Open questions
- The **strategic good**: what is it, what needs it (a pop luxury? a military input?), how rare?
- **Price scope**: one clearing price among connected nations, or a true per-node gradient goods chase?
- **Transport cost shape**: flat per-edge, per-tile distance, or congestion-based?
- **Diplomacy depth**: a single relations scalar modulating terms, or treaties/embargoes/trade pacts?
- **Blockade granularity**: cut a whole edge, or contest individual tiles?
- How much **AI trade is automated** vs. visibly reactive (so the player can read and exploit it)?

## Related
- [`EconomyDesign.md`](EconomyDesign.md) — the resources/transformations/scarcity + money-flow principles this
  sits on.
- [`../../DesignNotes.md`](../../DesignNotes.md) — hub-and-spoke / MST geometry.
- [`../M-Doc/L1-Milestone-Roadmap.md`](../M-Doc/L1-Milestone-Roadmap.md) — M6 Logistics.
