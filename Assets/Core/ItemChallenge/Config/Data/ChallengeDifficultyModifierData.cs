using System;
using Newtonsoft.Json;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ChallengeDifficultyModifierData : IWeighted {
        [field : SerializeField] [JsonProperty("multiplier")]
        public float Multiplier { get; set; }

        [field : SerializeField] [JsonProperty("weight")]
        public int Weight { get; set; }

        public ChallengeDifficultyModifierDbData ToStaticData() {
            return new ChallengeDifficultyModifierDbData {
                Multiplier = (float) Multiplier,
                Weight = Weight
            };
        }
    }
}
