using System;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ResourceSurplusData {
        [JsonProperty("thresholds_by_player_level")]
        public int[] ThresholdsByPlayerLevel { get; set; }

        public ResourceSurplusDbData ToStaticData() {
            return new ResourceSurplusDbData {
                ThresholdsByPlayerLevel = ThresholdsByPlayerLevel
            };
        }
    }
}
