using TMPro;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private bool _showTenths;

        private void Awake()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();
        }

        public void SetTime(float seconds)
        {
            int m = (int)seconds / 60;
            float s = seconds % 60f;
            if (_label != null)
            {
                _label.text = _showTenths
                    ? $"{m:D2}:{s:00.0}"
                    : $"{m:D2}:{(int)s:D2}";
            }
        }

        public void SetColor(Color c)
        {
            if (_label != null)
                _label.color = c;
        }
    }
}
