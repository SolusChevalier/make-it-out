using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    [CreateAssetMenu(fileName = "LevelRegistry", menuName = "MakeItOut/Level Registry")]
    public class LevelRegistryAsset : ScriptableObject
    {
        public LevelRegistry Registry = new LevelRegistry();

        private void OnValidate()
        {
            if (Registry == null)
                Registry = new LevelRegistry();

            Registry.Validate();
        }
    }
}
