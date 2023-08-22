using Example.Api;
using UnityEngine;

namespace Example.Locations.ItemChallenge {
    internal sealed class ChallengeBubbleSignalDispatcher {

        private readonly ItemChallengeEntry _entry;
        
        private static IGameSignalBus _signalBus => ItemChallengeFeatureDependencies.SignalBus;
        private static IUIModule _uiModule => ItemChallengeFeatureDependencies.UIModule;
        private static IItemChallengeModule _ItemChallengeModule => ItemChallengeFeatureDependencies.ItemChallengeModule;

        private static bool IsItemChallengeWindowOpened => _uiModule.GetWidget<IItemChallengeWindow>() != null;
        private static bool NeedToShowBubble => _ItemChallengeModule.HasCompletableChallenges || _ItemChallengeModule.HasNewChallenges;

        public ChallengeBubbleSignalDispatcher(ItemChallengeEntry entry) {
            _entry = entry;
        }

        public void Subscribe() {
            _signalBus.Subscribe<StorageItemAmountChangedSignal>(OnStorageItemAmountChanged);
            _signalBus.Subscribe<ItemChallengeSlotCooldownCompletedSignal>(OnItemChallengeSlotCooldownCompleted);
            _signalBus.Subscribe<ItemChallengeChallengeRefusedSignal>(OnItemChallengeChallengeRefused);
            _signalBus.Subscribe<WidgetOnBeforeShowSignal>(OnWidgetOnBeforeShow);
            _signalBus.Subscribe<ItemChallengeChallengeCompletedSignal>(ItemChallengeChallengeCompleted);
        }

        private void OnItemChallengeChallengeRefused(ItemChallengeChallengeRefusedSignal signal) {
            Notify();
        }

        private void OnItemChallengeSlotCooldownCompleted(ItemChallengeSlotCooldownCompletedSignal signal) {
            if (!IsItemChallengeWindowOpened) {
                _entry.HasUnseenChallenge = true;
            }
            Notify();
        }

        private void OnWidgetOnBeforeShow(WidgetOnBeforeShowSignal signal) {
            if (signal.Props is IItemChallengeWindowProps) {
                _entry.HasUnseenChallenge = false;
            }
            Notify();
        }

        private void OnStorageItemAmountChanged(StorageItemAmountChangedSignal signal) => Notify();
        private void ItemChallengeChallengeCompleted(ItemChallengeChallengeCompletedSignal signal) => Notify();

        private void Notify() {
            _signalBus.Fire(new ItemChallengeBubbleSignal(NeedToShowBubble));
        }

        public void Unsubscribe() {
            _signalBus.Unsubscribe<StorageItemAmountChangedSignal>(OnStorageItemAmountChanged);
            _signalBus.Unsubscribe<ItemChallengeSlotCooldownCompletedSignal>(OnItemChallengeSlotCooldownCompleted);
            _signalBus.Unsubscribe<ItemChallengeChallengeRefusedSignal>(OnItemChallengeChallengeRefused);
            _signalBus.Unsubscribe<WidgetOnBeforeShowSignal>(OnWidgetOnBeforeShow);
            _signalBus.Unsubscribe<ItemChallengeChallengeCompletedSignal>(ItemChallengeChallengeCompleted);
        }
    }
}