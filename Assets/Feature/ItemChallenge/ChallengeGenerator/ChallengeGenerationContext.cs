using System.Collections.Generic;

namespace Example.Locations.ItemChallenge {
    internal class ChallengeGenerationContext {
        
        public readonly ItemChallengeBalanceDbData BalanceData;
        
        public int InputSlotsAmount { get; set; }
        public float AllocatedDifficulty { get; set; }

        public ChallengePurpose ChallengePurpose { get; set; }
        public ChallengeDifficultyType DifficultyType { get; set; }
        public ExtractionDurationType InputsExtractionDurationType { get; set; }
        
        public IEnumerable<string> AvailableInputs { get; set; }
        public IEnumerable<string> ResourcesSurplus { get; set; }
        public HashSet<ExtractionDurationType> UnsuccessfulGenerationTypes { get; } = new();

        public ChallengeGenerationContext(ItemChallengeBalanceDbData balanceData) {
            BalanceData = balanceData;
        }
        
        public string GetDebugInfo() {
            return "New Challenge generated: \n"
                + $"{DifficultyType} \n"
                + $"ChallengePurpose: {ChallengePurpose} \n"
                + $"InputsExtractionDurationType: {InputsExtractionDurationType} \n"
                + $"InputSlotsAmount: {InputSlotsAmount} \n"
                + $"AllocatedDifficulty: {AllocatedDifficulty} \n"
                + $"\n"
                + $"AvailableInputs: \n---{AvailableInputs.JoinToString("\n---")} \n"
                + $"ResourcesSurplus: \n---{ResourcesSurplus.JoinToString("\n---")} \n"
                + $"\n"
                + $"UnsuccessfulGenerationTypes: \n---{UnsuccessfulGenerationTypes.JoinToString("\n---")} \n";
        }
    }
}
