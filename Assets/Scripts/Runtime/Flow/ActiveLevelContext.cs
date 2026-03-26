using MakeItOut.Runtime.Progression;

namespace MakeItOut.Runtime.Flow
{
    public class ActiveLevelContext
    {
        public LevelDefinition Definition { get; set; }
        public int LevelIndex { get; set; }
        public int ResolvedSeed { get; set; }

        public float ElapsedSeconds { get; set; }
        public int StarsEarned { get; set; }
        public bool IsComplete { get; set; }
        public bool IsPersonalBest { get; set; }
        public int OrientationSwitchCount { get; set; }

        public void Reset()
        {
            ElapsedSeconds = 0f;
            StarsEarned = 0;
            IsComplete = false;
            IsPersonalBest = false;
            OrientationSwitchCount = 0;
        }
    }
}
