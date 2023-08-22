using System;
using System.Collections.Generic;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ChallengeInputsData {
        [JsonProperty("availability_conditions")]
        public IEnumerable<EventData> AccessParams { get; set; }
        
        [JsonProperty("resource_id")]
        public string ResourceId { get; set; }
        
        [JsonProperty("extraction_difficulty")]
        public float ExtractionDifficulty { get; set; }
        
        [JsonProperty("extraction_duration_type")] [JsonConverter(typeof(StringEnumConverter))]
        public ExtractionDurationType ExtractionDurationType { get; set; }

        public ChallengeInputsDbData ToStaticData() {
            return new ChallengeInputsDbData {
                AccessParams = AccessParams?.ToStaticData().ToList(),
                ResourceId = ResourceId,
                ExtractionDifficulty = (float)ExtractionDifficulty,
                ExtractionDurationType = ExtractionDurationType
            };
        }
    }
}
