using System;
using System.Collections.Generic;
using System.Linq;
using Example.Api;
using Random = UnityEngine.Random;

namespace Example.Locations.ItemChallenge {
    public class ItemChallengeChestController {
        private readonly TrackData _trackData = new(TrackDataSources.Challenge_BOARD){SubSource = "Сhest"};
        private const string _depositId = "Challenge_board_chest_{0}";

        private static IStaticDataHelperModule StaticDataHelperModule => ItemChallengeFeatureDependencies.StaticDataHelperModule;
        private static ItemChallengeConfigsProvider ConfigsProvider => ItemChallengeFeatureDependencies.ConfigsProvider;
        private static IGameEventsModule GameEventsModule => ItemChallengeFeatureDependencies.GameEventsModule;
        private static IDepositoryModule DepositoryModule => ItemChallengeFeatureDependencies.DepositoryModule;
        private static ITimersModule TimersModule => ItemChallengeFeatureDependencies.TimersModule;
        private static IItemsRouter ItemsRouter => ItemChallengeFeatureDependencies.ItemsRouter;
        private static ITimeModule TimeModule => ItemChallengeFeatureDependencies.TimeModule;
        private static ILogModule LogModule => ItemChallengeFeatureDependencies.LogModule;
        private static IGameSignalBus SignalBus => ItemChallengeFeatureDependencies.SignalBus;

        private readonly ItemChallengeEntry _entry;
        private readonly ITimer _timer;

        private bool IsChestMaxed => CurrentChestProgress >= CurrentChestConfig.Stages.Sum(s => s.RequiredNumberOfChallenges);
        private ItemChallengeChestDbData CurrentChestConfig => ConfigsProvider.GetChestData(CurrentChestIConfig);
        private long CurrentChestRefreshTimestamp => _entry.GetChestRefreshTimestamp();
        private string CurrentChestIConfig => _entry.GetChestIConfig();
        private int CurrentChestProgress => _entry.GetChestProgress();

        public IItemChallengeChestData CurrentChestData {
            get {
                var config = CurrentChestConfig;
                var refreshTimestamp = CurrentChestRefreshTimestamp;
                var chestStage = CurrentChestStage;
                var currentProgress = CurrentChestProgress;
                var stageRewardStatus = StageStatuses;
                return new ItemChallengeChestData(config, refreshTimestamp, currentProgress, chestStage, stageRewardStatus);
            }
        }

        public bool IsRefreshEnabled {
            get => _entry.GetRefreshEnabled();
            private set {
                _entry.SetRefreshEnabled(value);
                _entry.SetChanged();
            }
        }

        private ChallengeChestStageStatus[] StageStatuses {
            get {
                ChallengeChestStageStatus[] result = new ChallengeChestStageStatus[CurrentChestConfig.Stages.Count()];
                for (int i = 0; i < result.Length; i++) {
                    if (i < CurrentChestStage || IsChestMaxed) {
                        var depositId = GetDepositId(i);
                        if (_entry.HasDepositForChest(depositId)) {
                            result[i] = ChallengeChestStageStatus.ReadyToOpen;
                        } else {
                            result[i] = ChallengeChestStageStatus.Opened;
                        }
                    } else {
                        result[i] = ChallengeChestStageStatus.Locked;
                    }
                }
                return result;
            }
        }

        private int CurrentChestStage {
            get {
                if (CurrentChestConfig == null) {
                    return 0;
                }
                var progress = CurrentChestProgress;
                var stages = CurrentChestConfig.Stages.ToArray();
                for (int i = 0; i < stages.Length; i++) {
                    var stage = stages[i];
                    if (progress < stage.RequiredNumberOfChallenges) {
                        return i;
                    }
                    progress -= stage.RequiredNumberOfChallenges;
                }
                return stages.Length - 1;
            }
        }

        public ItemChallengeChestController(ItemChallengeEntry entry) {
            _entry = entry;
            _timer = TimersModule.CreateTimer(OnRefreshCooldown);
        }

        public void Init() {
            _entry.ValidateChest();
            LogModule.Info("Preparing ItemChallengeChestController");
            if (CurrentChestConfig == null) {
                SetupNewChest();
            }
            SubscribeOnChanges();
            UpdateTimer();
        }

        public bool TryOpenChest(int stage) {
            LogModule.Info($"Attempting to open chest for stage {stage}");
            var rewardData = TakeRewardsFromDeposit(stage)?.ToArray();
            if (rewardData.IsEmpty()) {
                LogModule.Info($"No rewards for stage {stage}");
                return false;
            }
            foreach (var reward in rewardData) {
                ItemsRouter.AddResource(new IConfig(reward.Item), reward.Value, _trackData);
            }
            var rewards = StaticDataHelperModule.RewardToStaticData(rewardData);
            SignalBus.Fire(new ItemChallengeChestOpenedSignal(rewards));
            
            // The first chests for the user have no refresh so the user can open all the stages and understand how they work.
            // After he has opened a reward from the last stage, we enable refresh and update the chests every day.
            if (!IsRefreshEnabled) {
                TryEnableRefresh();
            }

            return true;
        }
        
