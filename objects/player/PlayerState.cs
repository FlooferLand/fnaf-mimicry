using Godot;

namespace Project;

/// The player's state manager.
/// This class manages player-related game things, without bloating up the character controller as a result
/// This includes populating fields on the character controller with game-related things.
public partial class PlayerState : Node {
    // Nodes
    [Export] public FirstPersonCharacter CharacterController;
}
