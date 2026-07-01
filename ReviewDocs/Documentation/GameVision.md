# Game Vision — The 30-Hour Stories

> **What is this document?**  
> A bird's-eye view of the whole game — not what's built today, but what we're building *toward*. Anchored by
> the question that should drive every design decision: *when a player closes the game after 30 hours, what story
> do they tell?* Everything in here flows from that question.
>
> Status: **design direction**, written 2026-06-30. The ground floor (L1) is code-complete. The stories in this
> document are the destination; the architecture already built is the foundation they rest on.

---

## The Central Question

When you imagine someone playing your game for 30 hours, what do you hope they say when they close it?

Not *"I got 847 Ore."*  
Not *"I optimized my extractor throughput."*

More like:

> *"I became the breadbasket of the continent."*  
> *"I controlled every trade route. Nobody moved a good without paying me."*  
> *"My ally went hostile. My pops starved. My treasury collapsed. I had to beg for grain."*  
> *"I transformed a frozen wasteland into the richest nation on the map."*  
> *"I built a road nobody thought was worth it — and it broke the monopoly."*  
> *"I started a war I couldn't afford. My army starved. My economy followed."*  
> *"Everyone needed tools. I was the only one who could make them. I named my price."*  
> *"A dragon hit my main mine. I had three ticks to reroute everything."*  
> *"I let my roads decay trying to save money. Everything fell apart."*

**The game design lives in those sentences.** Every mechanic, every number, every piece of UI either serves one
of those stories or it doesn't belong. This document maps the full design space — what we've built, what we're
building, and what decisions remain — through the lens of the stories we want players to tell.

---

## The Spine: What Kind of Game This Is

In plain language: **you are the government of a country, and your country's economy is a physical thing you can
see and touch.**

You can see goods moving along roads. You can watch a hauler carry grain from a farm to your capital. You can see
the price of tools rise in a nation that can't refine ore. You can see a supply line stretch thin as an army
pushes deep into enemy territory.

That visibility is the whole game. Everything is legible. Everything is spatial. The map is not decoration — it
*is* the game state.

**The frame:** Victoria 3's "you are the state" (big decisions, macro agency) + Factorio's spatial logistics (you
build the network; the network is the puzzle) + EU4's map-painting war (external pressure, rivals, conflict over
territory) — but with an economy that EU4's is too shallow to have.

**The differentiator:** there is one physical road network. Goods move on it. Armies move on it. Cut a road and
you cut both the economy *and* the army supply line at once. This is not a feature — it's the thesis. The economy
and the military are not two separate systems with a conversion rate. They are the same network, and the player
is always managing both whether they think of themselves as a builder or a conqueror.

---

## The Nine Stories — and What Makes Each Possible

### Story 1: "I became the breadbasket of the continent"

**The fantasy:** your nation has fertile grain tiles. You build farms, build roads, and slowly your grain floods
every connected nation. Your pops are fat. Your neighbors are dependent. You name the price — and when you raise
it, there's nothing they can do.

