using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable MemberCanBeMadeStatic.Global

namespace Example.Locations.ItemChallenge {
    internal class ChallengeGenerationContextCreator {
        public ChallengeGenerationContext Create() {
            var balanceConfig = GetBalanceConfig();
            var context = new ChallengeGenerationContext(balanceConfig);
            context.AvailableInputs = FindAvailableInputs(context);
            context.DifficultyType = DetermineDifficultyType(context);
            context.ResourcesSurplus = FindSurplusResources(context);
            context.ChallengePurpose = DetermineChallengePurpose(context);
            context.InputSlotsAmount = GetNumberOfInputSlots(context);
            context.InputsExtractionDurationType = GetExtractionDurationForInputs(context);
            context.AllocatedDifficulty = CalculateAllocatedDifficulty(context);
            return context;
        }

        public float CalculateAllocatedDifficulty(ChallengeGenerationContext context) {
            var difficultyMultipliers = new List<float> {
                context.BalanceData.DifficultyTypes[context.DifficultyType].Difficulty,
                context.BalanceData.PurposeDifficultyModifier[context.ChallengePurpose].Multiplier,
                context.BalanceData.SlotsAmountDifficultyModifier[context.InputSlotsAmount].Multiplier,
                context.BalanceData.ExtractionDurationTypeDifficultyModifier[context.InputsExtractionDurationType].Multiplier
            };

            return difficultyMultipliers.Aggregate((float) 1f, (current, multiplier) => current * multiplier);
        }
        
        public ExtractionDurationType GetExtractionDurationForInputs(ChallengeGenerationContext context) {
            var availableExtractionTypes = GetAvailableExtractionDurationTypes(context).ToList();
            if (!availableExtractionTypes.Any()) {
                throw new Exception($"{nameof(ChallengesGenerator)}: Can't find available resources for Challenge generation.\n"
                    + $"Last state: \n{context.GetDebugInfo()}");
            }
            return ItemChallengeFeatureDependencies.RandomModule.ChooseRandom(availableExtractionTypes);
        }
        
        private ItemChallengeBalanceDbData GetBalanceConfig() {
            return ItemChallengeFeatureDependencies.StaticDataModule.Data.ItemChallengeBalance;
        }
        
        private IEnumerable<string> FindAvailableInputs(ChallengeGenerationContext context) {
            var balanceConfig = context.BalanceData;
            return from inputData in balanceConfig.Inputs
                where ItemChallengeFeatureDependencies.GameEventsModule.Check(inputData.Value.AccessParams)
                select inputData.Key;
        }

        private IEnumerable<string> FindSurplusResources(ChallengeGenerationContext context) {
            return (from resource in context.AvailableInputs
                where context.BalanceData.ResourcesSurplus.ContainsKey(resource)
                let surplusData = context.BalanceData.ResourcesSurplus[resource]
                let surplusThreshold = surplusData.ThresholdsByPlayerLevel[ItemChallengeFeatureDependencies.PlayerDossier.Level]
                let resourceAmount = ItemChallengeFeatureDependencies.ItemsRouter.GetResourceAmount(resource)
                where resourceAmount > surplusThreshold
                select resource).ToList();
        }
        
        private ChallengeDifficultyType DetermineDifficultyType(ChallengeGenerationContext context) {
            return ItemChallengeFeatureDependencies.RandomModule.ChooseByWeight(context.BalanceData.DifficultyTypes);
        }
        
        private ChallengePurpose DetermineChallengePurpose(ChallengeGenerationContext context) {
            var availablePurposeModifiers = GetAvailablePurposeModifiers(context).ToList();
            return ItemChallengeFeatureDependencies.RandomModule.ChooseRandom(availablePurposeModifiers);
        }
        
        private static IEnumerable<ChallengePurpose> GetAvailablePurposeModifiers(ChallengeGenerationContext context) {
            var availablePurposeModifiers = new List<ChallengePurpose> { ChallengePurpose.TrivialResources };
            if (context.ResourcesSurplus.IsFilled()) {
                availablePurposeModifiers.Add(ChallengePurpose.ResourcesElimination);
            }
            if (context.DifficultyType is ChallengeDifficultyType.Hard) {
                availablePurposeModifiers.Add(ChallengePurpose.TopReward);
            }
            availablePurposeModifiers.Add(ChallengePurpose.Craft);
            return availablePurposeModifiers;
        }
        
        private int GetNumberOfInputSlots(ChallengeGenerationContext context) {
            var consts = GetBalanceConfig().Consts;
            if (context.DifficultyType is ChallengeDifficultyType.Hard) {
                return consts.MaxInputSlotsAmount;
            }
            return context.ChallengePurpose is ChallengePurpose.ResourcesElimination 
                ? consts.MinInputSlotsAmount 
                : ItemChallengeFeatureDependencies.RandomModule.Range(consts.MinInputSlotsAmount, consts.MaxInputSlotsAmount + 1);
        }

        private IEnumerable<ExtractionDurationType> GetAvailableExtractionDurationTypes(ChallengeGenerationContext context) {
            return context.ChallengePurpose switch {
                ChallengePurpose.TrivialResources => new[] { ExtractionDurationType.Instant },
                ChallengePurpose.ResourcesElimination => new[] { ExtractionDurationType.Instant },
                ChallengePurpose.TopReward => new[] { ExtractionDurationType.Instant, ExtractionDurationType.Short, ExtractionDurationType.Long },
                ChallengePurpose.Craft => new[] { ExtractionDurationType.Short, ExtractionDurationType.Long },
                ChallengePurpose.Undefined => throw new Exception("Undefined Challenge purpose"),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
