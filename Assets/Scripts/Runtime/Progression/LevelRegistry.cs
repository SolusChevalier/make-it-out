using System;
using System.Collections.Generic;
using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    [Serializable]
    public class LevelRegistry
    {
        [Tooltip("Ordered list of all campaign levels. Index 0 is Level 1.")]
        public List<LevelDefinitionAsset> CampaignLevels = new List<LevelDefinitionAsset>();

        public void Validate()
        {
            for (int i = 0; i < CampaignLevels.Count; i++)
            {
                if (CampaignLevels[i] == null)
                    Debug.LogError($"LevelRegistry: null entry at index {i}.");
            }
        }
    }
}
