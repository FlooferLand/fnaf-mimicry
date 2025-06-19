using GodotUtils;

namespace Project.Components;
using Godot;

public enum MimicState {
	Patrolling, Searching, Found, Chasing
}

/// Handles AI basically
public partial class MimicBrain : Node {
	[Signal] public delegate void StateChangedEventHandler(MimicState newState);

	[Export] public Mimic Mimic;
	[Export] public MimicAnimComponent AnimComponent;
	[Export] public MimicViewCone ViewCone;

	public MimicState State = MimicState.Patrolling;

	public override void _Ready() {
		StateChanged += newState => {
			State = newState;
		};

		ViewCone.FoundTarget += player => {
			EmitSignalStateChanged(MimicState.Found);
			GetTree().CreateTimer(1f).Timeout += () => {
				EmitSignalStateChanged(MimicState.Chasing);
			};
		};
		ViewCone.LostTarget += () => {
			EmitSignalStateChanged(MimicState.Searching);
			GetTree().CreateTimer(10f).Timeout += () => {
				EmitSignalStateChanged(MimicState.Patrolling);
			};
		};
	}

	public override void _Process(double delta) {
		DebugPrinter.Track(this, "mimic_state", State, "Mimic state");
		DebugPrinter.Track(this, "mimic_target", Mimic.NavComponent.Target.Name, "Mimic target");
	}
}