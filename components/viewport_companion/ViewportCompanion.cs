#nullable enable

namespace Project;
using Godot;

[Tool]
public partial class ViewportCompanion : Control {
	[Export] public SubViewport? Target;
	[Export] public bool Stretch = true;
	
	[ExportGroup("Local")]
	[Export] public TextureRect? TextureRect;
	
	public override void _Ready() {
		if (Stretch) {
			UpdateSize();
			Resized += UpdateSize;
			if (Target != null) Target.SizeChanged += UpdateSize;
		}
	}
	
	public override void _Process(double delta) {
		if (TextureRect != null && Target != null) {
			TextureRect.Texture = Target.GetTexture();
		} else {
			GD.PushError("Error in Process on ViewportCompanion");
		}
	}

	public override void _GuiInput(InputEvent @event) {
		if (Engine.IsEditorHint()) return;
		Target?.PushInput(@event, true);
	}

	public void UpdateSize() {
		if (Target != null) {
			Target.Size = (Vector2I) Size;
		} else {
			GD.PushError("Could not resize. Target is null");
		}
	}
}
