using Example.Api;

namespace Example.Locations.ItemChallenge  {
    public class ItemChallengeStageCompletedSignal : IGameSignal {
        public int Stage { get; }
        
        public ItemChallengeStageCompletedSignal(int stage) {
            Stage = stage;
        }
    }
}
