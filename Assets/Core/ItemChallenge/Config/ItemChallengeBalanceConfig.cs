using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Example.Api;

namespace Example.Locations.ItemChallenge {

    [Serializable]
    public class ItemChallengeBalanceConfig : IConfig {
        public static readonly IConfig CONFIG_PATH = new("rules/Challenge_board_balance");

        public const int MIN_INPUT_SLOTS_AMOUNT = 1;
        public const int MAX_INPUT_SLOTS_AMOUNT = 2;
        public const float SLOT_DIFFICULTY_FIT_FACTOR = 1.5f;
        
        public const int REWARD_SLOTS_AMOUNT = 2;
        public static readonly List<float[]> REWARD_DISTRIBUTION_VARIATIONS = new() {
            new[] {0.5f, 0.5f},
            new[] {0.4f, 0.6f},
            new[] {0.6f, 0.4f}
        };

        public string Id { get; set; }

        [field : SerializeField] [JsonProperty("Challenge_inputs_data")]
        public Dictionary<IConfig, ChallengeInputsData> ChallengeInputsData { get; set; }
        
        [field : SerializeField] [JsonProperty("resources_surplus_data")]
        public Dictionary<IConfig, ResourceSurplusData> ResourcesSurplusData { get; set; }

        [field : SerializeField] [JsonProperty("Challenge_difficulty_types_data")]
        public Dictionary<ChallengeDifficultyType, ChallengeDifficultyTypeData> ChallengeDifficultyTypesData { get; set; }
        
        [field : SerializeField] [JsonProperty("slots_amount_difficulty_modifier_data")]
        public Dictionary<int, ChallengeDifficultyModifierData> SlotsAmountDifficultyModifierData { get; set; }
        
        [field : SerializeField] [JsonProperty("extraction_duration_type_difficulty_modifier_data")]
        public Dictionary<ExtractionDurationType, ChallengeDifficultyModifierData> ExtractionDurationTypeDifficultyModifierData { get; set; }
        
        [field : SerializeField] [JsonProperty("Challenge_purpose_difficulty_modifier_data")]
        public Dictionary<ChallengePurpose, ChallengeDifficultyModifierData> ChallengePurposeDifficultyModifierData { get; set; }
        
        [field : SerializeField] [JsonProperty("Challenge_outputs_data")]
        public Dictionary<IConfig, ChallengeOutputData> ChallengeOutputsData { get; set; }

        public ItemChallengeBalanceDbData ToStaticData() {
            return new ItemChallengeBalanceDbData {
                Inputs = ChallengeInputsData?.ToDictionary(kv => kv.Key.Key, kv => kv.Value.ToStaticData()),
                ResourcesSurplus = ResourcesSurplusData?.ToDictionary(kv => kv.Key.Key, kv => kv.Value.ToStaticData()),
                DifficultyTypes = ChallengeDifficultyTypesData?.ToDictionary(kv => kv.Key, kv => kv.Value.ToStaticData()),
                SlotsAmountDifficultyModifier = SlotsAmountDifficultyModifierData?.ToDictionary(kv => kv.Key, kv => kv.Value.ToStaticData()),
                ExtractionDurationTypeDifficultyModifier = ExtractionDurationTypeDifficultyModifierData?.ToDictionary(kv => kv.Key, kv => kv.Value.ToStaticData()),
                PurposeDifficultyModifier = ChallengePurposeDifficultyModifierData?.ToDictionary(kv => kv.Key, kv => kv.Value.ToStaticData()),
                Outputs = ChallengeOutputsData?.ToDictionary(kv => kv.Key.Key, kv => kv.Value.ToStaticData()),
                Consts = new ItemChallengeBalanceConstsDbData {
                    MinInputSlotsAmount = MIN_INPUT_SLOTS_AMOUNT,
                    MaxInputSlotsAmount = MAX_INPUT_SLOTS_AMOUNT,
                    SlotsDifficultyFitFactor = (float) SLOT_DIFFICULTY_FIT_FACTOR,
                    RewardSlotsAmount = REWARD_SLOTS_AMOUNT,
                    RewardDistributionVariations = REWARD_DISTRIBUTION_VARIATIONS.Select(variation => {
                        var variationDbData = new float[2];
                        variationDbData[0] = (float) variation[0];
                        variationDbData[1] = (float) variation[1];
                        return variationDbData;
                    }).ToList()
                }
            };
        }
    }
}