**What has to be true for this story to happen:**
- Starting geography that rewards grain specialization (some maps give you this, some don't — that's the point)
- AI nations have real pop demand for grain (they're not infinite abstract markets)
- Your price rises when you're the only supplier — monopoly pricing is automatic and visible
- You can *feel* the dependency: when you go hostile with a buyer, their pops suffer, and you can see it

**Design lever:** specialty/development as the #1 player choice. Building toward a specialty is the mid-game
decision that defines your nation's story arc.

---

### Story 2: "I controlled every trade route"

**The fantasy:** you notice that two large nations are separated by a mountain range, and your territory sits in
the only navigable pass. You build the road through that pass. Now every good that crosses the continent crosses
your land. You set the tariff. You collect the toll. You become indispensable — and when you raise that tariff,
they either pay or go to war.

**What has to be true:**
- Chokepoints exist on the map — geographic constraints that force routes through specific tiles
- Road-building is the player's core infrastructure act (already built)
- Tariffs are a real lever — you can tax goods crossing your territory
- Blockade is the nuclear option — you can cut the route entirely, not just tax it
- The map legibility to *see* that you're the chokepoint: visible flow lines, congestion, prices

**Design lever:** chokepoints & blockades (lever #2 in priority). The map becomes a geopolitical puzzle once
players realize spatial control of routes is economic power.

---

### Story 3: "My ally betrayed me and my economy collapsed"

**The fantasy:** you specialized. You built workshops. You sell tools. But you don't have enough grain — you
import it from a friendly neighbor. You're deep in a productive groove when they flip to hostile. The grain stops
coming. Your pops start starving. Your workshops slow (workers need food). Your treasury drains. You have to make
a horrible choice: pivot to farming (abandon your specialty), find another grain source (build a long expensive
road), send diplomats to patch the relationship (costs gold you don't have), or go to war.

**What has to be true:**
- Import dependency is a real strategic state — you can actually specialize away from a good
- Diplomatic stance change has immediate material consequences on the supply network
- The player feels the cascade: grain shortage → pop unhappiness → production slowdown → treasury decline
- Recovery is possible but costs something real (time, gold, strategic pivots)
- The AI makes this choice for legible reasons — not randomly, but because you're doing something that threatens them

**Design lever:** diplomatic relations as a real risk variable (not just a buff to click). The "my ally betrayed
me" story only works if trust is something you build and can lose. Relations need *texture*.

---

### Story 4: "I transformed a frozen wasteland into the richest nation"

**The fantasy:** you spawned in a map with almost no grain, cold terrain, minimal pop capacity. Every other player
(or AI) has a head start. But deep in your frozen territory, there's a vein of the strategic good — the one
resource everyone needs but few have. You build roads through miserable terrain. You build workshops. You refine
raw goods into premium products. You become the supplier of the one thing that makes civilization run. The
geography that looked like a death sentence became your monopoly.

**What has to be true:**
- Map generation guarantees imbalanced starts — some maps are hard. That's intentional.
- The strategic good exists and is rare. Not Grain (everywhere) or Ore (common) — something that only a few
  maps have a source of. Something powerful: a military input, a luxury, an advanced material.
- Transformation chains reward investment. Refining raw into finished is not just "more gold" — it's a
  different market position, different buyers, different diplomatic leverage.
- Road-building through difficult terrain is hard but possible — an investment that pays off visibly.

**Design lever:** the strategic good (still an open question — see the Open Questions section). This is the
single biggest unresolved design decision. Whatever the strategic good is, it needs to be the thing that makes
a player's 30-hour run feel like it had a villain and a prize.

---

### Story 5: "I started a war I couldn't afford"

**The fantasy:** you had a border dispute with a smaller nation. Their territory blocked your road expansion. You
had the army. You attacked. And then everything went wrong. The war lasted longer than expected. The supply lines
stretched. The enemy cut a key road. Your army deep in their territory suddenly had no food. Your treasury, which
you thought was large, evaporated into military upkeep. You had to sue for peace in a position of weakness — or
watch your economy crumble trying to hold territory you couldn't afford.

**What has to be true:**
- Military uses the road network for supply — armies aren't independent of logistics
- Wars cost gold continuously, not just to declare
- Enemy can cut roads (and your army suffers)
- Overextension is a real failure state, not just a mild penalty
- The player can see this coming and choose to ignore the warning signs (treasury dropping, supply lines
  thinning) — the tragedy is always partly self-inflicted

**Design lever:** military as a consumer of the economy, not separate from it. This is an L3 system but its
foundation (roads as supply lines) must be baked into L1/L2. The "army starves when road is cut" story only works
if the road being cut has a visible, immediate economic AND military consequence.

---

### Story 6: "Everyone needed tools. I was the only one who could make them."

**The fantasy:** you built workshops. Many workshops. You have a lot of ore and you refine it all. The continent
runs on tools — every mine uses them, every advanced building needs them. But making tools requires ore AND a
workshop AND grain to feed the workers. You assembled all three. Other nations, distracted by grain farming or
raw ore extraction, never built the refining chain. Now you're the sole supplier of the highest-value good. Your
price is three times base. The gold flows.

**What has to be true:**
- Refining is non-trivial — it requires inputs (ore, grain for workers) and infrastructure (workshops)
- Not every nation can or will build the full chain (comparative advantage is real, not just flavor)
- Tools have premium buyers — someone needs tools badly enough to pay monopoly prices
- The player can see "who else makes tools" on the map — if a competitor emerges, you can see it coming
- Counter-play exists: rivals can build workshops if they have ore. Breaking your monopoly is a strategic option.

**Design lever:** refined goods having materially higher strategic value, not just higher base price. Tools should
unlock something — advanced buildings, military capacity, development options — that raw goods don't.

---

### Story 7: "I built a road nobody thought was worth it — and it broke the monopoly"

**The fantasy:** one AI nation controlled the only known source of Iron (the strategic good). They were charging
triple base price and growing rich. You had one option: explore the map and find that there was a secondary
Iron deposit — far away, across a mountain range, through a dense forest. Nobody had built to it. You did. It
was expensive. It took time. But when your Iron came online, the monopolist's price collapsed. Their treasury
stalled. The geopolitical balance shifted, and you were the reason.

**What has to be true:**
- The map holds secrets — deposits that aren't exploited yet (because they're remote or hard to reach)
- Road-building through difficult terrain is an investment with real cost (upkeep, time, resource drain)
- Price signals tell you where the opportunity is (high prices = someone is paying monopoly rates somewhere)
- The moment your supply comes online, prices actually move — the simulation reacts

**Design lever:** deposit geography as a discovery layer. Not everything is obvious from turn one. The map
rewards players who look, who push roads into the unknown, who invest in long-term bets.

---

### Story 8: "A dragon hit my main mine. I had three ticks to reroute everything."

**The fantasy:** mid-game, deep in a productive groove — and then a dragon spawns on your largest ore deposit.
Your ore income drops to zero. Your workshops stop (no input). Your tools income collapses. Meanwhile, your
upkeep ticks on. You have a few ticks before the treasury hits zero. The crisis is spatial: you need a new ore
source, connected to your capital by road. Do you have a backup deposit? Is there a friendly neighbor who has
ore? Can you reroute fast enough?

**What has to be true:**
- PvE events (dragons, floods, plagues) are supply shocks on specific nodes and edges — they're not abstract
  penalties, they physically break a tile or route
- The player can see the damage on the map (the deposit goes dark, the road disappears)
- The crisis creates a decision problem with urgency — time-limited because upkeep keeps running
- Recovery is possible if the player has built redundancy — backup routes, friendly neighbors, stockpiles

**Design lever:** PvE as supply-chain stress testing, not random punishment. The dragon isn't evil; it's a
geography event that reveals whether your network is robust or fragile. Players who built redundancy handle it.
Players who built a single critical path don't.

---

### Story 9: "I let my roads decay trying to save money. Everything fell apart."

**The fantasy:** treasury was tight. Upkeep on roads was a nagging drain. You started letting some connector
roads lapse — the ones that felt less essential. A hauler route broke. Then another. Your workshops stopped
getting ore (it was coming from a far deposit via those roads). Your income dropped, so you let more roads decay
to save gold. The collapse was gradual and then total. This isn't a dragon or a betrayal — it's pure
self-inflicted neglect.

**What has to be true:**
- Upkeep is real and accumulates — roads cost ongoing gold to maintain (already built: UpkeepPerRoad)
- The player can see individual roads' status and choose to let them lapse (active decision, not passive drain)
- The network failing has cascading consequences through the supply chain
- The player who maintains their network vigilantly doesn't hit this story — it's a *choice*

**Design lever:** upkeep as a structural constraint, not just a gold sink. Upkeep should force prioritization.
You can't maintain everything forever — you choose which roads are worth it. That's the design surface.

---

## What Kind of Game This Is (Technical Definition)

### One Network

The most important architectural decision is already made and must never be unmade: **there is one physical
network, and everything runs on it.**

```
Your Grain Farm → [road tiles] → Your Capital → [road tiles] → Allied Nation
Your Army Camp  → [same road] → Your Border   → [same road] → Enemy Territory
```

Cut a road and both flows stop. This is the differentiator. It means every decision about roads is simultaneously
an economic decision and a military decision. There is no "economy mode" and "war mode" — there's just the
network, and the situation it creates.

### You Play the Government, Not the Merchant

All micro is automated. Haulers dispatch themselves. Markets self-clear. Goods flow high → low price without
player orders. The player never says "move this unit of grain from tile (40, 12) to (55, 8)."

The player says:
- "Build a road here" (changes what flows where)
- "Build a farm here" (changes what is produced)
- "Set tariff to 20% with this nation" (changes the terms of flow)
- "Go hostile with this nation" (cuts the flow entirely)
- "Send a diplomat" (moves relations toward friendly terms)
- "Build here to develop this specialty" (commits to a production identity)

These are government acts. They're durable (they persist between ticks). They have visible consequences on the
map. And they stack — the combination of roads built + tariffs set + specialization chosen is unique to every
player, on every map. That combination is your nation's identity.

### Scarcity Is the Designer

The economy doesn't tell you what to do. The map does. The scarcity structure of your specific map — which goods
are rare, which deposits are remote, which nations need what — creates a unique decision problem every run. There
is no optimal build order because the map is different every time.

Three scarcity types shape every decision:
1. **Geographic scarcity** — you don't have a grain deposit; you must trade or starve
2. **Network scarcity** — you have the deposit but it's not connected; you must build road to access it
3. **Political scarcity** — your neighbor has what you need but relations are poor; you must negotiate or seize

The game is learning to read which scarcity you're facing, which transformation resolves it, and which resource
you spend to make that transformation happen.

---

## The Game Loop

### Early Game (Sessions 1–3, hours 0–8)

**The question:** what hand did the map deal me?

You look at your starting territory. What deposits do you have? What terrain? What nations border you, and what
do they seem to have? You're making the first structural decisions:
- Which deposits to exploit first (build-gating means you pick, not all)
- Where to build your first roads (each road costs upkeep; every choice is a commitment)
- Whether to sell raw goods for fast gold or hold for refining
- Whether your geography makes you a grain supplier, an ore supplier, or something else

The early game is a puzzle: can you close the basic loop (extract → haul → market income) before your treasury
runs dry? The answer depends on the map and the choices you make in the first 10-20 ticks.

**The feeling:** scrappy. Every gold spent matters. One farm feeds your pops; two farms gives you a small surplus
to sell. You're figuring out your hand.

**The turn you know you're past the early game:** you can afford a Workshop *and* keep upkeep paid without
stressing the treasury.

---

### Mid Game (Sessions 3–8, hours 8–20)

**The question:** what is my nation's specialty, and who do I sell it to?

By mid-game you know your geography. Now the question is identity: what do you become? The map usually makes one
specialization easier — but easier isn't always smarter, because your neighbors may have the same deposits.

Mid-game is where diplomacy becomes real. You've met your neighbors (road connections to their territory), you
can see their prices, you know who's a buyer and who's a competitor. The diplomatic panel goes from "I haven't
thought about this" to "this is how I'm making money."

You're also starting to see chokepoints. That narrow pass on the eastern edge of the map — you didn't notice it
in the early game, but now you see that two rich nations are separated by it, and your territory is right there.

**The feeling:** strategic. You're making choices that will define the late game. Every Workshop built, every
road extended, every diplomatic stance set is a bet on a future that hasn't happened yet.

**The turn you know you're in mid-game:** you change your tariff rate with a neighbor and see their export flow
to you change the next tick. The simulation is reading you.

---

### Late Game (Sessions 8+, hours 20–30)

**The question:** can I hold what I've built?

The late game is not about growing — it's about defending a position and exploiting it. You've specialized.
You've built routes. You have pricing power in at least one good. Now the game applies pressure:
- AI rivals figure out they need what you have and try to build alternative sources
- PvE events (dragons, disasters) stress-test your network
- A diplomatic betrayal by an important partner forces a scramble
- Military conflict over a chokepoint you both want

The late game is also where the **strategic good** matters most. Early game, everyone is mostly Grain and Ore.
Late game, the nation that controls the strategic good — and has built the roads and relations to sell it — is
the dominant power. Others may try to break that monopoly. The player who holds it must defend it. The player who
doesn't must find a path to it.

**The feeling:** managing fragility. Your network is large and valuable and has dependencies you built deliberately
— and now those dependencies are vulnerabilities. The same chokepoint that made you rich is the place an enemy
army would attack first.

**The turn you know you're in the late game:** you realize that a decision you made in hour 4 (where you built a
road, which nation you went friendly with) is now the structural constraint on everything you can do in hour 25.
The game rewarded that foresight or punished its absence.

---

## The Four Pillars

Everything in the game design should serve one of these four pillars. If a proposed mechanic doesn't serve any of
them, it doesn't belong.

### Pillar 1: Geography Is Destiny — But Not Prison

Every map deals a different hand. Some starts are good; some are hard. That's the point — the transformation
story (story 4) only works if bad geography is real and overcoming it means something. But no start should be
unwinnable. The map creates pressure and opportunity; the player's choices determine whether they turn their hand
into a story.

- Deposits are spatially distributed and not uniform
- Road terrain cost should differ (mountains are expensive; plains are cheap) — the geography shapes the network
  cost
- Starting treasury is tight enough that you can't build to everything at once — you commit to a direction

### Pillar 2: The Network Is Legible

The player should be able to read the simulation just by looking at the map. This is not a UI design nicety — it
is the core game experience. If you can see goods flowing, you understand what's happening. If you can see a
supply line thin out as an army extends, you understand the risk. If you can see a price spike on a nation's good,
you understand the opportunity.

- Road flows are visually distinct: direction, volume, good type
- Prices are visible per nation on the diplomacy panel (already in)
- Deposits pulse when active, dim when idle, flash when depleted (already in)
- Haulers are visible entities moving on the map (already in)
- Trade events and diplomatic changes generate brief readable feedback

The map is the UI. Everything should be readable from the map first, panel second.

### Pillar 3: Decisions Are Durable Government Acts

The player never manages individual goods. They set conditions. They build infrastructure. They set policy.
The market executes the resulting behavior automatically and continuously.

This means:
- No buy/sell orders, ever
- No per-hauler routing orders
- Road decisions, building decisions, diplomatic stances — these are the vocabulary of play
- The player's "hand" at any point is the set of durable choices they've made: their network shape, their
  buildings, their diplomatic relations, their specialization

The player who built roads east is a different player than the one who built roads north — not because of a
different UI path, but because their network has a different shape and creates different opportunities.

### Pillar 4: Pressure Prevents Coasting

A game where you can build forever without pressure is not a game — it's a toy. The stories that matter (betrayal,
collapse, scramble, pivots) require that the player is sometimes under pressure they didn't choose. Three pressure
sources:

