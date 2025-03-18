shader_type canvas_item;

uniform sampler2D province_map; // The dynamically generated texture
uniform vec2 screen_texture_size; // Texture size for proper UV calculations

void fragment() {
    vec2 tex_size = screen_texture_size;
    vec2 uv = FRAGCOORD.xy / tex_size; 

    float threshold = 0.1; // Edge detection threshold

    vec4 center = texture(province_map, uv);
    
    vec4 left   = texture(province_map, uv + vec2(-1.0,  0.0) / tex_size);
    vec4 right  = texture(province_map, uv + vec2( 1.0,  0.0) / tex_size);
    vec4 up     = texture(province_map, uv + vec2( 0.0, -1.0) / tex_size);
    vec4 down   = texture(province_map, uv + vec2( 0.0,  1.0) / tex_size);

    // Compute difference with neighbors
    float edge = length(center.rgb - left.rgb) + length(center.rgb - right.rgb) +
                 length(center.rgb - up.rgb)   + length(center.rgb - down.rgb);

    // Apply threshold to highlight edges
    if (edge > threshold) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0); // Black for edges
    } else {
        COLOR = center; // Keep the original texture colors
    }
}
