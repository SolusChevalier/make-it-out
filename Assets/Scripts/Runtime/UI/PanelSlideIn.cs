using System.Collections;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class PanelSlideIn : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private float _duration = 0.35f;
        [SerializeField] private float _offscreenOffsetY = -300f;

        private Vector2 _targetPos;

        private void Awake()
        {
            if (_panel == null)
                _panel = transform as RectTransform;
            if (_panel != null)
                _targetPos = _panel.anchoredPosition;
        }

        private void OnEnable()
        {
            if (_panel == null)
                return;

            StopAllCoroutines();
            StartCoroutine(SlideIn());
        }

        private IEnumerator SlideIn()
        {
            Vector2 start = _targetPos + new Vector2(0f, _offscreenOffsetY);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / _duration;
                _panel.anchoredPosition = Vector2.Lerp(start, _targetPos, EaseOutCubic(t));
                yield return null;
            }

            _panel.anchoredPosition = _targetPos;
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        }
    }
}
