using UnityEngine;

namespace FirstPerson.Interaction
{
    /// <summary>Freezes the player for cutscenes and blackouts: no moving, looking or interacting.</summary>
    public static class PlayerLock
    {
        /// <summary>Stops (true) or restores (false) the player's movement, look and interaction.</summary>
        public static void Set(FirstPersonController player, bool locked)
        {
            if (player == null) return;
            player.enabled = !locked;
            if (player.TryGetComponent(out PlayerInteraction interaction)) interaction.enabled = !locked;
        }

        /// <summary>Height of the player's pivot above the bottom of their capsule, so a teleport lands them standing on the ground.</summary>
        public static float FeetToPivot(FirstPersonController player)
        {
            if (player == null || !player.TryGetComponent(out CharacterController controller)) return 1f;
            float scale = player.transform.lossyScale.y;
            return (controller.height * 0.5f - controller.center.y) * scale + controller.skinWidth;
        }
    }
}
