using Example.Api;

namespace Example.Locations.ItemChallenge {
    public class ItemChallengeChallengeRefusedSignal : IGameSignal {
        public ChallengeDifficultyType DifficultyType { get; }
        
        public ItemChallengeChallengeRefusedSignal(ChallengeDifficultyType difficultyType) {
            DifficultyType = difficultyType;
        }
    }
}
