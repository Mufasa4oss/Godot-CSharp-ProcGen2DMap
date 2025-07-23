shader_type canvas_item;

uniform float fade : hint_range(0.0, 1.0) = 1.0;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec4 tex_color = texture(TEXTURE, UV);
	tex_color.a *= fade;
	COLOR = tex_color;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
