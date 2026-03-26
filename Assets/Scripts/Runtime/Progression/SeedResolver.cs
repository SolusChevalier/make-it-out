using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    public static class SeedResolver
    {
        public static int Resolve(LevelDefinition level)
        {
            return level.SeedMode == SeedMode.Fixed
                ? level.FixedSeed
                : Random.Range(int.MinValue, int.MaxValue);
        }
    }
}
