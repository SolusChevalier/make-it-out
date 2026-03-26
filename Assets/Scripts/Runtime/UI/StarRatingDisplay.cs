using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MakeItOut.Runtime.UI
{
    public class StarRatingDisplay : MonoBehaviour
    {
        [SerializeField] private Image[] _starImages;

        private void Awake()
        {
            if (_starImages == null || _starImages.Length == 0)
                _starImages = GetComponentsInChildren<Image>(true);
        }

        public void SetStars(int filled)
        {
            if (_starImages == null)
                return;

            filled = Mathf.Clamp(filled, 0, 5);
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] != null)
                    _starImages[i].color = i < filled ? UiStyle.StarFilled : UiStyle.StarEmpty;
            }
        }

        public IEnumerator AnimateReveal(int targetStars, float delayPerStar = 0.25f)
        {
            targetStars = Mathf.Clamp(targetStars, 0, 5);
            SetStars(0);
            for (int i = 1; i <= targetStars; i++)
            {
                SetStars(i);
                yield return new WaitForSeconds(delayPerStar);
            }
        }
    }
}
