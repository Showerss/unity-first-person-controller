namespace FirstPerson.Interaction
{
    /// <summary>
    /// Anything the player can look at and press Interact on: doors, lamps, people, pickups.
    /// Put it on (or above) a collider; trigger colliders work too.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called every frame while the player looks at this. Return cached strings; don't allocate.
        /// An empty prompt means "nothing to do right now" and the player looks straight past it.
        /// </summary>
        InteractionPrompt GetPrompt(PlayerInteraction interactor);

        void Interact(PlayerInteraction interactor);
    }
}
