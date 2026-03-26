using System.Collections;
using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    public class PlayerVisuals : MonoBehaviour
    {
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void OnLanded(float fallSpeed)
        {
            StopAllCoroutines();
            StartCoroutine(LandSquash(fallSpeed));
        }

        public void OnJump()
        {
            StopAllCoroutines();
            StartCoroutine(JumpSquash());
        }

        private IEnumerator LandSquash(float fallSpeed)
        {
            float intensity = Mathf.Clamp01(fallSpeed / 15f);
            float squashY = Mathf.Lerp(1f, 0.65f, intensity);
            float squashXZ = Mathf.Lerp(1f, 1.3f, intensity);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.08f;
                float y = Mathf.Lerp(squashY, 1f, EaseOutElastic(t));
                float xz = Mathf.Lerp(squashXZ, 1f, EaseOutElastic(t));
                transform.localScale = new Vector3(
                    _baseScale.x * xz,
                    _baseScale.y * y,
                    _baseScale.z * xz);
                yield return null;
            }

            transform.localScale = _baseScale;
        }

        private IEnumerator JumpSquash()
        {
            transform.localScale = new Vector3(_baseScale.x * 1.2f, _baseScale.y * 0.75f, _baseScale.z * 1.2f);
            yield return null;
            transform.localScale = new Vector3(_baseScale.x * 0.8f, _baseScale.y * 1.3f, _baseScale.z * 0.8f);
            yield return new WaitForSeconds(0.12f);
            transform.localScale = _baseScale;
        }

        private static float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }
}
