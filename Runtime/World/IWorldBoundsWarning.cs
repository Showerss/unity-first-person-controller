using System.Collections;

namespace FirstPerson.World
{
    /// <summary>
    /// Something shown on the black screen after <see cref="WorldBoundsWarden"/> puts the player back on the path,
    /// e.g. the dialogue package's WorldBoundsDialogueWarning. Put it on the same object as the warden.
    /// </summary>
    public interface IWorldBoundsWarning
    {
        /// <summary>Shows the warning and finishes once the player has dismissed it.</summary>
        IEnumerator Show();
    }
}
