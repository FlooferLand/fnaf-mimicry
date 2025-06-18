using GodotUtils;

namespace Project.Components;
using Godot;

public partial class ScreenEffects : CanvasLayer {
	[Export] public FirstPersonCharacter Character;

	public override void _Process(double delta) {
		/*
		sprintSmooth = Mathf.Lerp(sprintSmooth, Character.Sprinting ? 1.0f : 0.0f, (float) delta * 0.8f);
		DebugPrinter.TrackTemp(this, sprintSmooth, nameof(sprintSmooth));
		SpeedLines.SetShaderParameter("alpha", sprintSmooth);
		*/
	}
}
