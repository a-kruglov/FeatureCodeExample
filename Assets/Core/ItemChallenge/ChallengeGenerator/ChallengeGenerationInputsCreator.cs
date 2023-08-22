using System;
using System.Collections.Generic;
using System.Linq;

namespace Example.Locations.ItemChallenge {
    internal class ChallengeGenerationInputsCreator {
        
        private readonly ChallengeGenerationContextCreator _contextCreator;
        private readonly Dictionary<ExtractionDurationType, Func<ChallengeGenerationContext, string[], string>> extractionTypeToFunctionMap;
        
        public ChallengeGenerationInputsCreator(ChallengeGenerationContextCreator contextCreator) {
            _contextCreator = contextCreator;
            extractionTypeToFunctionMap = new Dictionary<ExtractionDurationType, Func<ChallengeGenerationContext, string[], string>> {
                { ExtractionDurationType.Instant, GenerateInstantInputConfig },
                { ExtractionDurationType.Short, GenerateInputConfigForType },
                { ExtractionDurationType.Long, GenerateInputConfigForType },
            };
        }

        public IEnumerable<GameItem> Create(ChallengeGenerationContext context) {
            return GenerateInputs(context);
        }

        private IEnumerable<GameItem> GenerateInputs(ChallengeGenerationContext context) {
            GameItem[] ChallengeInputs;
            var totalDifficulty = context.AllocatedDifficulty;

            do {
                ChallengeInputs = new GameItem[context.InputSlotsAmount];

                // Distributing the difficulty evenly among the available slots
                context.AllocatedDifficulty /= context.InputSlotsAmount;

                var generatedConfigs = new string[context.InputSlotsAmount];
                for (int slotIndex = 0; slotIndex < context.InputSlotsAmount; slotIndex++) {
                    if (TryToGenerateInputConfig(context, generatedConfigs, out var inputIConfig)) {
                        var inputDifficulty = context.BalanceData.Inputs[inputIConfig].ExtractionDifficulty;
                        var inputsAmount = CalculateInputsAmount(inputDifficulty, context.AllocatedDifficulty);

                        // Updating the allocated difficulty value subtracting the difficulty of the generated inputs from the total
                        context.AllocatedDifficulty = totalDifficulty - inputDifficulty * inputsAmount;
                        
                        generatedConfigs[slotIndex] = inputIConfig;
                        ChallengeInputs[slotIndex] = new GameItem(inputIConfig, inputsAmount);
                        continue;
                    }

                    // If the generation was unsuccessful, update the context for the next iteration
                    UpdateContextForNextIteration(context);
                    totalDifficulty = context.AllocatedDifficulty;
                    ChallengeInputs = null;
                    break;
                }

                // Repeating until successful configuration has been generated for all slots
            } while (ChallengeInputs == null);

            context.AllocatedDifficulty = totalDifficulty;
            return ChallengeInputs;
        }

        private void UpdateContextForNextIteration(ChallengeGenerationContext context) {
            context.UnsuccessfulGenerationTypes.Add(context.InputsExtractionDurationType);
            context.InputsExtractionDurationType = _contextCreator.GetExtractionDurationForInputs(context);
            context.AllocatedDifficulty = _contextCreator.CalculateAllocatedDifficulty(context);
        }
        
        private static int CalculateInputsAmount(float inputDifficulty, float allocatedDifficulty) {
            return (int)Math.Ceiling(allocatedDifficulty / inputDifficulty);
        }

        private bool TryToGenerateInputConfig(ChallengeGenerationContext context, string[] generatedConfigs, out string IConfig) {
            IConfig = GenerateInputConfig(context, generatedConfigs);
            return !string.IsNullOrEmpty(IConfig);
        }
        
        private string GenerateInputConfig(ChallengeGenerationContext context, string[] excludeConfigs) {
            if (!extractionTypeToFunctionMap.TryGetValue(context.InputsExtractionDurationType, out var generationFunc)) {
                throw new Exception($"{nameof(ChallengeGenerationInputsCreator)}: Unknown or undefined extraction duration type");
            }
            var id = generationFunc(context, excludeConfigs);
            return id;
        }
        
        private static string GenerateInstantInputConfig(ChallengeGenerationContext context, string[] excludeConfigs) {
            var resources = context.ChallengePurpose is ChallengePurpose.ResourcesElimination 
                ? context.ResourcesSurplus.Except(excludeConfigs).ToArray()
                : context.AvailableInputs.ToArray();
        
            var availableInputs = resources.IsFilled() ? resources : context.AvailableInputs;
            return SelectRandomConfig(context, availableInputs.ToList(), ExtractionDurationType.Instant, excludeConfigs);
        }
        
        private static string GenerateInputConfigForType(ChallengeGenerationContext context, string[] excludeConfigs) {
            return SelectRandomConfig(context, context.AvailableInputs, context.InputsExtractionDurationType, excludeConfigs);
        }

        private static string SelectRandomConfig(ChallengeGenerationContext context, IEnumerable<string> inputs, ExtractionDurationType type, string[] excludeConfigs) {
            var availableInputs = FilterInputs(context, inputs, type, excludeConfigs);
            if (availableInputs.Count == 0) {
                return null;
            }
            var randomData = ItemChallengeFeatureDependencies.RandomModule.ChooseRandom(availableInputs);
            return context.BalanceData.Inputs.First(i => i.Value == randomData).Key;
        }

        private static List<ChallengeInputsDbData> FilterInputs(ChallengeGenerationContext context, IEnumerable<string> inputs, ExtractionDurationType type, string[] excludeConfigs) {
            return inputs
                .Where(i => !excludeConfigs.Contains(i))
                .Select(r => context.BalanceData.Inputs[r])
                .Where(i => i.ExtractionDurationType == type)
                .Where(i => i.ExtractionDifficulty <= context.AllocatedDifficulty * context.BalanceData.Consts.SlotsDifficultyFitFactor)
                .ToList();
        }
    }
}
