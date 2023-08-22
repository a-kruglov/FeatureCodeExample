using Example.Api;

namespace Example.Locations.ItemChallenge  {
    public class ItemChallengeChallengeCompletedSignal : IExtendedGameEventSignal {
        
        public int Index { get; }
        public ChallengeDifficultyType DifficultyType { get; }
        public string SignalIdentifier => $"{IConfig}_slot_completed_{Index}";
        public int Value => 1;

        public ItemChallengeChallengeCompletedSignal(int index, ChallengeDifficultyType difficultyType) {
            Index = index;
            DifficultyType = difficultyType;
        }
    }
}
