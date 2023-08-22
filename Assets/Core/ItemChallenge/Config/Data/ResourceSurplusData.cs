using System;
using Newtonsoft.Json;
using UnityEngine;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ResourceSurplusData {
        [field : SerializeField] [JsonProperty("thresholds_by_player_level")]
        public int[] ThresholdsByPlayerLevel { get; set; }

        public ResourceSurplusDbData ToStaticData() {
            return new ResourceSurplusDbData {
                ThresholdsByPlayerLevel = ThresholdsByPlayerLevel
            };
        }
    }
}
