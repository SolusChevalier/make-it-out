using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MakeItOut.Runtime.Flow
{
    public class BootstrapSceneLoader : MonoBehaviour
    {
        public string MainMenuSceneName = "MainMenu";

        private IEnumerator Start()
        {
            yield return null;

            AsyncOperation load = SceneManager.LoadSceneAsync(MainMenuSceneName, LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }
        }
    }
}
