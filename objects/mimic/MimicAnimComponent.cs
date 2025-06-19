namespace Project.Components;
using Godot;

public partial class MimicAnimComponent : Node {
	[Export] public AnimationPlayer AnimPlayer;

	[Export] public Mimic Mimic;
	[Export] public MimicBrain Brain;
	[Export] public NavigationAgent3D Agent;

	#region Parameters
	[Export] public bool Idle = true;
	[Export] public bool Walking = false;
	[Export] public bool Running = false;

	[Export] public bool Searching = false;
	[Export] public bool Chasing = false;
	#endregion

	public override void _Process(double delta) {
		bool moving = Mimic.Velocity.Length() > 0.1f;

		// Setting other parameters
		Searching = (Brain.State == MimicState.Searching);
		Chasing = (Brain.State == MimicState.Chasing);

		// Setting movement parameters
		Idle = !moving;
		Walking = moving;
		Running = Chasing;

		// Animating
		const float blend = 0.6f;
		if (Idle) {
			AnimPlayer.Play("Idle", blend);
		} else if (Running || Chasing) {
			AnimPlayer.Play("FastWalk", blend);
		} else if (Walking) {
			AnimPlayer.Play("Walk", blend);
		} else if (Searching) {
			AnimPlayer.Play("Look", blend);
		}
	}
}
