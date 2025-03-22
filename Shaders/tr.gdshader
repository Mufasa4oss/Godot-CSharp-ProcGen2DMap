shader_type canvas_item;

uniform sampler2D province_map;
uniform vec2 screen_texture_size;

void fragment() {
    vec2 uv = UV; // Use SCREEN_UV instead of FRAGCOORD
    vec4 center = texture(province_map, uv);

    vec4 left   = texture(province_map, uv + vec2(-1.0,  0.0) / screen_texture_size);
    vec4 right  = texture(province_map, uv + vec2( 1.0,  0.0) / screen_texture_size);
    vec4 up     = texture(province_map, uv + vec2( 0.0, -1.0) / screen_texture_size);
    vec4 down   = texture(province_map, uv + vec2( 0.0,  1.0) / screen_texture_size);

    float edge = length(center.rgb - left.rgb) + length(center.rgb - right.rgb) +
                 length(center.rgb - up.rgb)   + length(center.rgb - down.rgb);

    if (edge > 0.1) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0); // Edge color
}
}
