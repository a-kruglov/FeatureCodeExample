using Example.Api;

namespace Example.Locations.ItemChallenge {
    public readonly struct ItemChallengeChestData : IItemChallengeChestData {
        public ItemChallengeChestDbData Data { get; }
        public int CurrentProgress { get; }
        public int MaxProgress { get; }
        public int CurrentStage { get; }
        public long RefreshTimestamp { get; }
        public ChallengeChestStageStatus[] StageStatuses { get; }

        public int GetRequiredNumberOfChallengesForStage(int stage) => Data.Stages.Take(stage + 1).Sum(s => s.RequiredNumberOfChallenges);

        public ItemChallengeChestData(
            ItemChallengeChestDbData data,
            long refreshTimestamp,
            int currentProgress,
            int currentStage,
            ChallengeChestStageStatus[] stageStatuses
        ) {
            Data = data;
            RefreshTimestamp = refreshTimestamp;
            CurrentProgress = currentProgress;
            CurrentStage = currentStage;
            StageStatuses = stageStatuses;
            MaxProgress = Data.Stages.Sum(s => s.RequiredNumberOfChallenges);
        }
    }
}
