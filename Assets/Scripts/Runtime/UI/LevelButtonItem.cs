using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using TMPro;
using UnityEngine;

namespace MakeItOut.Runtime.UI
{
    /// <summary>
    /// Attach this to the root of the LevelButton prefab.
    /// Assign all references in the prefab inspector — the Awake fallbacks are
    /// only a safety net for manually-built scene objects.
    /// </summary>
    public class LevelButtonItem : MonoBehaviour
    {
        [SerializeField] private UiButton _button;
        [SerializeField] private TMP_Text _numberLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _gridSizeLabel;
        [SerializeField] private StarRatingDisplay _stars;
        [SerializeField] private GameObject _lockIcon;

        private void Awake()
        {
            // Fallback auto-wiring — only kicks in when a field was left unassigned.
            // Prefer assigning everything in the prefab inspector.
            if (_button == null)
                _button = GetComponent<UiButton>();
            if (_numberLabel == null)
                _numberLabel = transform.Find("LevelNumber")?.GetComponent<TMP_Text>();
            if (_nameLabel == null)
                _nameLabel = transform.Find("LevelName")?.GetComponent<TMP_Text>();
            if (_gridSizeLabel == null)
                _gridSizeLabel = transform.Find("GridSizeLabel")?.GetComponent<TMP_Text>();
            if (_stars == null)
                _stars = transform.Find("StarRatingDisplay")?.GetComponent<StarRatingDisplay>();
            if (_lockIcon == null)
                _lockIcon = transform.Find("LockIcon")?.gameObject;
        }

        public void Bind(int index, LevelDefinition def, bool unlocked, int stars, float bestTime)
        {
            if (_button == null)       { Debug.LogWarning($"[LevelButtonItem] _button is null on '{name}'",       this); return; }
            if (_numberLabel == null)  { Debug.LogWarning($"[LevelButtonItem] _numberLabel is null on '{name}'",  this); return; }
            if (_nameLabel == null)    { Debug.LogWarning($"[LevelButtonItem] _nameLabel is null on '{name}'",    this); return; }
            if (_gridSizeLabel == null){ Debug.LogWarning($"[LevelButtonItem] _gridSizeLabel is null on '{name}'",this); return; }
            if (_stars == null)        { Debug.LogWarning($"[LevelButtonItem] _stars is null on '{name}'",        this); return; }
            if (_lockIcon == null)     { Debug.LogWarning($"[LevelButtonItem] _lockIcon is null on '{name}'",     this); return; }

            _numberLabel.text  = $"{index + 1:D2}";
            _nameLabel.text    = def.DisplayName;
            _gridSizeLabel.text = $"{def.GridSize} x {def.GridSize} x {def.GridSize}";
            _stars.SetStars(stars);
            _lockIcon.SetActive(!unlocked);
            _button.SetInteractable(unlocked);

            if (unlocked)
                _button.AddListener(() => GameManager.Instance.SelectLevel(index));
        }
    }
}
