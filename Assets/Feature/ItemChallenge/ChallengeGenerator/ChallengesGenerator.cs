using System.Collections.Generic;
using System.Linq;


namespace Example.Locations.ItemChallenge {
    
    internal class ChallengesGenerator {

        private readonly ChallengeGenerationContextCreator _contextCreator;
        private readonly ChallengeGenerationInputsCreator _generationInputCreator;
        private readonly ChallengeGenerationOutputsCreator _generationOutputsCreator;

        public ChallengesGenerator() {
            _contextCreator = new ChallengeGenerationContextCreator();
            _generationInputCreator = new ChallengeGenerationInputsCreator(_contextCreator);
            _generationOutputsCreator = new ChallengeGenerationOutputsCreator();
        }

        public ChallengeGenerationResult CreateNewChallenge() {
            var context = _contextCreator.Create();
            var ChallengeInputs = _generationInputCreator.Create(context).ToList();
            var ChallengeOutputs = _generationOutputsCreator.Create(context).ToList();
            
            ValidateGenerationResult(ChallengeInputs, ChallengeOutputs, context);

            var debugInfo = context.GetDebugInfo() + "\n"
                + $"ChallengeInputs: \n---{ChallengeInputs.JoinToString("\n---")} \n"
                + $"ChallengeOutputs: \n---{ChallengeOutputs.JoinToString("\n---")} \n";
            
            ItemChallengeFeatureDependencies.LogModule.Info(debugInfo);
            
            var Challenge = new ItemChallengeSlot(context.DifficultyType, ChallengeInputs, ChallengeOutputs);
            return new ChallengeGenerationResult(Challenge, debugInfo);
        }

        private void ValidateGenerationResult(IEnumerable<GameItem> inputs, IEnumerable<GameReward> outputs, ChallengeGenerationContext context) {
            var maxExtractionDifficulty = float.zero;
            var resultInputsTotalDifficulty = float.zero;
            foreach (var input in inputs) {
                var extractionDifficulty = context.BalanceData.Inputs[input.Item].ExtractionDifficulty;
                if (extractionDifficulty > maxExtractionDifficulty) {
                    maxExtractionDifficulty = extractionDifficulty;
                }
                resultInputsTotalDifficulty += extractionDifficulty * input.Value;
            }
            if (!IsApproximatelyEqual(context.AllocatedDifficulty, resultInputsTotalDifficulty, maxExtractionDifficulty)) {
                ItemChallengeFeatureDependencies.LogModule.Error($"Mismatch between allocated difficulty and result inputs difficulty. Allocated: {context.AllocatedDifficulty}, result: {resultInputsTotalDifficulty}");
                return;
            }

            var maxOutputDifficulty = 0f;
            var resultOutputsTotalDifficulty = 0f;
            foreach (var output in outputs) {
                var outputDifficulty = GetOutputDifficulty(output, context);
                if (outputDifficulty > maxOutputDifficulty) {
                    maxOutputDifficulty = outputDifficulty;
                }
                resultOutputsTotalDifficulty += outputDifficulty;
            }
            if (IsApproximatelyEqual(context.AllocatedDifficulty, resultOutputsTotalDifficulty, maxOutputDifficulty)) {
                return;
            }
            ItemChallengeFeatureDependencies.LogModule.Error($"Mismatch between allocated difficulty and result outputs difficulty. Allocated: {context.AllocatedDifficulty}, result: {resultOutputsTotalDifficulty}");
        }

        private float GetOutputDifficulty(GameItem output, ChallengeGenerationContext context) {
            var outputIConfig = new IConfig(output.Item);
            var level = ItemChallengeFeatureDependencies.PlayerDossier.Level;
            var outputDifficulty = context.BalanceData.Outputs[outputIConfig].OutputsPerDifficultyByPlayerLevel[level];
            return 1 / outputDifficulty * output.Value;
        }
        
        private static bool IsApproximatelyEqual(float a, float b, float epsilon) {
            var dif = a - b;
            if (dif == 0) {
                return true;
            }
            if (dif > 0) {
                return dif < epsilon;
            }
            return -dif < epsilon;
        }
    }
}
