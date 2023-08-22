using System.Collections.Generic;
using System.Linq;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    internal class ItemChallengeSlotsController {
        private readonly ChallengesGenerator _ChallengesGenerator;
        private readonly ItemChallengeEntry _entry;
        private readonly ITimer _timer;
        
        private static ItemChallengeConfigsProvider ConfigsProvider => ItemChallengeFeatureDependencies.ConfigsProvider;
        private static IStaticDataModule StaticDataModule => ItemChallengeFeatureDependencies.StaticDataModule;
        private static IStaticDataHelperModule StaticDataHelperModule => ItemChallengeFeatureDependencies.StaticDataHelperModule;
        private static IGameEventsModule GameEventsModule => ItemChallengeFeatureDependencies.GameEventsModule;
        private static IItemChallengeModule ItemChallengeModule => ItemChallengeFeatureDependencies.ItemChallengeModule;
        private static ITimersModule TimersModule => ItemChallengeFeatureDependencies.TimersModule;
        private static IItemsRouter ItemsRouter => ItemChallengeFeatureDependencies.ItemsRouter;
        private static ITimeModule TimeModule => ItemChallengeFeatureDependencies.TimeModule;
        private static ILogModule LogModule => ItemChallengeFeatureDependencies.LogModule;
        private static IGameSignalBus SignalBus => ItemChallengeFeatureDependencies.SignalBus;

        public int SlotsCount => StaticDataModule.Data.ItemChallenge.ChallengeSlotsCount;
        private static long TimeNow => TimeModule.UtcNowTimeStamp;

        private ItemChallengeSlot[] _slots;
        private ItemChallengeSlot[] Slots => _slots ??= new ItemChallengeSlot[SlotsCount];

        public ItemChallengeSlotsController(ChallengesGenerator ChallengesGenerator, ItemChallengeEntry entry) {
            _entry = entry;
            _ChallengesGenerator = ChallengesGenerator;
            _timer = TimersModule.CreateTimer(OnSlotCooldown);
        }
        
        public IItemChallengeSlot GetChallengeSlot(int index) => Slots[index];
        public bool IsSlotOnCooldown(int index) => _entry.GetSlotCooldownTimestamp(index) > 0 && _entry.GetSlotCooldownTimestamp(index) > TimeNow;
        public bool СanFulfillChallenge(int index) {
            if (IsSlotOnCooldown(index)) {
                return false;
            }
            return Slots[index] != null && ItemsRouter.IsEnough(Slots[index].Inputs);
        }

        public void Init() {
            _entry.ValidateCompletedChallenges();
            for (int slotIndex = 0; slotIndex < SlotsCount; slotIndex++) {
                _entry.ValidateSlot(slotIndex);

                var isOnCooldown = IsSlotOnCooldown(slotIndex);
                if (isOnCooldown) {
                    continue;
                }
                var isLoadedWithConfig = TryLoadSlotFromConfig(slotIndex, out var slot);
                if (isLoadedWithConfig) {
                    Slots[slotIndex] = slot;
                    continue;
                }
                var isLoadedAsGenerated = TryLoadGeneratedChallenge(slotIndex, out slot);
                if (isLoadedAsGenerated) {
                    Slots[slotIndex] = slot;
                    continue;
                }
                var isCreatedAfterCooldown = TryCreateChallengeAfterCooldown(slotIndex, out slot);
                if (isCreatedAfterCooldown) {
                    SetSlotAfterCooldownAndNotify(slotIndex, slot);
                }
            }
            TrySetTimer();
        }
        
        public bool TryFulfillSlot(int slotIndex, bool force) {
            var slot = Slots[slotIndex];
            var requiredItems = force ? slot.GetForceFulfillPrice() : slot.Inputs;
            var canBeFulfilled = ItemsRouter.IsEnough(requiredItems);
            if (!canBeFulfilled) {
                return false;
            }
            FulfillSlot(slot, force);
            Slots[slotIndex] = CreateNewChallenge(slot.Index);
            SignalBus.Fire(new ItemChallengeChallengeCompletedSignal(slot.Index, slot.DifficultyType));
            return true;
        }
        
        public bool TryRefuseSlot(int slotIndex) {
            if (IsSlotOnCooldown(slotIndex)) {
                return false;
            }
            var delay = ConfigsProvider.ItemChallengeData.DelayAfterRejection;
            var cooldownTimestamp = TimeModule.GetUtcNowTimeStampWithOffset(delay);

            SaveSlotAsCooldown(slotIndex, cooldownTimestamp);
            TrySetTimer();
            
            var slot = GetChallengeSlot(slotIndex);
            SignalBus.Fire(new ItemChallengeChallengeRefusedSignal(slot.DifficultyType));
            return true;
        }
        
        public bool TrySpeedupSlot(int index) {
            var cost = ItemChallengeModule.GetSlotSpeedupCost(index);
            if (cost != null && !ItemsRouter.IsEnough(new[] { cost })) {
                return false;
            }
            var removeResult = cost == null || ItemsRouter.RemoveResource(cost.Item, cost.Value, CreateTrackData(ChallengeDifficultyType.Undefined));
            if (removeResult) {
                SpeedupSlotCooldown(index);
                return true;
            }
            LogModule.Error($"ItemChallengeSlotsController.TrySpeedupSlot: failed to remove resource, index: {index}");
            return false;
        }

        private bool TryLoadSlotFromConfig(int slotIndex, out ItemChallengeSlot slot) {
            slot = null;
            var savedConfig = _entry.GetSlotIConfig(slotIndex);
            if (string.IsNullOrEmpty(savedConfig)) {
                return false;
            }
            var config = ConfigsProvider.GetChallengeConfig(savedConfig);
            if (config == null) {
                LogModule.Warning($"ItemChallengeSlotsController.TryLoadSlotFromConfig: config is in save, but not found in configs: {savedConfig}");
                return false;
            }

            slot = CreateNewChallengeWithConfig(slotIndex, config);
            return true;
        }
        
        private bool TryLoadGeneratedChallenge(int slotIndex, out ItemChallengeSlot slot) {
            slot = null;
            var inputs = _entry.GetSlotInputs(slotIndex);
            if (inputs == null) {
                return false;
            }
            var outputs = _entry.GetSlotOutputs(slotIndex);
            if (outputs == null) {
                return false;
            }
            var difficultyType = _entry.GetSlotDifficulty(slotIndex);
            if (difficultyType is ChallengeDifficultyType.Undefined) {
                return false;
            }
            var inputsCasted = inputs.Select(x => new GameItem(x.Item, x.Value)).ToArray();
            var outputsCasted = outputs.Select(x => new RewardData(x.Item, x.Value)).ToArray();
            slot = new ItemChallengeSlot(difficultyType, inputsCasted, outputsCasted) {
                Index = slotIndex
            };
            return true;
        }

        private bool TryCreateChallengeAfterCooldown(int slotIndex, out ItemChallengeSlot slot) {
            slot = null;
            if (IsSlotOnCooldown(slotIndex)) {
                return false;
            }
            slot = CreateNewChallenge(slotIndex);
            return true;
        }
        
        private void SpeedupSlotCooldown(int index) {
            _entry.SetSlotData(index, cooldownTimestamp : 1);
            OnSlotCooldown();
        }

        private void SetSlotAfterCooldownAndNotify(int slotIndex, ItemChallengeSlot slot) {
            Slots[slotIndex] = slot;
            SignalBus.Fire(new ItemChallengeSlotCooldownCompletedSignal(slotIndex));
        }

        private void UpdateSlots() {
            for (int i = 0; i < SlotsCount; i++) {
                var cooldown = _entry.GetSlotCooldownTimestamp(i);
                var isSlotActive = cooldown <= 0;
                if (isSlotActive) {
                    continue;
                }
                var result = TryCreateChallengeAfterCooldown(i, out var slot);
                if (!result) {
                    continue;
                }
                SetSlotAfterCooldownAndNotify(i, slot);
            }
        }
        
        private void OnSlotCooldown() {
            UpdateSlots();
            TrySetTimer();
        }

        private void TrySetTimer() {
            var nearestCooldown = _entry.GetNearestCooldownTimestamp();
            if (nearestCooldown <= 0) {
                return;
            }
            _timer.Terminate();
            _timer.SetFireTimeStamp(nearestCooldown);
        }

        private void FulfillSlot(IItemChallengeSlot slot, bool force) {
            var trackData = CreateTrackData(slot.DifficultyType);
            var price = force
                ? slot.GetForceFulfillPrice().ToList()
                : slot.Inputs.ToList();
            ItemsRouter.RemoveResources(price, trackData);
            
            foreach (var reward in slot.Outputs) {
                ItemsRouter.AddResource(reward.Item, reward.Value, trackData);
            }
            
            SaveSlotAsCompleted(slot.Index);
        }

        private ItemChallengeSlot CreateNewChallenge(int index) {
            var availableChallenge = GetFirstAvailableChallengeConfig();
            ItemChallengeSlot Challenge;
            if (availableChallenge != null) {
                Challenge = CreateNewChallengeWithConfig(index, availableChallenge);
                SaveSlot(index, availableChallenge.Id);
            } else {
                Challenge = GenerateNewChallenge(index);
                SaveSlot(Challenge);
            }
            return Challenge;
        }

        private static ItemChallengeSlot CreateNewChallengeWithConfig(int index, ChallengeDbData data) {
            var inputs = StaticDataHelperModule.StaticDataItems(data.ChallengeItems);
            var outputs = StaticDataHelperModule.StaticDataToRewards(data.Rewards);
            return new ItemChallengeSlot(data.DifficultyType, inputs, outputs) {
                Index = index
            };
        }
        
        private ItemChallengeSlot GenerateNewChallenge(int index) {
            var generationResult = _ChallengesGenerator.CreateNewChallenge();
            var Challenge = generationResult.Slot;
            Challenge.Index = index;
            return Challenge;
        }

        private ChallengeDbData GetFirstAvailableChallengeConfig() {
            var allConfigs = ConfigsProvider.GetAllChallengeConfigs();
            var currentChallenges = _entry.GetCurrentChallengeIds();
            var completedChallengesIds = _entry.GetCompletedChallengeIds();
            return allConfigs.FirstOrDefault(data => !currentChallenges.Contains(data.Id)
                && !completedChallengesIds.Contains(data.Id)
                && GameEventsModule.Check(data.AccessParams));
        }
        
        private void SaveSlotAsCooldown(int index, long cooldown) {
            _entry.SetSlotData(index, cooldownTimestamp : cooldown);
            SaveSlotAsCompleted(index);
        }

        private void SaveSlotAsCompleted(int index) {
            var IConfig = _entry.GetSlotIConfig(index);
            if (IConfig != null) {
                _entry.AddCompletedChallenge(IConfig, TimeNow);
            }
            _entry.SetChanged();
        }

        private void SaveSlot(IItemChallengeSlot slot) {
            _entry.SetSlotData(
                slotIndex : slot.Index,
                difficulty : slot.DifficultyType,
                inputs : GetItemsForSave(slot.Inputs),
                outputs : GetItemsForSave(slot.Outputs)
            );
        }

        private void SaveSlot(int index, string IConfig) {
            _entry.SetSlotData(index, IConfig);
        }

        private static List<GameItemForSave> GetItemsForSave(IEnumerable<GameItem> items) {
            return items.Select(x => new GameItemForSave(x.Item, x.Value)).ToList();
        }

        public void Clear() {
            _timer.Terminate();
            for (int i = 0; i < SlotsCount; i++) {
                Slots[i] = null;
            }
        }

        private TrackData CreateTrackData(ChallengeDifficultyType difficultyType) {
            return new TrackData(TrackDataSources.Challenge_BOARD) {
                SubSource = difficultyType.ToString()
            };
        }
    }
}