1. **Upkeep** — roads and buildings cost ongoing gold; you can't build everything and hold it forever without
   income to match. This forces prioritization and creates the decay-collapse failure mode (story 9).

2. **AI competition** — AI nations produce, trade, and compete. They don't just sit there as trade targets. They
   build to the same deposits. They establish their own specializations. They respond to price signals. The
   player who ignores the AI gets undercut or priced out.

3. **PvE events** — dragons, floods, plagues are supply shocks on specific nodes. They aren't random punishment;
   they're stress tests. A robust network handles them. A fragile single-path network doesn't. Players should
   see these as reveals of their own decisions, not arbitrary setbacks.

---

## The Player Archetypes

Different players will find different stories. The game should support all of these play styles on the same
simulation without hard-coding any of them:

### The Builder (Factorio player)
Wants to optimize the network. Will spend 20 hours routing roads, placing workshops, watching haulers. Succeeds
when they realize their beautiful efficient network is also the best-defended network because it has no single
point of failure. Fails when they optimize for throughput and forget upkeep math.

### The Diplomat (Victoria 3 player)
Wants to win without fighting. Will specialize heavily, maintain friendly relations, use tariffs as leverage,
send diplomats constantly. Succeeds when they're indispensable — when their supply is so important that
going hostile with them is unthinkable. Fails when an AI rival outmaneuvers their diplomacy and cuts them out.

