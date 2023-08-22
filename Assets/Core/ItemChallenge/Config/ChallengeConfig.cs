using System;
using System.Collections.Generic;
using UnityEngine;

namespace Example.Locations.ItemChallenge {
    [Serializable, LocalizedConfigPath("Challenges")]
    public class ChallengeConfig : IConfig {
        
        public string Id { get;  set; }
        
        [field : SerializeField] [JsonProperty("difficulty_type")]
        public ChallengeDifficultyType DifficultyType { get; set; }
        
        [field : SerializeField] [JsonProperty("availability_conditions")]
        public IEnumerable<EventData> AccessParams { get; private set; }
        
        [field : SerializeField] [JsonProperty("Challenge_items")]
        public IEnumerable<GameItem> ChallengeItems { get; private set; }
        
        [field : SerializeField] [JsonProperty("rewards")]
        public IEnumerable<RewardData> Rewards { get; private set; }

        public ChallengeDbData ToStaticData(IStaticDataHelperModule staticDataHelperModule) {
            return new ChallengeDbData {
                Id = Id,
                AccessParams = AccessParams?.ToStaticData().ToList(),
                ChallengeItems = staticDataHelperModule.ItemsToStaticData(ChallengeItems),
                Rewards = staticDataHelperModule.RewardToStaticData(Rewards)
            };
        }
    }
}
