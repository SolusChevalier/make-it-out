using System.Collections;
using MakeItOut.Runtime.Player;
using UnityEngine;

namespace MakeItOut.Runtime.Dev
{
    public sealed class DevSceneBootstrap : MonoBehaviour
    {
        public string CurrentSeedLabel => "quick-start";

        private IEnumerator Start()
        {
            yield return null;

            if (GameManager.Instance == null)
            {
                Debug.LogError("DevSceneBootstrap: GameManager not found. Ensure Bootstrap scene is loaded first.");
                yield break;
            }

            GameManager.Instance.GoToLevelSelect();
            GameManager.Instance.SelectLevel(0);
            GameManager.Instance.ConfirmLevelStart();
        }
    }
}
