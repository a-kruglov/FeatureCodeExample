using System.Collections.Generic;

namespace Example.Api {
    public interface IItemChallengeSlot {
        int Index { get; }
        
        ChallengeDifficultyType DifficultyType { get; }
        IEnumerable<GameItem> Inputs { get; }
        IEnumerable<GameReward> Outputs { get; }
        
        IEnumerable<GameItem> GetForceFulfillPrice();
        public IEnumerable<GameItem> GetItemsRemaining();
    }
}
