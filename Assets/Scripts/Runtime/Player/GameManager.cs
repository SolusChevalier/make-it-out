using UnityEngine;
using UnityEngine.SceneManagement;

namespace MakeItOut.Runtime.Player
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void TriggerWin()
        {
            Debug.Log("WIN — player reached exit");
        }

        public void TriggerFail()
        {
            Debug.Log("FAIL");
        }

        public void RestartRun()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
