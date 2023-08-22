using Example.Api;

namespace Example.Locations.ItemChallenge  {
    public class ItemChallengeChestProgressSignal : IGameSignal {
        public int Delta { get; }
        public int NewProgress { get; }
        
        public ItemChallengeChestProgressSignal(int delta, int newProgress) {
            Delta = delta;
            NewProgress = newProgress;
        }
    }
}
