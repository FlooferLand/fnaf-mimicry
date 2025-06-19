using System;
using GodotUtils;

namespace Project;
using Godot;

// Handles footstep, camera shake, etc

public partial class FootstepManager : Node {
	[Export] public FirstPersonCharacter Player;
	[Export] public Node3D HeadBob;
	[Export] public Node3D HeadShake;

	[ExportGroup("Local")]
	[Export] public Timer FootstepTimer;
	[Export] public AudioStreamPlayer FootstepAudioPlayer;

	int stepsLeft = 0;
	float walking = 0f;
	float movingCamera = 0f;
	AudioEffectLowPassFilter lowPasFilterBus;

	float footstepPunch = 0f;
	float time = 0f;
	
	public override void _Ready() {
		FootstepTimer.Timeout += TriggerFootstep;

		// Footsteps audio bus stuff
		int footstepBus = AudioServer.GetBusIndex("PlayerFootsteps");
		lowPasFilterBus = AudioServer.GetBusEffect(footstepBus, 0) as AudioEffectLowPassFilter;
	}

	public override void _Process(double delta) {
		footstepPunch = Mathf.Lerp(footstepPunch, 0f, 10.0f * (float) delta);

		HeadBob.Position = HeadBob.Position
			.WithY(Mathf.Lerp(HeadBob.Position.Y, footstepPunch * -0.5f, 3f * (float) delta));
		HeadBob.Rotation = HeadBob.Rotation
			.WithX(Mathf.Lerp(HeadBob.Rotation.X, (0.5f + (Mathf.Sin(time * 10.0f) * footstepPunch * 0.8f)) * (Mathf.Pi * -0.04f), 5f * (float) delta))
			.WithZ(Mathf.Sin(time * 5.0f) * (walking * (Mathf.Pi * 0.005f)));
		
		// Camera shake
		double amount = 0.2f + (walking * 0.45f) + (movingCamera * 0.45f) + (Player.Fear * 0.8f);
		var shake = new Vector3(
			(float) GD.RandRange(-amount, amount),
			(float) GD.RandRange(-amount, amount),
			(float) GD.RandRange(-amount, amount)
		);
		HeadShake.Rotation = HeadShake.Rotation.Lerp(shake, 0.2f * (float) delta);

		if (walking > 0.1f)
			time += (float) delta;
		else
			time = 0f;
	}

	public void TriggerFootstep() {
		FootstepAudioPlayer.Play();
		footstepPunch = 1f;

		if (stepsLeft > 0) {
			--stepsLeft;
			FootstepTimer.Start();
		}
	}

	public void SetWalking(float value, bool sprinting=false, bool crouching=false) {
		walking = Mathf.Clamp(value, 0f, 1f);

		float newWaitTime = sprinting
			? 0.3f
			: (crouching ? 0.6f : 0.45f);
		if (Math.Abs(newWaitTime - FootstepTimer.WaitTime) > 0.1) {
			FootstepTimer.WaitTime = newWaitTime;
		}

		FootstepAudioPlayer.VolumeLinear = (crouching ? 0.5f : 1.0f);
		lowPasFilterBus.CutoffHz = (crouching ? 2200 : 20500);
		switch (walking) {
			case > 0.1f when FootstepTimer.TimeLeft == 0:
				FootstepTimer.Start();
				break;
			case < 0.1f when FootstepTimer.TimeLeft != 0:
				FootstepTimer.Stop();
				break;
		}
	}

	public void SetCameraMoving(float value) {
		movingCamera = value;
	}

	public void Play(int count) {
		stepsLeft = count;
		TriggerFootstep();
	}
}
