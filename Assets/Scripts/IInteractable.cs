public interface IInteractable
{
    TriggerType GetTriggerType();
    void HandleInteraction(PlayerActions playerActions);
}