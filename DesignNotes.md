# Game Design Notes — Economy Strategy Game

## Concept

Capitalism + Factorio hybrid. A map-based economic optimization game where geography
determines resource value, production chains create depth, and logistics is the core puzzle.

---

## Trade Network

> **Fuller vision:** the macro design of how trade should work — automate the micro, keep agency in the
> network/policy/production/diplomacy — lives in
> [`ReviewDocs/Documentation/TradeDesign.md`](ReviewDocs/Documentation/TradeDesign.md), which sits on the
> economy-design principles in
> [`ReviewDocs/Documentation/EconomyDesign.md`](ReviewDocs/Documentation/EconomyDesign.md). This section is the
> spatial/geometry layer those build on.

### MST as the Base Highway System

- The MST connecting all province capitals is the **permanent** base trade network.
- Pre-built, free to use, cannot be removed by anyone.
- Think interstate highways — they exist, you route through them.
- Hub provinces (capitals with many MST connections) are naturally more valuable real estate.

### Hub-and-Spoke Logistics

```
Capital A ──────────────── Capital B
              │
              │ ← short connector road (player/AI built)
           [Ore Mine]
```

- **MST edges** = trunk routes (the highways)
- **Connector roads** = on-ramps built by players or AI
- A deposit 2 tiles from the MST is worth more than one 20 tiles away.
  No explicit formula needed — it falls out of the geometry.

### Resource Value Gradient

Every resource tile has implicit value based on one question:
  *How far is it from the nearest MST edge?*

- Close to MST → cheap connector, fast ROI, contested early
- Far from MST → expensive connector, needs a high-yield deposit to justify

### Connector Road Rules

- Players and AI build connectors from resource deposits to the **nearest point on any MST edge**
  (not just capitals — mid-edge tapping like a highway on-ramp).
- This keeps connectors short and makes mid-map resources accessible.
- Controlling a hub province is strategically valuable: many connectors funnel through it.

---

## Production Chains (Rough Sketch)

The current goods are **Grain / Ore / Tools**, with one refining step already live:

```
Ore ──► Workshop ──► Tools     (raw → refined; the M3 chain)
Grain ──► (fed to pops)        (raw → consumed; the M5 demand sink)
```

Deeper multi-tier chains (raw → processed → finished) are a **future** direction — e.g. adding a refined good
that consumes Tools, or a rare strategic good with its own chain. Any such expansion should be designed through
the triad in [`ReviewDocs/Documentation/EconomyDesign.md`](ReviewDocs/Documentation/EconomyDesign.md) (a new
good only earns its place if it creates a decision).

---

## Player Action Loop

1. Scout the map — find high-value resources near the MST
2. Build a connector road to tap the MST
3. Place production buildings on the province
4. Sell output into the trade network
5. Prices respond to supply — find undersupplied goods
6. Reinvest profit into new connections and better buildings

---

## Open Design Questions

- **Player agency level** — *Directionally resolved (2026-06-27):* **automate the micro, keep agency in the
  macro** — the market clears itself; the player shapes conditions (specialty, routes, tariffs, blockades,
  diplomacy). See [`ReviewDocs/Documentation/TradeDesign.md`](ReviewDocs/Documentation/TradeDesign.md).
  (Building placement specifics still open.)

- **Connector road connection point** — Nearest MST node (capital) or any point along
  an MST edge? Mid-edge is richer but more complex to implement.

- **Conflict** — *Directionally resolved (2026-06-27):* an **economic casus belli** — monopoly pricing power
  plus chokepoint/blockade control gives a reason to fight; warfare/conquest layers on later. See
  [`ReviewDocs/Documentation/TradeDesign.md`](ReviewDocs/Documentation/TradeDesign.md).

---

## Build Priority Order

1. Wire up TickManager — production happens per tick
2. Province ownership — assign each province to player or AI faction
3. Transport cost = connector length + MST path length to destination
4. Prices that respond to supply and demand
5. Population/demand nodes in provinces (drives what sells)
6. Connector road construction mechanic for players and AI
