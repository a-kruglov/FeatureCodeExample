using System;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ChallengeDifficultyModifierData : IWeighted {
        [JsonProperty("multiplier")]
        public float Multiplier { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }

        public ChallengeDifficultyModifierDbData ToStaticData() {
            return new ChallengeDifficultyModifierDbData {
                Multiplier = (float) Multiplier,
                Weight = Weight
            };
        }
    }
}
