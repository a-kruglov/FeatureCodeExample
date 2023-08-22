using System.Collections.Generic;
using System.Linq;

namespace Example.Locations.ItemChallenge {
    public class ItemChallengeChestConfig : IConfig {
        public string Id { get; set; }

        [JsonProperty("stages")] public List<ItemChallengeChestStage> Stages { get; private set; }
        [JsonProperty("availability_conditions")] public List<EventData> AccessParams { get; private set; }

        public ItemChallengeChestDbData ToStaticData(IStaticDataHelperModule staticDataHelperModule) {
            return new ItemChallengeChestDbData {
                Id = Id,
                Stages = Stages?.Select(stage => stage.ToStaticData(staticDataHelperModule)).ToList(),
                AccessParams = AccessParams?.ToStaticData().ToList()
            };
        }
    }

    public class ItemChallengeChestStage {
        [JsonProperty("required_number_of_Challenges")] public int RequiredNumberOfChallenges { get; private set; }
        [JsonProperty("rewards")] public List<RewardData> Rewards { get; private set; }

        public ItemChallengeChestStageDbData ToStaticData(IStaticDataHelperModule staticDataHelperModule) {
            return new ItemChallengeChestStageDbData {
                RequiredNumberOfChallenges = RequiredNumberOfChallenges,
                Rewards = staticDataHelperModule.RewardToStaticData(Rewards)
            };
        }
    }
}
