# Blender for Tile Props — a beginner's survival guide

You don't need to "learn Blender." You need to learn the ~12 keys and one render setup that let you
model a simple prop, point a camera at it, and spit out PNG frames for a tile. That's a much smaller
target than "Blender" and you can get there in an afternoon. This guide is scoped to **exactly your use
case**: small props rendered to 2D images that sit on the map tiles (the Factorio look).

> You've already done the hard part once: your `0001.png … 0060.png` are a Blender **PNG image
> sequence** (animation render). So you've found the output pipeline — this guide mostly fills in the
> setup *around* it and fixes the two issues we saw (non-square, non-transparent).

---

## 0. Is this overkill? (honest answer)

- **For a strategy map you mostly view zoomed out:** a lot of 3D detail is lost on a 64px tile, so
  high-effort renders don't pay off visually. Cheap, readable props win.
- **But the 3D-render workflow is legit** (Factorio, RimWorld-ish, tons of 2D games pre-render 3D). Its
  superpower is **consistency + reuse**: set up a camera + lights *once*, then every prop renders at the
  same angle/lighting "for free." That's worth the setup cost if you'll make many props.
- **Recommendation:** don't try to master Blender. Build a **reusable template .blend** (camera + lights
  + render settings fixed), and for each prop just model a shape and hit render. If Blender keeps
  fighting you, the 2D route (see [AssetPipeline.md](AssetPipeline.md)) gets readable placeholders much
  faster. Pick one and move on — art polish is an M7 problem, not now.

### The art convention (deliberate, not a one-off)

This is the "Factorio compromise" — the authoritative version lives in the M4.5 blockquote of
[`../M-Doc/L1-Milestone-Roadmap.md`](../M-Doc/L1-Milestone-Roadmap.md); the Blender-side gist:

- **Animation state = economic state.** A Mine with no deposit worked = idle frame; actively
  extracting = running loop; depleted reserve = sputter/stop. That makes M4.5's "where & why am
  I gaining resources" legible *without text* — the building itself tells you.
