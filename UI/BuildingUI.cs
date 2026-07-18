using Godot;
using System.Collections.Generic;
using System;

public partial class BuildingUI : PanelContainer
{
	private Label _header;
	private readonly Dictionary<ResourceType, Label> _labels = new();
	private Building _selected;

	// Panel-drag state (same as the city panel in UIManager).
	private bool _dragging;
	private Vector2 _grabOffset;

	public override void _Ready()
	{
		// This script is attached to your new BuildingPanelContainer node.
		// The node already exists in the scene, so we just fetch children.

		_header = GetNode<Label>("VBoxContainer/Header");
		_header.MouseFilter = MouseFilterEnum.Ignore;

		foreach (ResourceType type in Enum.GetValues<ResourceType>())
		{
			var label = GetNode<Label>($"VBoxContainer/{type}Label");
			label.MouseFilter = MouseFilterEnum.Ignore;
			_labels[type] = label;
		}

		// Make the whole panel a draggable window, exactly like the city panel: the panel itself catches
		// every click (and shields the map underneath), while its contents ignore the mouse so a grab
		// anywhere on it starts a drag.
		MouseFilter = MouseFilterEnum.Stop;
		GetNode<VBoxContainer>("VBoxContainer").MouseFilter = MouseFilterEnum.Ignore;
		GuiInput += OnPanelInput;

		Visible = false; // hidden at start
	}

	// Drag the whole panel by pressing and moving the mouse anywhere on it — same behaviour as the city
	// panel. While the button is held, Godot keeps sending moves here even if the cursor slips off it.
	private void OnPanelInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
		{
			_dragging = mb.Pressed;
			if (mb.Pressed)
				_grabOffset = GetGlobalMousePosition() - GlobalPosition;
		}
		else if (_dragging && @event is InputEventMouseMotion)
		{
			GlobalPosition = GetGlobalMousePosition() - _grabOffset;
		}
	}

	public void ShowBuilding(Building building)
	{
		_selected = building;
		Visible = true;
		Refresh();

		// Open next to the cursor, nudged clear of it, but never past the edge of the screen.
		// Size still holds last frame's value here — Godot only re-lays-out containers on its next
		// layout pass, so on the first open it would be the stale size from the scene file. Ask for the
		// size the contents actually need instead; that one is recomputed on demand.
		Vector2 panelSize = GetCombinedMinimumSize();
		Vector2 spot = GetGlobalMousePosition() + new Vector2(16, 16);
		Vector2 limit = GetViewportRect().Size - panelSize;
		GlobalPosition = new Vector2(
			Mathf.Clamp(spot.X, 0, Mathf.Max(0, limit.X)),
			Mathf.Clamp(spot.Y, 0, Mathf.Max(0, limit.Y)));
	}

	public void Refresh()
	{
		if (_selected == null) return;

		_header.Text = $"{_selected.Type} {_selected.Location}";

		foreach (ResourceType type in Enum.GetValues<ResourceType>())
			_labels[type].Text = $"{type}: {_selected.Stockpile.Get(type)}";
	}

	public void HidePanel()
	{
		Visible = false;
		_selected = null;
	}
}
