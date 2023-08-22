using System.Collections.Generic;
using System.Linq;


namespace Example.Locations.ItemChallenge {
    public class ItemChallengeConfig : IConfig {
        public static readonly IConfig CONFIG_PATH = new("configs/Challenge_board");
        
        private const int Challenge_SLOTS_COUNT = 4;

        public string Id { get; set; }

        [JsonProperty("delay_after_rejection")]
        public int DelayAfterRejection { get; private set; }
        
        [JsonProperty("slot_cooldown_speedup_free_time")]
        public int SlotCooldownSpeedUpFreeTime { get; private set; }

        [JsonProperty("slot_cooldown_speedup_cost")]
        public GameItem SlotCooldownSpeedUpCost { get; private set; }

        [JsonProperty("Challenges")]
        public List<IConfig> Challenges { get; private set; }
        
        [JsonProperty("chest_configs")]
        public List<IConfig> ChestConfigs { get; private set; }

        public ItemChallengeDbData ToStaticData() {
            return new ItemChallengeDbData {
                ChallengeSlotsCount = Challenge_SLOTS_COUNT,
                DelayAfterRejection = DelayAfterRejection,
                SlotCooldownSpeedUpFreeTime = SlotCooldownSpeedUpFreeTime,
                SlotCooldownSpeedUpCost = new ResourceInfoDbData(SlotCooldownSpeedUpCost.Item, ItemType.Resource, SlotCooldownSpeedUpCost.Value),
                Challenges = Challenges.Select(IConfig => IConfig.Key).ToList(),
                Chests = ChestConfigs.Select(IConfig => IConfig.Key).ToList()
            };
        }
    }
}
