using System.Collections;
using UnityEngine;

namespace FirstPerson.UI
{
    /// <summary>
    /// A full-screen black overlay. Set its darkness directly or fade it over time.
    /// Sits below the dialogue box so lines can be read on a black screen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        private CanvasGroup _group;

        /// <summary>0 = clear, 1 = fully black.</summary>
        public float Alpha
        {
            get => _group.alpha;
            set => _group.alpha = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        /// <summary>Fades from the current darkness to <paramref name="target"/>. Uses unscaled time so pausing doesn't freeze it.</summary>
        public IEnumerator FadeTo(float target, float seconds)
        {
            float start = Alpha;
            if (seconds <= 0f)
            {
                Alpha = target;
                yield break;
            }

            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            Alpha = target;
        }
    }
}
