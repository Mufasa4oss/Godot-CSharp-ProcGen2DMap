shader_type canvas_item;

//uniform float time;  // Passed in from GDScript

void fragment() {
    vec2 uv = UV;
    float fade_thickness = 0.2;

    // Time-based glow intensity
    float glow_effect = 0.5 + 0.5 * sin(TIME * 2.0);  // Pulsates between 0 and 1

    float dist_to_center = length(uv - vec2(0.5));
    float fade_factor = smoothstep(0.0, fade_thickness, dist_to_center);

    // Get tile color
    vec4 base_color = texture(TEXTURE, uv);

    // Apply pulsating glow
    vec3 final_color = mix(base_color.rgb, vec3(0.0, 0.0, 0.0), fade_factor * glow_effect);

    COLOR = vec4(final_color, base_color.a);
}
