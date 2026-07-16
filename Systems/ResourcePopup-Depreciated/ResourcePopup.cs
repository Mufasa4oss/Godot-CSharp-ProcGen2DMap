using Godot;
using System;

public partial class ResourcePopup : Node2D
{
	private ShaderMaterial _shaderMaterial;
	private float _fadeTime = 1.0f; // seconds to fade out
	private float _elapsed = 0.0f;
	private bool _fadingOut = false;

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");
		_shaderMaterial = (ShaderMaterial)sprite.Material;
	}

	public void StartFadeOut()
	{
		_fadingOut = true;
	}

	public override void _Process(double delta)
	{
		if (!_fadingOut) return;

		_elapsed += (float)delta;
		float fadeValue = Mathf.Clamp(1.0f - _elapsed / _fadeTime, 0.0f, 1.0f);
		_shaderMaterial.SetShaderParameter("fade", fadeValue);

		if (fadeValue <= 0.0f)
			QueueFree();
	}
}
