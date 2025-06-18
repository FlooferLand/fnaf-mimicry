namespace Project;

public interface IPlayerHoverable {
    public void HoverBegin(FirstPersonCharacter player, RaycastData ray) {}
    public void HoverHold(FirstPersonCharacter player, RaycastData ray) {}
    public void HoverEnd(FirstPersonCharacter player) {}
}