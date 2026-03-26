using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MakeItOut.Runtime.UI
{
    public class SwitchFlashController : MonoBehaviour
    {
        public static SwitchFlashController Instance { get; private set; }

        [SerializeField] private Image _flash;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_flash == null)
                _flash = GetComponent<Image>();

            if (_flash != null)
                _flash.color = Color.clear;
        }

        public void Flash()
        {
            if (_flash == null)
                return;

            StopAllCoroutines();
            StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            _flash.color = new Color(1f, 1f, 1f, 0.12f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.2f;
                _flash.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.12f, 0f, t));
                yield return null;
            }

            _flash.color = Color.clear;
        }
    }
}
