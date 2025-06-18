using Godot;

namespace Project.Components;

public partial class PlayerInteractionRelay : Node {
    string notice = "Must be the first child of the Area3D";
    [Export] public string Notice {
        get => notice;
        set {}
    }
    
    [Export] public Node3D Target;
}