using Godot;
using System;

public partial class UIManager : Control
{
	// References to UI labels
	private Label WoodLabel;
	public Label StoneLabel;
	public Label GrainLabel;
	ResourceManager rm;
	public override void _Ready()
	{
		WoodLabel = GetNode<Label>("PanelContainer/VBox/WoodLabel");
		StoneLabel = GetNode<Label>("PanelContainer/VBox/StoneLabel");
		GrainLabel = GetNode<Label>("PanelContainer/VBox/GrainLabel");
		UpdateResourceDisplayAll(0,0,0);
		
		/*
		PanelContainer pc = GetNode<PanelContainer>("PanelContainer");
		VBoxContainer vbox = pc.GetNode<VBoxContainer>("VBox");
		WoodLabel = vbox.GetNode<Label>("WoodLabel");
		StoneLabel = vbox.GetNode<Label>("StoneLabel");
		GrainLabel = vbox.GetNode<Label>("GrainLabel");
		*/
	}
	
	public void UpdateWoodDisplay(int wood)
	{

	}
	public void UpdateStoneDisplay(int stone)
	{
	}

	public void UpdateGrainDisplay(int grain)
	{
	}
	public void UpdateResourceDisplayAll(int wood, int stone, int grain)
	{
		WoodLabel.Text = $"Wood: {wood}";
		StoneLabel.Text = $"Stone: {stone}";
		GrainLabel.Text = $"Grain: {grain}";
	}
}
