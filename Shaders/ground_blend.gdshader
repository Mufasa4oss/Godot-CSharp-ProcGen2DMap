shader_type canvas_item;

// Two tiling ground textures, cross-faded by a noise mask for soft pockets.
uniform sampler2D sand_tex : repeat_enable, filter_linear;
uniform sampler2D grass_tex : repeat_enable, filter_linear;
uniform sampler2D mask_noise : repeat_enable, filter_linear;

// How many times the ground textures repeat across the surface.
uniform float tiles = 8.0;
// How many times the noise repeats -> roughly how many pockets appear.
uniform float mask_tiles = 3.0;
// Noise cutoff (mask is 0..1): higher = more sand, lower = more grass.
uniform float threshold = 0.5;
// Width of the blended border between grass and sand. 0 = hard edge.
uniform float softness = 0.08;

void fragment() {
	vec2 ground_uv = UV * tiles;
	vec4 sand = texture(sand_tex, ground_uv);
	vec4 grass = texture(grass_tex, ground_uv);

	float n = texture(mask_noise, UV * mask_tiles).r;
	float blend = smoothstep(threshold - softness, threshold + softness, n);

	COLOR = mix(sand, grass, blend);
}