### The Conqueror (EU4 player)
Wants to paint the map. Will go hostile, blockade rivals, attack chokepoints. Succeeds when they seize a key
pass and tax every flow through it. Fails when they forget that wars cost gold and that an army without a supply
line is just expensive walking.

### The Monopolist (Wall Street Raider player)
Wants to corner a market. Will hunt the strategic good, control its source, set the price, and watch gold flow
in. Succeeds when they have pricing power in the most demanded good on the map. Fails when a rival builds an
alternative source and their monopoly collapses.

All four archetypes use the same mechanics. None requires a special mode. The map situation and the player's
inclinations determine which story they're living.

---

## The Goods and Their Stories

### Current Goods (L1)

| Good | Role | Story it enables |
|------|------|-----------------|
| Grain | Staple food. Pop demand is automatic. Cannot be refined. | Breadbasket (story 1). Dependency weapon. Pop-starvation crisis. |
| Ore | Raw material. Feeds workshops. | Industrial base. Refining chain. Export good when surplus. |
| Tools | Refined output (Ore → Tools via Workshop). High value. | Workshop monopoly (story 6). Military input (future). Advanced building unlock (future). |

### The Strategic Good (Vision — not yet built)

This is the most important unresolved design question. The strategic good is:
- **Rare** — only a few deposits per map, often in difficult terrain
- **Necessary** — something that advanced players or AI must have for late-game development; without it, you're
  capped at a lower tier of development