- **Fixed dimetric render angle for every building** (Factorio's ~off-top-down). Pick the angle
  once, bake everything to it, so all buildings share a light direction and read as one set.
- **Budget: cheap to fake, expensive to over-do.** 60 frames × 10 building types is a lot of
  renders. For MVP a short 8–16-frame loop per building is plenty; reserve the long smooth
  animations for hero buildings.
- **Pairs with M4.5 sub-rung 4** (the fading `+1` shader on resource gain): the building animates
  *and* spits a number. That combination is most of Factorio's game-feel, in pure 2D.

---

## 1. Moving around (the part that feels impossible at first)

Blender's navigation is mouse-heavy. With a 3-button mouse + scroll wheel:

| Action | How |
|--------|-----|
| **Orbit** the view | hold **Middle Mouse Button** (the wheel) and drag |
| **Pan** | **Shift + MMB** drag |
| **Zoom** | scroll wheel |
| Snap to **Front / Top / Side / Camera** | Numpad **1 / 7 / 3 / 0** |
| Frame the selected object | **Numpad .** (or View ▸ Frame Selected) |

**No numpad?** Edit ▸ Preferences ▸ Input ▸ check **Emulate Numpad** (then the number row works), or use
the little axis gizmo / View menu top-right of the viewport.

---

## 2. The 10 keys that do 90% of the work

Hover the mouse over the 3D viewport, then:

| Key | Does |
|-----|------|
| **Tab** | toggle **Object Mode** ↔ **Edit Mode** (edit the mesh's verts/edges/faces) |
| **Shift + A** | **Add** an object (mesh, light, camera…) |
| **G** | **grab/move** (then move mouse, click to confirm). Add **X/Y/Z** to lock an axis |
| **R** | **rotate** (then X/Y/Z, type an angle e.g. `45`, Enter) |
| **S** | **scale** (then X/Y/Z) |
| **A** | select **all** · **Alt+A** deselect all |
| **X** (or Delete) | delete selected |
| **Shift+D** | duplicate |
| **Z** | shading pie menu (Solid / Material Preview / Rendered) |
| **F12** | **render** the current frame · **Ctrl+F12** render the animation |

Confirm a move with **left-click**, cancel with **right-click / Esc**. That's the muscle memory.

---

## 3. The scene you need (it's tiny)

A new Blender file already has the three things a render needs: a **Mesh** (the default cube), a
**Light**, and a **Camera**. For tile props you want:

1. **Your prop** — model it (start by just reshaping the default cube; Tab into Edit Mode, select faces,
   E to extrude, S to scale). Keep it simple and centered near the world origin (0,0,0).
2. **A camera, set to a fixed angle.** Select the camera ▸ green camera **Object Data Properties** tab ▸
   set **Lens ▸ Type = Orthographic** (no perspective distortion → tiles read consistently). Position it
   at a repeatable 3/4 / iso-ish angle (e.g. rotate it ~55° down and ~45° around). *Save this.*
3. **Lighting you reuse.** A **Sun** light (Shift+A ▸ Light ▸ Sun) pointed down-ish gives even,
   consistent lighting. Maybe one weaker fill light. Same lights for every prop = matching shading.
4. **A material** for color: select the prop ▸ red-checker **Material Properties** tab ▸ New ▸ set **Base
   Color**. (Fancy materials are an EEVEE/Cycles topic — skip for now.)

---

## 4. Render settings (this fixes the two bugs we hit)

In the **Output** (printer icon) and **Render** (camera-back icon) properties tabs:

- **Square resolution.** Output Properties ▸ Resolution **X = Y** (e.g. **256 × 256**). This fixes the
  "wide dark bar" — a square render maps cleanly onto a square tile. (Don't render 1920×1080.)
- **Transparent background.** Render Properties ▸ **Film ▸ Transparent ✔**. This is why you saw a dark
  panel instead of the prop floating on grass — without it, the world background is baked in. With it,
  the PNG has real alpha.
- **Engine = EEVEE** (Render Properties ▸ Render Engine). It's the fast real-time one — fine for props
  and much quicker than Cycles. Use Cycles only if you later want nicer lighting and don't mind waiting.
- **Output format = PNG, Color = RGBA.** Output Properties ▸ File Format **PNG**, Color **RGBA** (RGBA =
  keep the alpha channel). Set the output folder.

---

## 5. Rendering (still vs animation)

- **Single image:** `F12`. Save with Image ▸ Save As.
- **Animation (your 60-frame case):** set the **frame range** (Output Properties ▸ Frame Start/End, or
  the timeline at the bottom), set the **output folder + PNG/RGBA**, then **Ctrl+F12** (Render ▸ Render
  Animation). Blender writes `0001.png, 0002.png, …` — exactly what you already have. Our
  `WorkshopSprites` loads those frames directly.

**Tip:** fewer frames = less VRAM and faster load. 60 full-res frames is what caused the placement lag.
For a looping idle animation, 12–24 frames at a modest resolution is plenty.

---

## 6. A 10-minute first prop (do this once)

1. New file. Delete the default cube? No — keep it; we'll reshape it into a little "workshop."
2. `S` `Z` `0.6` Enter → squash it a bit. `Tab` into Edit Mode, select the top face, `E` `Z` to extrude a
   chimney, `S` to shrink it. `Tab` back out.
3. Material Properties ▸ New ▸ Base Color = a stone gray.
4. Select camera ▸ Object Data ▸ Orthographic. Numpad 0 to look through it; nudge with `G`/`R` until the
   prop sits centered and reads well from a 3/4 angle.
5. Render Properties ▸ Film ▸ Transparent ✔, Engine EEVEE. Output ▸ 256×256, PNG, RGBA.
6. `F12`. Save the PNG. Drop it on a tile in-game to sanity-check the scale/look.
7. **Save this .blend as `prop_template.blend`.** Next prop = open it, reshape the mesh, re-render. The
   camera/lights/settings are already right.

---

## 7. Cheat sheet

```
Orbit  MMB-drag      Pan  Shift+MMB     Zoom  scroll
Views  Numpad 1/3/7  Camera Numpad 0    Frame sel  Numpad .
Mode   Tab           Add  Shift+A
Move G  Rotate R  Scale S   (+ X/Y/Z to lock, type number, Enter)
Select A / Alt+A    Delete X    Duplicate Shift+D
Render still F12     Render anim Ctrl+F12
```

Two settings that fix our current sprite: **square resolution** + **Film ▸ Transparent**. Render at
**256×256 RGBA**, and the workshop will sit cleanly on one tile with grass showing around it.

> Want me to set up the reusable `prop_template.blend` (camera + sun + EEVEE + 256² transparent PNG
> output) for you? I can create/edit the .blend so you just open it and start modeling.
