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

            // Only auto-run in dev scenes (names starting with "Dev").
            // This component is also present in the MainMenu scene for legacy
            // reasons but must not auto-navigate there.
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!sceneName.StartsWith("Dev"))
                yield break;

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