- **Contestable** — multiple nations could potentially reach a deposit, creating genuine competition
- **Non-substitutable** — unlike Grain (you can farm anywhere given the tiles), you can't produce it without a
  deposit

**Candidate designs:**
- *Iron/Steel* — a military-only input; everyone needs it for armies; pure conflict driver
- *Spice/Luxury* — a pop luxury; nations with it have higher pop happiness/growth/tax yield; softer monopoly
- *Coal/Fuel* — an industrial multiplier; unlocks advanced workshop tiers; refining chain extender
- *Gems/Gold* — a pure money sink absorber; the nation that exports it absorbs others' gold, creating a reserve
  currency dynamic

**Design test:** does controlling the strategic good make a player feel like they're running the continent? If
the answer is "it's nice but not transformative," it's not the right design. The strategic good should be the
thing that makes a monopolist's 30 hours feel like story 6. It should be the thing that makes a wasteland run
feel possible (story 4). It should be the thing that starts wars.

**Recommendation:** Coal/Fuel as the strategic good. Reasoning:
- It's an industrial input — advanced workshops require fuel to operate
- It's geographically plausible on maps with mountain/deep-terrain biomes
- It creates a refining chain: Coal + Ore → Steel (future L2 good) → Tools/Military
- It doesn't directly translate to "buy army" — it works through the economic layer
- Without it, your economy is capped at the basic tier: extractors and basic workshops
- With it, you unlock advanced buildings, higher throughput, better refinement ratios

