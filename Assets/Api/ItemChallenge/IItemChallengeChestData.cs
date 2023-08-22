namespace Example.Api {
    public interface IItemChallengeChestData {
        ItemChallengeChestDbData Data { get; }
        int CurrentProgress { get; }
        int MaxProgress { get; }
        int CurrentStage { get; }
        long RefreshTimestamp { get; }
        ChallengeChestStageStatus[] StageStatuses { get; }

        int GetRequiredNumberOfChallengesForStage(int stage);
    }
}
