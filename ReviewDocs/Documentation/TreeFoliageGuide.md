# Tree/Foliage Guide — top-down leaf-card trees for the map

Companion to [BlenderGuide.md](BlenderGuide.md) (read that first for Blender basics/navigation/render
settings). This one is scoped to **trees specifically**, rendered for our fixed top-down `Camera2D`
(see [Camera.cs](../../Systems/Cameras/Camera.cs) — the game camera never rotates or tilts, it always
looks straight down).

## The core idea

A tree = a simple trunk/branch **mesh** + a canopy made of **leaf-card materials** (flat alpha-cutout
planes textured with a leaf-cluster image), not modeled leaf-by-leaf and not full billboard geometry.
Leaf cards are cheap and read fine because from far away individual leaf shapes don't matter — only the
overall canopy silhouette/color does.

**Do NOT build a camera-facing billboard rig** (Copy Rotation constraint + duplicate-for-shadows trick).
That technique exists for 3D games where the camera rotates/orbits around objects — ours never does, it's
locked top-down. A flat card lying parallel to the ground is *already* facing the camera dead-on with zero
rotation logic. We're also not rendering live in Godot — we bake one static top-down Blender render to a
PNG sprite, so there's no runtime camera to track at all.

## Steps

1. **Model the trunk** — simple low-poly mesh, cylinder-ish with a taper, maybe a couple of branch
   splits. This is the only "real" geometry; don't over-detail it, it'll barely be visible under canopy
   from directly above.
2. **Make the leaf-card material:**
   - Add a plane (or a few crossed planes through the canopy volume for fullness).
   - New material ▸ plug a leaf-cluster texture (a photo/painted image of a clump of leaves with
     transparent background) into **Base Color**.
   - Plug the same texture's alpha (or a separate B/W mask) into **Alpha**.
   - Set **Blend Mode = Alpha Clip** (or Hashed) so the transparent parts don't render as a solid card.
3. **Build the canopy** — duplicate (`Shift+D`) leaf cards around the branch structure until the canopy
   reads as a full, dense round mass from above. Favor **many small clusters over a few large ones** —
   sparse big cards read as "flat cutout," dense small ones read as "solid foliage."
4. **Hide the base** — don't show bare roots/trunk-ground contact, it breaks scale and makes the tree
   look like it's floating. Either let the canopy overhang enough to hide it from the top-down angle, or
   plan for ground-texture/shadow to cover it once placed on a tile.
5. **Scale to the tile, not by eye** — tile size is 64px (`TileSize` in
   [Camera.cs:13](../../Systems/Cameras/Camera.cs)). Decide how many tiles wide a mature canopy should
   span *before* scaling, apply the scale in Blender (`Ctrl+A` ▸ Scale) so it's baked in, not left as a
   transform.
6. **Camera + render** — reuse the `prop_template.blend` idea from BlenderGuide.md, but for trees the
   camera should point **straight down** (orthographic, no tilt) since that's the only angle the game
   ever shows. Frame the canopy fully in a square render, Film ▸ Transparent ✔, output PNG/RGBA.
7. **Sanity check in-engine** — drop the rendered PNG on a tile and compare its footprint against
   neighboring tiles/roads at actual game zoom before making more variants.

## Quick checklist for "does this tree look right"

- [ ] Canopy reads as one dense round/blobby mass from directly above (no bald patches, no visible
      skeleton branches)
- [ ] No visible bare trunk/root base
- [ ] Scale matches the 64px tile grid (canopy footprint decided in tiles, not eyeballed)
- [ ] Square render, transparent background, straight-down orthographic camera
- [ ] Same lighting setup reused across all tree variants so they match each other on the map