---

## The Layer Roadmap

### L0 — Foundation (Done)
Map, provinces, deposits, roads, the visual world. The canvas.

### L1 — The Economy Loop (Done: M0–M8, LR0–LR5)

The physical economy is running. What's in:
- **Physical road network** (player-built, tile-based; BFS connectivity; A* pathfinding)
- **Hauler system** (Dispatcher + Hauler entities moving goods tile-by-tile on roads)
- **TileInventory** (goods exist physically at tiles — not abstract nation stockpiles for the player)
- **Internal market** (pops buy from capital inventory → treasury income; demand-capped)
- **Export** (capital surplus → road-connected neighbors at their price − tariff − transport)
- **Diplomatic stances** (Hostile = blockade; Neutral = tariff; Friendly = free trade)
- **Upkeep** (gold sink: UpkeepPerBuilding + UpkeepPerRoad every N ticks)
- **AI nations** (produce on their own stockpiles; auto-build extractors; have pops and demand)
- **Encrypted saves** + visual overlay (haulers, deposit pulses, flow visualization)

**What L1 establishes for all future layers:**
- (1) Every good has a price per nation — monopoly pricing is already the model
- (2) Every tile has an owner — territory and ownership are already the political model
- (3) Everything is a node/edge on one graph — military, trade, supply all use the same network

