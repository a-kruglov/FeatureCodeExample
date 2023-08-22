using System.Collections.Generic;
using System.Linq;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    [InjectGenerate]
    public class ItemChallengeConfigsProvider {
        private readonly IStaticDataModule _staticDataModule;

        public ItemChallengeDbData ItemChallengeData => _staticDataModule.Data.ItemChallenge;

        public ItemChallengeConfigsProvider(IStaticDataModule staticDataModule) {
            _staticDataModule = staticDataModule;
        }

        public ChallengeDbData GetChallengeConfig(string IConfig) => _staticDataModule.Data.Challenges.ContainsKey(IConfig) ? _staticDataModule.Data.Challenges[IConfig] : null;
        public ItemChallengeChestDbData GetChestData(string IConfig) => IConfig != null && _staticDataModule.Data.ItemChallengeChests.ContainsKey(IConfig) ? _staticDataModule.Data.ItemChallengeChests[IConfig] : null;
        
        public IEnumerable<ChallengeDbData> GetAllChallengeConfigs() => ItemChallengeData.Challenges.Select(GetChallengeConfig);
        public IEnumerable<ItemChallengeChestDbData> GetAllChestsData() => _staticDataModule.Data.ItemChallengeChests.Values;
    }
}
