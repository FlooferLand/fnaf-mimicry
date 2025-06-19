using Project.Components;

namespace Project;
using Godot;

public partial class Mimic : CharacterBody3D {
	[Export] public Node3D TestTarget;

	[ExportGroup("Local")]
	[Export] public Node3D Pivot;
	[Export] public MimicNavComponent NavComponent;

	public override void _Ready() {
		// TODO: Do random patrol pattern
		NavComponent.Target = TestTarget;
	}
}
