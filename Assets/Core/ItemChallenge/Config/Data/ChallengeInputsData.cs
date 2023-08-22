using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    [Serializable]
    public class ChallengeInputsData {
        [field : SerializeField] [JsonProperty("availability_conditions")]
        public IEnumerable<EventData> AccessParams { get; set; }
        
        [field : SerializeField] [JsonProperty("resource_id")]
        public string ResourceId { get; set; }
        
        [field : SerializeField] [JsonProperty("extraction_difficulty")]
        public float ExtractionDifficulty { get; set; }
        
        [field : SerializeField] [JsonProperty("extraction_duration_type")] [JsonConverter(typeof(StringEnumConverter))]
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
