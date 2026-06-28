shader_type canvas_item;

// Realistic blended ground for the whole map, driven by a per-cell terrain mask instead of the old
// per-tile stamps. Grass and sand are sampled continuously in world space (so there's no repeating
// grid) and cross-faded by the mask; water is a flat color (matching the old water tile) with a
// crisp shore. Generalized from ground_blend.gdshader (random-noise mask -> real terrain mask).
uniform sampler2D grass_tex : repeat_enable, filter_linear;
uniform sampler2D sand_tex : repeat_enable, filter_linear;
// One texel per map cell. R = sandiness (0 grass .. 1 sand), G = water (1 = water). Linear filtering
// blends neighbouring cells, giving soft grass/sand borders for free.
uniform sampler2D terrain_mask : repeat_disable, filter_linear;

// Map size in pixels (cells * tile size). Keeps ground texels square on a non-square map.
uniform vec2 map_px = vec2(16384.0, 8192.0);
// On-screen pixels per ground-texture tile. Higher = the grass/sand pattern reads bigger.
uniform float ground_px = 512.0;
// Width of the soft grass<->sand border, in mask units (0..0.5). 0 = hard edge.
uniform float softness = 0.25;
// Flat water color (sampled from the old Water.png so the look is unchanged for now).
uniform vec4 water_color : source_color = vec4(0.278, 0.671, 0.663, 1.0);

void fragment() {
	// Continuous world-space UV so the textures tile across the whole map, not per cell.
	vec2 ground_uv = UV * map_px / ground_px;
	vec4 grass = texture(grass_tex, ground_uv);
	vec4 sand = texture(sand_tex, ground_uv);

	vec4 m = texture(terrain_mask, UV);
	float sand_amt = smoothstep(0.5 - softness, 0.5 + softness, m.r);
	vec4 ground = mix(grass, sand, sand_amt);

	// Crisp shoreline for now; soft ocean blend is deferred until water has a real texture.
	float water = step(0.5, m.g);
	COLOR = mix(ground, water_color, water);
}
