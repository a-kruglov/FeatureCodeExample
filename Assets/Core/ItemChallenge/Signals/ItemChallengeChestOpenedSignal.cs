using System.Collections.Generic;
using Example.Api;

namespace Example.Locations.ItemChallenge  {
    public class ItemChallengeChestOpenedSignal : IGameSignal {
        public List<RewardData> Rewards { get; }
        
        public ItemChallengeChestOpenedSignal(List<RewardData> rewards) {
            Rewards = rewards;
        }
    }
}
