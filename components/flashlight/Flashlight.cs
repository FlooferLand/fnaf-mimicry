namespace Project;
using Godot;

public partial class Flashlight : Node3D {
	[Export] public FirstPersonCharacter Player;
	[Export] public Light3D Light;
	[Export] public AudioStreamPlayer SwitchSound;
	[Export] public AudioStreamPlayer AmbientSound;
	[Export] public AudioStream TurnOnStream;
	[Export] public AudioStream TurnOffStream;

	public bool On { get; private set; } = false;

	public void SetOn(bool on) {
		Light.SetVisible(on);
		On = on;

		if (Player.ClickAndPointMode) {
			Input.SetCustomMouseCursor(null);
			Input.SetMouseMode(on ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Captured);
		}

		SwitchSound.Stream = (on ? TurnOnStream : TurnOffStream);
		SwitchSound.Play();
		
		if (on) 
			AmbientSound.Play((float) GD.RandRange(0.0, AmbientSound.Stream.GetLength()));
		else
			AmbientSound.Stop();
	}
}
