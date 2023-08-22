using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Expeditions.View;
using Game.Modules;
using UnityEngine;
using Example.Api;
using Example.Common;
using Example.Locations.ItemChallenge;

namespace Game.UI {
    public class ItemChallengeWindow : AbstractBuildingWindow<ItemChallengeWindowProps>, IChestDataSource, IItemChallengeWindow {

        [SerializeField] private List<ItemChallengeSlotView> _slotViews;
        [SerializeField] private ItemChallengeChestBarView chestBar;

        private static IItemChallengeModule ItemChallengeModule => GameModules.Get<IItemChallengeModule>();
        private static IItemsRouter ItemsRouter => GameModules.Get<IItemsRouter>();
        private static IStaticDataHelperModule StaticDataHelper => GameModules.Get<IStaticDataHelperModule>();
        
        
        protected override UniTask ConstructInternal() {
            chestBar.DataSource = this;
            for (int slotIndex = 0; slotIndex < _slotViews.Count; slotIndex++) {
                _slotViews[slotIndex].Init(slotIndex);
            }
            return base.ConstructInternal();
        }

        protected override void OnPropsApplied() {
            base.OnPropsApplied();
            SubscribeOnEvents();
        }

        protected override void BeforeShow() {
            base.BeforeShow();
            UpdateSlotsData();
            chestBar.UpdateVisual(GetChestData());
        }

        protected override void AfterShow() {
            base.AfterShow();
            TryGetRewardsFromLastPeriod();
            SubscribeOnButtons();
        }

        protected override void BeforeHide() {
            base.BeforeHide();
            UnsubscribeFromButtons();
        }

        protected override void BeforeClose() {
            base.BeforeClose();
            UnsubscribeFromEvents();
        }

        public IItemChallengeChestData GetChestData() => ItemChallengeModule.GetCurrentChestData();
        private static void TryGetRewardsFromLastPeriod() => ItemChallengeModule.TryTakeChestRewardsFromDeposit();

        private void UpdateSlotsData() {
            for (int i = 0; i < _slotViews.Count; i++) {
                UpdateSlot(i);
            }
        }

        private void UpdateSlotsVisual() {
            foreach (ItemChallengeSlotView slot in _slotViews) {
                slot.UpdateVisual();
            }
        }

        private void UpdateSlot(int index) {
            var slotView = _slotViews[index];
            slotView.UpdateData();
        }
        
        private void SubscribeOnEvents() {
            SignalBus.Subscribe<ItemChallengeSlotCooldownCompletedSignal>(OnSlotCooldownComplete);
            SignalBus.Subscribe<ItemChallengeChestProgressSignal>(OnChestProgress);
            SignalBus.Subscribe<ItemChallengeChestRenewSignal>(OnChestRenew);
            SignalBus.Subscribe<ItemChallengeStageCompletedSignal>(OnChestStageCompleted);
        }

        private void UnsubscribeFromEvents() {
            SignalBus.Unsubscribe<ItemChallengeSlotCooldownCompletedSignal>(OnSlotCooldownComplete);
            SignalBus.Unsubscribe<ItemChallengeChestProgressSignal>(OnChestProgress);
            SignalBus.Unsubscribe<ItemChallengeChestRenewSignal>(OnChestRenew);
            SignalBus.Unsubscribe<ItemChallengeStageCompletedSignal>(OnChestStageCompleted);
        }

        private void SubscribeOnButtons() => Enumerable.Range(0, _slotViews.Count).ToList().ForEach(SubscribeOnButtonsInChallenge);
        private void UnsubscribeFromButtons() => Enumerable.Range(0, _slotViews.Count).ToList().ForEach(UnsubscribeFromButtonsInChallenge);

        private void SubscribeOnButtonsInChallenge(int index) {
            _slotViews[index].FulfillButtonClickedEvent += OnFulfillRequested;
            _slotViews[index].RefuseButtonClickedEvent += OnRefuseRequested;
            _slotViews[index].CloseWindowRequestedEvent += Close;
        }

        private void UnsubscribeFromButtonsInChallenge(int index) {
            _slotViews[index].FulfillButtonClickedEvent -= OnFulfillRequested;
            _slotViews[index].RefuseButtonClickedEvent -= OnRefuseRequested;
            _slotViews[index].CloseWindowRequestedEvent -= Close;
        }
        
        private void OnSlotCooldownComplete(ItemChallengeSlotCooldownCompletedSignal signal) => AnimateSlot(signal.SlotIndex, SlotAnimationType.Renew);
        private void OnChestProgress(ItemChallengeChestProgressSignal signal) => chestBar.AnimateProgress(signal.Delta, signal.NewProgress, GetChestData());
        private void OnChestStageCompleted(ItemChallengeStageCompletedSignal signal) => chestBar.AnimateAsReadyToOpen(signal.Stage, GetChestData());
        
        private void OnChestRenew(ItemChallengeChestRenewSignal signal) {
            TryGetRewardsFromLastPeriod();
            chestBar.UpdateVisual(GetChestData());
        }

        private void OnFulfillRequested(int index) {
            UpdateSlotsData();
            var result = ItemChallengeModule.TryFulfillSlot(index);
            if (result) {
                AnimateSlot(index, SlotAnimationType.Fulfill);
            } else {
                var slot = ItemChallengeModule.GetSlotData(index);
                ShowForceFulfillWindow(slot).Forget();
            }
        }

        private async UniTask ShowForceFulfillWindow(IItemChallengeSlot slot) {
            var rewards = slot.GetItemsRemaining().Select(r => new RewardData(r.Item, r.Value));
            var rewardsDb = StaticDataHelper.RewardToStaticData(rewards);
            
            var crystals = slot.GetForceFulfillPrice().FirstOrDefault(r => r.Item == ItemsConstants.RealId);
            var props = PurchaseResourcesWindowProps.Create(rewardsDb, crystals);
            
            await ShowSubWindowAsync(props);
            if (props.PurchaseConfirmed) {
                OnForceFulfillConfirmed(slot, crystals);
            }
        }

        private void OnForceFulfillConfirmed(IItemChallengeSlot slot, GameItem crystalsRequired) {
            if (!ItemsRouter.IsEnough(crystalsRequired.Item, crystalsRequired.Value)) {
                var command = new ShowNotEnoughItemsCommand(crystalsRequired.Item, crystalsRequired.Value);
                command.Execute();
                return;
            }
            var result = ItemChallengeModule.TryForceFulfillSlot(slot.Index);
            if (result) {
                AnimateSlot(slot.Index, SlotAnimationType.Fulfill);
            }
        }

        private void OnRefuseRequested(int index) {
            var result = ItemChallengeModule.TryRefuseSlot(index);
            if (result) {
                AnimateSlot(index, SlotAnimationType.Refuse);
            }
        }

        private void AnimateSlot(int index, SlotAnimationType animationType) {
            var slotView = _slotViews[index];
            UpdateSlotsData();
            slotView.Animate(animationType);
            UpdateSlotsVisual();
        }
    }

    public interface IChestDataSource {
        IItemChallengeChestData GetChestData();
    }
}
