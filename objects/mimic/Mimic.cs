using GodotUtils;

namespace Project;
using Godot;

public partial class Mimic : CharacterBody3D {
	[Export] public FirstPersonCharacter TargetCharacter;

	[ExportGroup("Local")]
	[Export] public NavigationAgent3D Agent;
	[Export] public Timer FindPlayerTimer;
	[Export] public AnimationPlayer AnimPlayer;
	[Export] public Node3D Pivot;
	[Export] public AudioStreamPlayer3D DebugSfx;

	float gravity = (float) ProjectSettings.GetSetting("physics/3d/default_gravity");
	const float MovementSpeed = 8f;

	public override void _Ready() {
		FindPlayerTimer.Timeout += () => {
			float finalPosDistance = GlobalPosition.DistanceTo(TargetCharacter.GlobalPosition);

			if (finalPosDistance > Agent.PathDesiredDistance) {
				DebugSfx.Play();
				Agent.TargetPosition = TargetCharacter.GlobalPosition;
			}

			// Updating more often the closer the player is
			// This is better for performance, and close-up navigation
			const float maxDist = 10.0f;
			float charProximity = Mathf.Clamp(Mathf.InverseLerp(0, maxDist, finalPosDistance), 0.0f, 1.0f);
			FindPlayerTimer.WaitTime = Mathf.Max(0.5, charProximity);
			GD.Print(FindPlayerTimer.WaitTime);
		};

		AnimPlayer.Play("FastWalk");
		Agent.VelocityComputed += OnVelocityComputed;
		Agent.PathChanged += () => {
			AnimPlayer.Play("FastWalk", 0.5f);
		};
		Agent.TargetReached += () => {
			if (AnimPlayer.CurrentAnimation != "Look")
				AnimPlayer.Play("Look", 0.5f);
		};
	}

	public override void _PhysicsProcess(double delta) {
		#region Updating navigation
		// Calculating where it's going
		Vector3 nextPathPosition = GlobalPosition;

		// Do not query when the map has never synchronized and is empty, or navigation is finished.
		if (NavigationServer3D.MapGetIterationId(Agent.GetNavigationMap()) != 0 && !Agent.IsNavigationFinished()) {
			nextPathPosition = Agent.GetNextPathPosition();
		}

		var direction = GlobalPosition.DirectionTo(nextPathPosition) * MovementSpeed;
		var newVelocity = Velocity + (direction - Velocity);
		if (Agent.AvoidanceEnabled) {
			Agent.Velocity = newVelocity;
		} else {
			OnVelocityComputed(newVelocity);
		}
		#endregion

		// Gravity
		if (!IsOnFloor()) {
			Velocity += Vector3.Down * gravity * (float) delta;
		}

		// Looking
		// TODO: LERP
		var newRotation = Pivot.RotationDegrees;
		if (Pivot.GlobalPosition != nextPathPosition) {
			Pivot.LookAt(nextPathPosition);
			newRotation = new Vector3(0f, Pivot.RotationDegrees.Y + 180f, 0f);
		}
		Pivot.RotationDegrees = newRotation;

		// Moving
		MoveAndSlide();
	}

	void OnVelocityComputed(Vector3 safeVelocity) {
		Velocity = safeVelocity;
	}
}
