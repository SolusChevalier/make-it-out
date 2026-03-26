using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MakeItOut.Runtime.UI
{
    public class UiButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_background == null)
                _background = GetComponent<Image>();
            if (_label == null)
                _label = GetComponentInChildren<TMP_Text>(true);

            if (_button == null || _background == null)
                return;

            _button.targetGraphic = _background;
            _background.color = UiStyle.ButtonNormal;

            ColorBlock colours = _button.colors;
            colours.normalColor = UiStyle.ButtonNormal;
            colours.highlightedColor = UiStyle.ButtonHighlight;
            colours.pressedColor = UiStyle.AccentGold;
            _button.colors = colours;
        }

        public void SetLabel(string text)
        {
            if (_label != null)
                _label.text = text;
        }

        public void AddListener(Action onClick)
        {
            if (_button == null || onClick == null)
                return;

            _button.onClick.AddListener(() => onClick());
        }

        public void SetInteractable(bool value)
        {
            if (_button != null)
                _button.interactable = value;
            if (_label != null)
                _label.color = value ? UiStyle.TextPrimary : UiStyle.TextDim;
            if (_background != null)
                _background.color = value ? UiStyle.ButtonNormal : UiStyle.LockedLevel;
        }
    }
}
