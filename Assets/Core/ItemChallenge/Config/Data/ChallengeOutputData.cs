using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ChallengeOutputData {
        
        [field : SerializeField ] [ JsonProperty("total_output_amount")]
        public int? TotalOutputAmount { get; set; }
        
        [field : SerializeField] [JsonProperty("difficulty_price_by_player_level")]
        public float[] OutputsPerDifficultyByPlayerLevel { get; set; }
        
        [field : SerializeField] [JsonProperty("weight_by_slot")]
        public int[] WeightBySlot { get; set; }
        
        [field : SerializeField] [JsonProperty("weight_by_difficulty")]
        public Dictionary<ChallengeDifficultyType, int> WeightByDifficulty { get; set; }

        public ChallengeOutputDbData ToStaticData() {
            return new ChallengeOutputDbData {
                TotalOutputAmount = TotalOutputAmount,
                OutputsPerDifficultyByPlayerLevel = OutputsPerDifficultyByPlayerLevel?.Select(value => (float) value).ToArray(),
                WeightBySlot = WeightBySlot,
                WeightByDifficulty = WeightByDifficulty
            };
        }
    }
}