### L2 — Trade Depth and Specialization (Next)

The simulation is running; now make the *strategic layer* of trade richer:
- **The strategic good** — design and implement it; one new good that creates late-game pressure
- **Specialty development** — player can invest in making a nation better at its specialty (output rate, cost
  reduction, quality tier); the #1 design priority per the locked trade model
- **Per-node price gradient** vs. single clearing price — currently exports use neighbor prices; the next
  question is whether a true price gradient emerges across the network
- **AI strategic behavior** — AI doesn't just extract greedily; it identifies what it needs (by price signal)
  and makes diplomatic moves to get it
- **Route congestion** — heavy traffic is visible and creates natural routing optimization puzzles
- **More goods / refining chains** — the two-step Ore→Tools chain can extend: a third-tier good, or a fuel
  input that unlocks it

### L3 — Conflict and External Pressure (Future)

War, AI rivals with real strategy, and PvE events as supply shocks:
- **Military** as a consumer of the road network (supply lines from capital; cut = starve)
- **AI rivals** that identify strategic threats (your monopoly) and act against them (build alternative routes,
  attempt to seize your chokepoints diplomatically or militarily)
- **PvE events** (dragons, floods, plagues) as node/edge shocks — Dwarf Fortress-style disruptions that stress
  the network
- **Economic casus belli** — the road-control system naturally produces conditions for war (you're starving me
  by blocking my grain; I need to seize your pass)

### L4 — Finance and Characters (Long-term)

Deeper systems if L3 earns them:
- **Finance/ownership** — investment, debt, trade guilds, the Wall Street Raider layer
- **Pop strata** — different pop classes with different consumption and political effects
- **Characters** — if the game gets to CK3-style character events; this is the optional final layer

---

## The Open Questions

These are decisions that are not yet made and will significantly shape the game's character. They should be
answered during L2 design, not before.

### 1. What is the strategic good?

See the Goods section above. This is the most important open question in the whole design. The strategic good
determines whether monopoly feels meaningful, whether the wasteland transformation story is possible, and whether
late-game diplomatic/military conflict has a clear prize.

**Decision needed before:** starting L2 goods design.

### 2. Single clearing price vs. per-node price gradient?

Currently: exports use the buyer's local price. A true price gradient would mean goods flow along a multi-hop
chain with prices adjusting at each node — closer to real spatial economics but more complex to compute and
legible.

