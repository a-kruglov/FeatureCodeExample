using System;
using System.Collections.Generic;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    [InjectGenerate]
    public class ItemChallengeFeature : IItemChallengeModule {
        public ISet<Type> DependenciesList => new HashSet<Type> {
            typeof(IItemsRouter)
        };

        private readonly ItemChallengeSlotsController _slotsController;
        private readonly ItemChallengeChestController _chestController;
        private readonly ItemChallengeSignalsHandler _signalsHandler;
        private readonly ChallengeBubbleSignalDispatcher _bubbleDispatcher;
        private readonly ChallengesGenerator _ChallengesGenerator;
        private readonly ItemChallengeEntry _entry;
        private readonly IFeaturesModule _featuresModule;

        public bool IsChestRefreshEnabled => _chestController.IsRefreshEnabled;
        public bool HasNewChallenges => _entry.HasUnseenChallenge;
        public bool HasCompletableChallenges {
            get {
                for (int slotIndex = 0; slotIndex < _slotsController.SlotsCount; slotIndex++) {
                    if (_slotsController.СanFulfillChallenge(slotIndex)) {
                        return true;
                    }
                }
                return false;
            }
        }

        public ItemChallengeFeature(
            IStaticDataModule staticDataModule,
            IStaticDataHelperModule staticDataHelperModule,
            ItemChallengeConfigsProvider configsProvider,
            IItemsRouter itemsRouter,
            IGameSignalBusesModule signalBusesModule,
            IWindowsModule windowsModule,
            IConfigsModule configsModule,
            IDepositoryModule depositoryModule,
            ILogModule logModule,
            IGameEventsModule gameEventsModule,
            IRandomModule randomModule,
            IPlayerDossier playerDossier,
            ITimersModule timersModule,
            ITimeModule timeModule,
            ITrackItemChallenge trackingModule,
            IFeaturesModule featuresModule,
            IUIModule uiModule,
            ItemChallengeEntry entry
        ) {
            ItemChallengeFeatureDependencies.Create(this,
                staticDataModule,
                staticDataHelperModule,
                configsProvider,
                gameEventsModule,
                depositoryModule,
                configsModule,
                randomModule,
                playerDossier,
                timersModule,
                itemsRouter,
                timeModule,
                logModule,
                uiModule,
                signalBusesModule.Default
            );
            _entry = entry;
            _featuresModule = featuresModule;
            _signalsHandler = new ItemChallengeSignalsHandler(signalBusesModule.Default, windowsModule, trackingModule);
            _ChallengesGenerator = new ChallengesGenerator();
            _slotsController = new ItemChallengeSlotsController(_ChallengesGenerator, entry);
            _chestController = new ItemChallengeChestController(entry);
            _bubbleDispatcher = new ChallengeBubbleSignalDispatcher(_entry);
        }
        
        public void Init() => _featuresModule.SubscribeOnActivation(FeatureId.ItemChallenge, OnFeatureActivated);

        private void OnFeatureActivated() {
            _signalsHandler.Subscribe();
            _bubbleDispatcher.Subscribe();
            
            _slotsController.Init();
            _chestController.Init();
        }
        
        public IItemChallengeSlot GetSlotData(int index) => _slotsController.GetChallengeSlot(index);
        public IItemChallengeChestData GetCurrentChestData() => _chestController.CurrentChestData;

        public bool IsSlotActive(int slotIndex) => !_slotsController.IsSlotOnCooldown(slotIndex);

        public bool TryFulfillSlot(int slotIndex) => _slotsController.TryFulfillSlot(slotIndex, false);
        public bool TryForceFulfillSlot(int slotIndex) => _slotsController.TryFulfillSlot(slotIndex, true);
        public bool TryRefuseSlot(int slotIndex) => _slotsController.TryRefuseSlot(slotIndex);
        public bool TrySpeedupSlot(int index) => _slotsController.TrySpeedupSlot(index);
        
        public long GetSlotSpeedUpFreeTime() => ItemChallengeFeatureDependencies.ConfigsProvider.ItemChallengeData.SlotCooldownSpeedUpFreeTime;
        public long GetSlotCooldownTimestamp(int slotIndex) => _entry.GetSlotCooldownTimestamp(slotIndex);
        public int GetSlotCooldownDuration(int slotIndex) => ItemChallengeFeatureDependencies.ConfigsProvider.ItemChallengeData.DelayAfterRejection;

        public void ForceChestRefresh() => _chestController.ForceChestRefresh();
        public bool TryOpenChest(int stage) => _chestController.TryOpenChest(stage);
        public bool TryTakeChestRewardsFromDeposit() => _chestController.TryTakeRewardsFromDeposit();
        
        public GameItem GetSlotSpeedupCost(int index) {
            var timeLeft = GetSlotCooldownTimestamp(index) - ItemChallengeFeatureDependencies.TimeModule.UtcNowTimeStamp;
            var isFree = GetSlotSpeedUpFreeTime() - timeLeft >= 0;
            if (isFree) {
                return null;
            }
            
            var speedupCost = ItemChallengeFeatureDependencies.ConfigsProvider.ItemChallengeData.SlotCooldownSpeedUpCost;
            return new GameItem(speedupCost.Id, speedupCost.Amount);
        }

        public void Terminate() {
            _slotsController.Clear();
            _chestController.Clear();
            _signalsHandler.Unsubscribe();
            _bubbleDispatcher.Unsubscribe();
        }
        
#if UNITY_EDITOR
        public ChallengeGenerationResult GenerateNewChallenge_Debug() => _ChallengesGenerator.CreateNewChallenge();
#endif
    }
}
