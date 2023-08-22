using System;
using Newtonsoft.Json;
using UnityEngine;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ChallengeDifficultyTypeData : IWeighted {
        [field : SerializeField] [JsonProperty("difficulty")]
        public int Difficulty { get; set; }
        
        [field : SerializeField] [JsonProperty("weight")]
        public int Weight { get; set; }

        public ChallengeDifficultyTypeDbData ToStaticData() {
            return new ChallengeDifficultyTypeDbData {
                Difficulty = Difficulty,
                Weight = Weight
            };
        }
    }
}