        public bool TryTakeRewardsFromDeposit() {
            if (!IsRefreshEnabled) {
                return false;
            }
            var autoCollectTimestamp = _entry.GetDepositsAutoCollectTimestamp();
            if (autoCollectTimestamp > 0 && autoCollectTimestamp > TimeModule.UtcNowTimeStamp) {
                return false;
            }
            var allDeposits = _entry.GetChestDeposits();
            var allDepositsCopy = new List<string>(allDeposits);
            var rewardsData = allDepositsCopy.Select(TakeRewardsFromDeposit)
                .SelectMany(rewards => rewards)
                .ToArray();

            var chestRefreshTimestamp = _entry.GetChestRefreshTimestamp();
            _entry.SetDepositsAutoCollectTimestamp(chestRefreshTimestamp);
            
            if (rewardsData.IsEmpty()) {
                return false;
            }
            foreach (var reward in rewardsData) {
                ItemsRouter.AddResource(new IConfig(reward.Item), reward.Value, _trackData);
            }

            var rewards = StaticDataHelperModule.RewardToStaticData(rewardsData);
            SignalBus.Fire(new ItemChallengeChestOpenedSignal(rewards));
            
            return true;
        }

        public void ForceChestRefresh() {
            LogModule.Info("Forcing chest refresh");
            RenewCurrentChest();
        }

        public void Clear() {
            LogModule.Info("Clearing ItemChallengeChestController");
            _timer.Terminate();
            UnsubscribeFromChanges();
        }
        
        private void TryEnableRefresh() {
            var allChestOpened = StageStatuses.All(s => s == ChallengeChestStageStatus.Opened);
            if (!allChestOpened) {
                return;
            }
            IsRefreshEnabled = true;
            OnRefreshCooldown();
        }

        private IEnumerable<GameReward> TakeRewardsFromDeposit(int stage) {
            var depositId = GetDepositId(stage);
            return TakeRewardsFromDeposit(depositId);
        }

        private IEnumerable<GameReward> TakeRewardsFromDeposit(string depositId) {
            if (!DepositoryModule.HasDeposit(depositId)) {
                return null;
            }
            var deposit = DepositoryModule.TakeDeposit(depositId);
            _entry.RemoveDepositForChest(depositId);
            var rewards = deposit.GetRewards();
            return new List<GameReward>(rewards.Select(r => new RewardData(r.Key, r.Value)));
        }

        private void MarkChestAsComplete(int stage) {
            var rewards = CurrentChestConfig.Stages.ElementAt(stage).Rewards;
            var rewardsDict = rewards.ToDictionary(r => r.Item.Id, r => StaticDataHelperModule.GetAmount(r.Item));
            var depositId = GetDepositId(stage);
            DepositoryModule.PlaceDeposit(depositId, rewardsDict);
            _entry.AddDepositForChest(depositId);
            var signal = new ItemChallengeStageCompletedSignal(stage);
            SignalBus.Fire(signal);
        }

        private void UpdateTimer() {
            _timer.Terminate();
            _timer.SetFireTimeStamp(CurrentChestRefreshTimestamp);
        }

        private void OnRefreshCooldown() {
            if (!IsRefreshEnabled) {
                return;
            }
            RenewCurrentChest();
        }

        private void RenewCurrentChest() {
            SetupNewChest();
            UpdateTimer();
            SignalBus.Fire<ItemChallengeChestRenewSignal>();
        }

        private void SetupNewChest() {
            var config = GetAvailableRandomChestConfig();
            if (config == null) {
                return;
            }
            // Calculate the timestamp for the next 12 midnight UTC.
            // This is a temporary local solution.
            // We need to have a proper way to get the next "game refresh" timestamp, from time Module or somehow else.
            var refreshTimestamp = GetNextMidnightUtcTimeStamp();
            _entry.SetCurrentChest(0, refreshTimestamp, config.Id);
        }

        private static long GetNextMidnightUtcTimeStamp() {
            DateTime nowUtc = TimeModule.UtcNow;
            DateTime nextMidnightUtc = nowUtc.Date.AddDays(1);
            return nextMidnightUtc.ToUnixUtcTimeStamp();
        }

        private ItemChallengeChestDbData GetAvailableRandomChestConfig() {
            var allConfigs = ConfigsProvider.GetAllChestsData();
            var availableChallenges = allConfigs
                .Where(c => GameEventsModule.Check(c.AccessParams))
                .ToList();

            if (availableChallenges.IsEmpty()) {
                return null;
            }
            var random = Random.Range(0, availableChallenges.Count);
            return availableChallenges[random];
        }

        private void SubscribeOnChanges() {
            SignalBus.Subscribe<ItemChallengeChallengeCompletedSignal>(OnChallengeComplete);
        }

        private void UnsubscribeFromChanges() {
            SignalBus.Unsubscribe<ItemChallengeChallengeCompletedSignal>(OnChallengeComplete);
        }

        private static string GetDepositId(int stage) => string.Format(_depositId, stage);

        private void OnChallengeComplete(ItemChallengeChallengeCompletedSignal signal) {
            if (IsChestMaxed) {
                return;
            }

            var prevStage = CurrentChestStage;
            _entry.AddChestProgress();
            var isStageChanged = CurrentChestStage > prevStage;
            if (isStageChanged || IsChestMaxed) {
                MarkChestAsComplete(prevStage);
            }

            var progressSignal = new ItemChallengeChestProgressSignal(1, CurrentChestProgress);
            SignalBus.Fire(progressSignal);
        }
    }
}
