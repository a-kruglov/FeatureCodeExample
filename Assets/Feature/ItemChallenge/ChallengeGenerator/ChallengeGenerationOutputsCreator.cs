using System;
using System.Collections.Generic;
using System.Linq;


namespace Example.Locations.ItemChallenge {
    internal class ChallengeGenerationOutputsCreator {
        public IEnumerable<GameReward> Create(ChallengeGenerationContext context) {
            var configs = GetRewardConfigs(context);
            return CreateRewards(context, configs);
        }

        private IConfig[] GetRewardConfigs(ChallengeGenerationContext context) {
            var configs = new IConfig[context.BalanceData.Consts.RewardSlotsAmount];
            for (int slotIndex = 0; slotIndex < context.BalanceData.Consts.RewardSlotsAmount; slotIndex++) {
                configs[slotIndex] = GetRandomRewardIConfig(context, slotIndex);
            }
            return configs;
        }
        
        private IConfig GetRandomRewardIConfig(ChallengeGenerationContext context, int slotIndex) {
            var availableRewardConfigs = context.BalanceData.Outputs
                .Where(d => d.Value.WeightBySlot[slotIndex] > 0 && d.Value.WeightByDifficulty[context.DifficultyType] > 0)
                .Select(d => d.Key)
                .ToList();

            if (!availableRewardConfigs.Any()) {
                throw new Exception($"No reward configs available for slot {slotIndex} and difficulty {context.DifficultyType}");
            }
            return ItemChallengeFeatureDependencies.RandomModule.ChooseRandom(availableRewardConfigs);
        }

        private IEnumerable<GameReward> CreateRewards(ChallengeGenerationContext context, IReadOnlyList<IConfig> rewardConfigs) {
            var totalAllocatedDifficulty = context.AllocatedDifficulty;
            bool isRandomDistribution = InitializeRewardsWithDefinedAmounts(context, rewardConfigs, out var result);

            if (isRandomDistribution) {
                DistributeRewardsRandomly(context, rewardConfigs, result, totalAllocatedDifficulty);
            } else {
                DistributeRemainingDifficulty(context, rewardConfigs, result, totalAllocatedDifficulty);
            }
            context.AllocatedDifficulty = totalAllocatedDifficulty;
            return result;
        }

        private bool InitializeRewardsWithDefinedAmounts(ChallengeGenerationContext context, IReadOnlyList<IConfig> rewardConfigs, out GameReward[] result) {
            bool isRandomDistribution = true;
            result = new GameReward[rewardConfigs.Count];
            for (int i = 0; i < rewardConfigs.Count; i++) {
                IConfig config = rewardConfigs[i];
                if (context.BalanceData.Outputs[config].TotalOutputAmount == null) {
                    continue;
                }
                var amount = context.BalanceData.Outputs[config].TotalOutputAmount.Value;
                var playerLevel = ItemChallengeFeatureDependencies.PlayerDossier.Level;
                var outputsPerDifficulty = context.BalanceData.Outputs[config].OutputsPerDifficultyByPlayerLevel[playerLevel];
                var rewardDifficulty = amount / outputsPerDifficulty;
                context.AllocatedDifficulty -= rewardDifficulty;
                result[i] = new RewardData(config, amount);
                isRandomDistribution = false;
            }
            return isRandomDistribution;
        }

        private void DistributeRewardsRandomly(ChallengeGenerationContext context, IReadOnlyList<IConfig> rewardConfigs, IList<GameReward> result, float totalAllocatedDifficulty) {
            var distributionVariations = context.BalanceData.Consts.RewardDistributionVariations;
            var variation = ItemChallengeFeatureDependencies.RandomModule.ChooseRandom(distributionVariations);
            for (int i = 0; i < variation.Length; i++) {
                var multiplier = variation[i];
                var difficulty = totalAllocatedDifficulty * multiplier;
                var amount = GetOutputAmountForDifficulty(context, rewardConfigs[i], difficulty);
                result[i] = new RewardData(rewardConfigs[i], amount);
            }
        }

        private void DistributeRemainingDifficulty(ChallengeGenerationContext context, IReadOnlyList<IConfig> rewardConfigs, IList<GameReward> result, float totalAllocatedDifficulty) {
            var emptySlotsCount = result.Count(d => d == null);
            var emptySlotsDifficulty = totalAllocatedDifficulty / emptySlotsCount;
            for (int i = 0; i < result.Count; i++) {
                if (result[i] != null) {
                    continue;
                }
                var amount = GetOutputAmountForDifficulty(context, rewardConfigs[i], emptySlotsDifficulty);
                result[i] = new RewardData(rewardConfigs[i], amount);
            }
        }
        
        private int GetOutputAmountForDifficulty(ChallengeGenerationContext context, IConfig config, float difficultyToFit) {
            var playerLevel = ItemChallengeFeatureDependencies.PlayerDossier.Level;
            var outputsPerDifficulty = context.BalanceData.Outputs[config].OutputsPerDifficultyByPlayerLevel[playerLevel];
            return (int) Math.Ceiling(difficultyToFit * outputsPerDifficulty);
        }
    }
}
