#nullable enable
namespace Project;

public interface IPlayerInteractable {
    /// RaycastData may be null.
    public void Interact(FirstPersonCharacter player, RaycastData? ray, InteractState state) {}
}