**The tradeoff:** gradient = more realistic monopoly dynamics, emergent trade routes that are clearly visible;
single = simpler, more predictable, easier for the player to read.

**Recommendation:** gradient, but only between directly connected nodes (not global). The current
neighbor-price export model is already a simplified gradient — extend it one hop further and you have meaningful
spatial pricing without global clearing complexity.

### 3. How do diplomatic relations deepen?

Currently: a three-state stance (Hostile/Neutral/Friendly) set by the player. The next question is whether
relations become a *variable* that moves over time based on:
- Trade flows (if you're buying from someone, relations improve)
- Diplomatic acts (sending diplomats costs gold, improves relations)
- Economic harm (if your tariffs are squeezing someone, relations drift hostile)
- Events (AI reacts to being blockaded, to monopoly pricing, to military threats)

**Decision needed:** does AI drive its own diplomacy, or does the player just manage a number? The "ally
betrayal" story requires AI-driven diplomacy — the AI has to *decide* to go hostile based on its own interests.

### 4. How does military integrate with the road network?

The locked differentiator is "cut the road, starve the army." Implementing this means:
- Armies consume goods (grain) from the road network while marching
- An army in enemy territory needs a supply line back to a road-connected capital
- Severing that line (by blocking or destroying a road tile) causes attrition

**Decision needed:** what's the minimum military system that makes "supply lines" feel real without building
a full RTS? Recommendation: armies have a single stat (strength/supply level) that degrades when supply is
cut, and recovers when connected to a capital with goods.

### 5. What makes the late game feel fresh instead of solved?

The logarithmic money curve (fast early, steady mid, slowing late) requires "refreshed by new objectives" in
the late game. What are those new objectives?

Candidates:
- The strategic good becoming available (new deposit opens, new tech unlocks access)
- AI rivals reaching a threshold that creates an obvious strategic threat
- PvE events of escalating severity (dragon year 1 is a small mine; dragon year 5 is the capital)
- A player-driven prestige system ("control 5 of the 7 chokepoints to win")

**The danger:** a late game that becomes just "accumulate gold and build more of the same things." The strategic
good + AI rivals are the primary defenses against this.

---

## What Success Looks Like

The design North Star is simple: **a player should be able to describe their last run in one sentence, and that
sentence should be unique.**

Not "I built farms and sold grain." (That's a mechanic description.)

But: "I was totally locked out of grain — my territory was all mountains — so I went all-in on ore and tools,
cornered the tools market, and got rich selling to the two grain-rich nations that had no workshops."

That sentence has geography, a constraint, a strategic decision, and a payoff. It could only happen on that map,
with that player's choices, against those AI opponents. No other player in any other run would tell exactly that
story.

**That** is the game.

---

## Design Rules (The Guardrails)

These apply to every future design decision:

1. **If it doesn't appear on the map, it's not the game.** Every mechanic should be spatially visible. If you're
   adding a system and can't point to where it appears on the map, rethink it.

2. **Before adding income, add the matching sink.** Gold must never feel meaningless. Every new income source
   needs a corresponding pressure on the outflow.

3. **Automate the micro; design the macro.** No per-good order, no per-hauler command. Every player lever should
   be a durable government-level decision.

4. **Every session should pose a fresh "what first?" decision.** If the optimal opening is always the same, the
   scarcity design is broken. Randomized maps are the guarantee; scarcity structure is the lever.

5. **Don't add features the stories don't require.** Characters, CK3 diplomacy, detailed military unit stats —
   these are L4, if ever. Every L2 addition should directly serve one of the nine stories above. If it doesn't,
   it waits.

6. **The differentiator is sacred.** The unified network (goods + armies on the same roads) is the thesis. Do
   not design any military system that has its own separate supply model. Everything uses the roads.

---

*This document is a living direction guide, not a locked spec. Update it when major design decisions are made.
Cross-link to `ReviewDocs/Build-History.md` for what's actually been built against this vision.*
