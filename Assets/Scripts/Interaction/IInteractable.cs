namespace Mouth.Interaction
{
    public interface IInteractable
    {
        CursorType GetCursorType();
        void HandleInteraction(Interaction interaction);
    }
}