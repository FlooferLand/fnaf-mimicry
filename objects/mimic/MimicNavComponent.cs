using GodotUtils;

namespace Project.Components;
using Godot;

/// Movement script for characters that use navigation
public partial class MimicNavComponent : Node {
	[Export] public float MovementSpeed = 3f;

	[ExportGroup("Nodes")]
	[Export] public Mimic Character;
	[Export] public MimicBrain Brain;
	[Export] public NavigationAgent3D Agent;
	[Export] public Node3D LooksAtTarget;
	[Export] public Timer FindPlayerTimer;
	[Export] public AudioStreamPlayer3D DebugSfx;

	/// Global position for where the mimic will walk to
	public Node3D Target { get; set; }
	public Vector3 NextPathPosition;

	public override void _Ready() {
		NextPathPosition = Character.GlobalPosition;
		Agent.VelocityComputed += OnVelocityComputed;
		FindPlayerTimer.Timeout += () => {
			float finalPosDistance = Character.GlobalPosition.DistanceTo(Target.GlobalPosition);

			if (finalPosDistance > Agent.PathDesiredDistance) {
				DebugSfx.Play();
				Agent.TargetPosition = Target.GlobalPosition;
			}

			// Updating more often the closer the player is
			// This is better for performance, and close-up navigation
			const float maxDist = 10.0f;
			float charProximity = Mathf.Clamp(Mathf.InverseLerp(0, maxDist, finalPosDistance), 0.0f, 1.0f);
			FindPlayerTimer.WaitTime = Mathf.Max(0.8, charProximity);
		};
	}

	public override void _PhysicsProcess(double delta) {
		float speed = MovementSpeed * (Brain.State == MimicState.Chasing ? 1.5f : 1f);

		// Do not query when the map has never synchronized and is empty, or navigation is finished.
		if (NavigationServer3D.MapGetIterationId(Agent.GetNavigationMap()) != 0 && !Agent.IsNavigationFinished()) {
			NextPathPosition = Agent.GetNextPathPosition();
		}

		var direction = Character.GlobalPosition.DirectionTo(NextPathPosition) * speed;
		var newVelocity = Character.Velocity + (direction - Character.Velocity);
		if (Agent.AvoidanceEnabled) {
			Agent.Velocity = newVelocity;
		} else {
			OnVelocityComputed(newVelocity);
		}

		// Looking at the next path point
		// TODO: Linearly interpolating this won't work because Godot is broken..
		if (LooksAtTarget.GlobalPosition != NextPathPosition) {
			LooksAtTarget.LookAt(NextPathPosition, Vector3.Up, true);
		}
		Character.Pivot.GlobalRotationDegrees = Character.Pivot.GlobalRotationDegrees.Lerp(
			LooksAtTarget.GlobalRotationDegrees,
			3f * (float) delta
		);

		// Actually moving
		Character.MoveAndSlide();
	}

	void OnVelocityComputed(Vector3 safeVelocity) {
		Character.Velocity = safeVelocity;
	}
}
