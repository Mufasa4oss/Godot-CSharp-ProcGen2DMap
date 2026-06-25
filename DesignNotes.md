# Game Design Notes — Economy Strategy Game

## Concept

Capitalism + Factorio hybrid. A map-based economic optimization game where geography
determines resource value, production chains create depth, and logistics is the core puzzle.

---

## Trade Network

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
           [Iron Mine]
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

Raw materials → Processed goods → Finished goods

```
Coal ──┐
       ├──► Steel Mill ──► Steel ──► Tools / Machines
Iron ──┘

Grain ──► Mill ──► Flour ──► Bakery ──► Bread

Wood ──► Sawmill ──► Lumber ──► Construction Materials
```

The existing resource names (Wood, Stone, Iron, Gold, Grain, Livestock, Coal) are the correct
tier-1 raw inputs. Processed and finished goods come next.

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

- **Player agency level** — Does the player place buildings manually (Factorio-style)
  or invest capital abstractly and let buildings auto-run (Capitalism-style)?

- **Connector road connection point** — Nearest MST node (capital) or any point along
  an MST edge? Mid-edge is richer but more complex to implement.

- **Conflict** — Pure economic competition between AI factions and player,
  or is there warfare/province conquest?

---

## Build Priority Order

1. Wire up TickManager — production happens per tick
2. Province ownership — assign each province to player or AI faction
3. Transport cost = connector length + MST path length to destination
4. Prices that respond to supply and demand
5. Population/demand nodes in provinces (drives what sells)
6. Connector road construction mechanic for players and AI
