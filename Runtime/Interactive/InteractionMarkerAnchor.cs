using UnityEngine;

namespace FirstPerson.Interaction
{
    /// <summary>
    /// Optional. Marks where an interactable's marker dot and prompt are drawn (an NPC's head, a door handle).
    /// Put it on the interactable's object or any child. Without one, the collider's bounds centre is used.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionMarkerAnchor : MonoBehaviour
    {
        /// <summary>The anchor transform under <paramref name="target"/>, or null. Walks the hierarchy, so cache the result.</summary>
        public static Transform Find(Component target)
        {
            if (target == null) return null;
            InteractionMarkerAnchor anchor = target.GetComponentInChildren<InteractionMarkerAnchor>();
            return anchor != null ? anchor.transform : null;
        }

        /// <summary>World point to draw at: the anchor if there is one, else the collider's centre, else the target's pivot.</summary>
        public static Vector3 Position(Transform anchor, Collider collider, Component target)
        {
            if (anchor != null) return anchor.position;
            if (collider != null) return collider.bounds.center;
            return target != null ? target.transform.position : Vector3.zero;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
#endif
    }
}
