using System;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ChallengeDifficultyTypeData : IWeighted {
        [JsonProperty("difficulty")]
        public int Difficulty { get; set; }
        
        [JsonProperty("weight")]
        public int Weight { get; set; }

        public ChallengeDifficultyTypeDbData ToStaticData() {
            return new ChallengeDifficultyTypeDbData {
                Difficulty = Difficulty,
                Weight = Weight
            };
        }
    }
}
