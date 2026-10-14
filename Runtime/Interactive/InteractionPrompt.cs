using System;

namespace FirstPerson.Interaction
{
    /// <summary>
    /// What the HUD shows while the player looks at an interactable.
    /// Prompts are polled every frame, so implementers should build them from cached strings.
    /// </summary>
    public readonly struct InteractionPrompt : IEquatable<InteractionPrompt>
    {
        public static readonly InteractionPrompt None = default;

        /// <param name="text">The action ("Open door") or message ("The gate is barred."). Never include the key.</param>
        /// <param name="showKey">True when pressing Interact does what the text says, so the HUD shows the key.</param>
        public InteractionPrompt(string text, bool showKey = true)
        {
            Text = text;
            ShowKey = showKey;
        }

        public string Text { get; }
        public bool ShowKey { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Text);

        /// <summary>HUD-ready text, e.g. "[E] Open door". Allocates, so call it when the prompt changes, not every frame.</summary>
        public string Format(string keyLabel)
        {
            if (IsEmpty) return string.Empty;
            return ShowKey && !string.IsNullOrEmpty(keyLabel) ? $"[{keyLabel}] {Text}" : Text;
        }

        public bool Equals(InteractionPrompt other) => ShowKey == other.ShowKey && string.Equals(Text, other.Text);
        public override bool Equals(object obj) => obj is InteractionPrompt other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Text, ShowKey);
    }
}
