using Godot;
using System;

public partial class UIManager : Control
{
	private Label WoodLabel;
	public Label StoneLabel;
	public Label GrainLabel;
	//CHANGES 23-Jun-26// removed unused ResourceManager rm field //END CHANGES 23-Jun-26//
	private Button _buildRoadButton;
	public bool IsRoadBuildMode { get; private set; } = false;

	public override void _Ready()
	{
		WoodLabel = GetNode<Label>("PanelContainer/VBox/WoodLabel");
		StoneLabel = GetNode<Label>("PanelContainer/VBox/StoneLabel");
		GrainLabel = GetNode<Label>("PanelContainer/VBox/GrainLabel");
		UpdateResourceDisplayAll(0, 0, 0);
		_buildRoadButton = GetNode<Button>("PanelContainer/VBox/Button");
        _buildRoadButton.Toggled += pressed => {
            IsRoadBuildMode = pressed;
            _buildRoadButton.Modulate = pressed ? new Color(0.5f, 1f, 0.5f) : Colors.White;
        };

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
