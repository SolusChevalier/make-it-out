using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "MakeItOut/Level Definition")]
    public class LevelDefinitionAsset : ScriptableObject
    {
        public LevelDefinition Definition = new LevelDefinition();

        private void OnValidate()
        {
            if (Definition == null)
                Definition = new LevelDefinition();

            Definition.Validate(name);
        }
    }
}
