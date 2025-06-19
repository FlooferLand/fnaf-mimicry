namespace Project.Components;
using Godot;

public partial class MimicTauntComponent : Node {
	[Export] public MimicBrain Brain;
	[Export] public AudioStreamPlayer3D TauntAudio;

	AudioStreamPlaybackInteractive playback;

	public override void _Ready() {
		playback = TauntAudio.GetStreamPlayback() as AudioStreamPlaybackInteractive;

		Brain.StateChanged += state => {
			switch (state) {
				case MimicState.Found: {
					playback.SwitchToClipByName("found");
					break;
				}
				case MimicState.Patrolling: {
					playback.SwitchToClipByName("patrolling");
					break;
				}
				case MimicState.Searching: {
					playback.SwitchToClipByName("searching");
					break;
				}
			}
		};

		TauntAudio.Finished += () => {
			GetTree().CreateTimer(GD.RandRange(0.8, 3.0)).Timeout += () => {
				if (Brain.State is MimicState.Patrolling or MimicState.Searching) {
					playback.Start();
				}
			};
		};
	}
}
