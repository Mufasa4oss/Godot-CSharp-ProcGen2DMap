shader_type canvas_item;
uniform sampler2D texture_albedo;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	float lineThickness = 0.02;
	float distToCenter = abs(UV.x - 0.5);
	
	float lineIntensity = smoothstep(lineThickness,0.0,distToCenter);
	
	vec4 baseColor = texture(texture_albedo, UV);
	
	vec3 finalColor = mix(vec3(0.0),baseColor.rgb, lineIntensity);
	
	COLOR = vec4(finalColor, baseColor.a);
	
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
