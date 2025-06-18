namespace Project;
using Godot;

public partial class EditorOnly : Node {
	public override void _EnterTree() {
		QueueFree();
	}
}
