

shader_type canvas_item;

uniform float time_speed : hint_range(0.1, 2.0) = 1.0;  // Speed of the animation
uniform float wave_strength : hint_range(0.0, 10.0) = 1.0;  // Strength of the wave effect
uniform vec2 tile_size = vec2(64.0,64.0);
void fragment() {
    vec2 uv = UV;

    // Sine wave based on time and the x-coordinate to simulate grass sway
    float wave = sin(uv.x * 10.0 + TIME * time_speed) * wave_strength;

    // Add wave to the y-axis of the texture's UV coordinates to simulate movement
    uv.y += wave;

    // Sample the texture
    vec4 texColor = texture(TEXTURE, uv);

    // Output the color
    COLOR = texColor;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}



