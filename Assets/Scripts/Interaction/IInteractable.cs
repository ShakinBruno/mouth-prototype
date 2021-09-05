namespace Mouth.Interaction
{
    public interface IInteractable
    {
        TriggerType GetTriggerType();
        void HandleInteraction(Interaction interaction);
    }
}