# Asset Pipeline — tile size, image resolution, and 2D vs 3D

Straight answers to the art-pipeline questions, with the reasoning so you can decide as the project
changes. Companion to [BlenderGuide.md](BlenderGuide.md).

---

## 1. The current workshop sizing bug

The workshop shows as a **wide dark bar** because the render is **(a) not square** and **(b) not
transparent**:

- Our code (`WorkshopSprites.AddWorkshop`) scales the sprite by **width** to one tile. A 16:9 frame
  scaled to 64px wide becomes ~36px tall → a short, wide rectangle.
- Without Blender's **Film ▸ Transparent**, the render's background is baked in → the dark panel instead
  of grass showing through.

**Fix at the source (best):** render **square** (e.g. 256×256) with **transparent film** — see
[BlenderGuide.md](BlenderGuide.md) §4. Then it drops onto one tile cleanly.

**Optional code hardening:** I can change the scale to fit the *larger* dimension into one tile
(`scale = TileSize / max(width, height)`), so a non-square image is letterboxed inside one tile instead
of stretched. Say the word and I'll apply it — but fixing the render is the real solution.

---

## 2. Is 64×64 good? Should tiles be bigger?

**64×64 is a good default — keep it.** It's power-of-two (GPU-friendly), enough pixels to read a prop,
and it matches your tileset's cell size (the map is laid out on 64px cells, `TileSize = 64`).

Think of it as **two separate numbers that people conflate:**
- **Tile grid size** = how big a cell is in the world (64 world-px here). Changing this touches the
  tileset, camera math, and every overlay (`TileSize` constants in `Camera`, `WorkshopSprites`,
  `TerritoryOutline`). It's a real refactor.
- **Source image resolution** = how many pixels you author/render a sprite at. **Independent** of the
  grid — a 256×256 render can display on a 64px tile (the engine scales it).

So "should I make tiles bigger?" usually really means "do I want more *detail*," and you get detail by
raising **image resolution**, not the grid.

| | Keep 64px grid | Bigger grid (128px) |
|--|--|--|
| **Pros** | matches everything now; cheap; fine for a strategy map you view zoomed-out | more on-screen detail when zoomed in |
| **Cons** | less detail per cell up close | 4× texture memory; bigger map in world units; touches camera/overlay math; detail wasted at your default zoom-out |

**Verdict:** you spend most time **zoomed out** (the camera starts at 0.1 zoom showing the whole map),
where extra tile detail is invisible. Keep the **64px grid**; if props look soft when you zoom in,
**raise image resolution**, not the grid.

---

## 3. Author/render large and scale down, or author at tile size?

**Default: author/render LARGER and let the engine scale down — but not *huge*.**

- **Downscaling looks good** (the GPU averages pixels → smooth). **Upscaling looks bad** (blurry/blocky).
  So having more source pixels than you display is safe and future-proofs you if you ever zoom in or bump
  the grid.
- **The catch is memory/load** — which you already felt. Your 60 frames are ~1.1 MB each (likely
  ~1080p); decoding all 60 caused the placement lag. "Larger" should mean **2–4× the tile**, i.e.
  **128–256px square**, *not* 1080p.

**Rules of thumb:**
- **3D renders (Blender):** render **square, transparent, 128–256px**. Crisp at normal zoom, light enough
  to avoid the lag. For animations, also **cut frame count** (12–24, not 60).
- **Pixel art:** author at **1× the display size** (or an integer multiple like 2×) and scale by integer
  factors only. Pixel art scaled by non-integer amounts shimmers and loses its crisp edges — the opposite
  of the "render big, scale down" advice. Pixel art wants exact sizes.

---

## 4. Blender (3D) vs LibreSprite (2D)?

Two different aesthetics and two very different effort curves.

**LibreSprite / Aseprite (2D pixel art)**
- ➕ Far less friction; you can make a readable prop in minutes. Built-in animation (onion-skinning),
  tiny files, instant iteration, naturally consistent style.
- ➖ You draw everything by hand; the rendered-3D "Factorio" look is hard to fake without real pixel-art
  skill. Perspective/lighting is all manual.

**Blender (3D → rendered 2D)**
- ➕ The Factorio look (real lighting, volume, perspective); set up a camera/light rig **once** and every
  prop renders consistently; easy to rotate/animate machinery; reuse models.
- ➖ Steep UI (you noticed), heavier assets, render setup overhead, slower iteration per prop.

**Recommendation**
- If you want the **rendered-3D aesthetic** and will make **many** props → **Blender**, but with a
  **reusable template scene** so each prop is just "model + render" (see BlenderGuide §6). The setup cost
  amortizes.
- If you want **readable placeholders fast**, or a clean flat strategy look → **LibreSprite**. Much less
  fighting the tool.
- **Right now (temporary art, prototyping the economy loop):** don't sink hours into either. The art is
  an **M7 (polish)** concern. Use whatever gets a recognizable shape on the tile fastest — honestly even
  flat colored shapes or quick 2D would do until the *gameplay* loop is fun. You picked Blender and have
  a working pipeline; lock a template and move on.

**Mixing is fine too:** many games render 3D props but hand-touch them in a 2D tool. You could render a
base in Blender and clean it up in LibreSprite later.

---

## TL;DR
- Keep the **64px grid**. Get detail from **image resolution**, not a bigger grid.
- Render/author **larger then scale down**, but cap it at **128–256px square** (3D) to avoid the load
  lag; **pixel art wants exact integer sizes** instead.
- Fix the workshop: **square + transparent** render. (Or have me letterbox non-square images in code.)
- **Blender** for the Factorio 3D look (with a reusable rig) vs **LibreSprite** for fast 2D — but it's an
  M7 polish decision; don't over-invest before the loop is fun.
