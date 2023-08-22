using System;
using System.Collections.Generic;
using System.Linq;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    internal class ItemChallengeSlot : IItemChallengeSlot {
        private CityBalanceDbData CityBalance => ItemChallengeFeatureDependencies.StaticDataModule.Data.CityBalance;
        
        public int Index { get; set; }
        public ChallengeDifficultyType DifficultyType { get; }
        
        public IEnumerable<GameItem> Inputs { get; }
        public IEnumerable<GameReward> Outputs { get; }

        public ItemChallengeSlot(
            ChallengeDifficultyType difficultyType,
            IEnumerable<GameItem> inputs,
            IEnumerable<GameReward> outputs
        ) {
            Inputs = inputs;
            Outputs = outputs;
            DifficultyType = difficultyType;
        }

        public IEnumerable<GameItem> GetForceFulfillPrice() {
            var itemsRemaining = GetItemsRemaining();
            return GetForceFulfillPrice(itemsRemaining);
        }

        public IEnumerable<GameItem> GetItemsRemaining() {
            return GetResourceInfo(Inputs, (itemValue, current) => Math.Max(itemValue.Value - current, 0));
        }

        private IEnumerable<GameItem> GetForceFulfillPrice(IEnumerable<GameItem> itemsRemaining) {
            var totalfloatPrice = float.zero;
            foreach (var itemRemaining in itemsRemaining) {
                var resourcePrice = CityBalance.ResourcePrices.FirstOrDefault(x => x.ResourceId == itemRemaining.Item);
                if (resourcePrice == null) {
                    ItemChallengeFeatureDependencies.LogModule.Error($"Resource price for {itemRemaining.Item} not found");
                    continue;
                }
                totalfloatPrice += resourcePrice.CrystalPrice;
            }

            var totalPrice = Math.Max(1, totalfloatPrice.ToFloorInt());
            return GetAvailableItems().Append(new GameItem(ItemsConstants.RealId, totalPrice));
        }

        private IEnumerable<GameItem> GetAvailableItems() {
            return GetResourceInfo(Inputs, (itemValue, current) => Math.Min(itemValue.Value, current));
        }

        private static IEnumerable<GameItem> GetResourceInfo(IEnumerable<GameItem> items, Func<GameItem, int, int> calculateValue) {
            if (items == null) {
                return Enumerable.Empty<GameItem>();
            }
            return from itemValue in items
                let current = ItemChallengeFeatureDependencies.ItemsRouter.GetResourceAmount(itemValue.Item)
                let value = calculateValue(itemValue, current)
                where value > 0
                select new GameItem(itemValue.Item, value);
        }
    }
}