using System;
using System.Collections.Generic;
using System.Linq;
using Game.Expeditions.View;
using Game.Modules;
using UnityEngine;
using UnityEngine.UI;
using Example.Animations;
using Example.Api;
using Example.Common;
using Example.Extensions;
using Example.UI;

namespace Game.UI {
    public enum SlotAnimationType {
        Fulfill,
        Renew,
        Refuse
    }
    public class ItemChallengeSlotView : MonoBehaviour {

        private static readonly int _isActiveBool = Animator.StringToHash("IsActive");
        private static readonly int _isSpecialBool = Animator.StringToHash("IsSpecial");
        private static readonly int _completeTrigger = Animator.StringToHash("Complete");
        
        private static readonly int _afterCompleteAppear = Animator.StringToHash("AfterCompleteAppear");
        private static readonly int _afterRenewAppear = Animator.StringToHash("AfterRenewAppear");
        private static readonly int _afterRefuseAppear = Animator.StringToHash("AfterRefuseAppear");

        [SerializeField] private Animator _animator;
        [SerializeField] private Button _fulfillButton;
        [SerializeField] private Button _refuseButton;
        [SerializeField] private TimerSlider _timer;
        [SerializeField] private VisualState _ChallengeOneCardLayout;
        [SerializeField] private VisualState _ChallengeTwoCardsLayout;
        [SerializeField] private ItemDuplicator<UpgradePriceItem> _ChallengeTargets;
        [SerializeField] private ItemDuplicator<RewardView> _rewards;
        [SerializeField] private AnimationEventAdapter _animationEventAdapter;
        
        private static IItemChallengeModule ItemChallengeModule => GameModules.Get<IItemChallengeModule>();
        private static IItemsRouter ItemsRouter => GameModules.Get<IItemsRouter>();
        private static ICameraModule CameraModule => GameModules.Get<ICameraModule>();
        private static IUIModule UIModule => GameModules.Get<IUIModule>();
        private static IGameSignalBus SignalBus => GameModules.Get<IGameSignalBusesModule>().Default;
        
        public event Action<int> FulfillButtonClickedEvent;
        public event Action<int> RefuseButtonClickedEvent;
        public event Action CloseWindowRequestedEvent;
        
        public int SlotIndex { get; private set; }
        private IItemChallengeSlot Slot { get; set; }
        private IEnumerable<RewardView> RewardViews => _rewards;
        
        private bool _isReadyForUpdateVisual;
        
        private bool IsActive => Slot != null && ItemChallengeModule.IsSlotActive(Slot.Index);
        private ChallengeDifficultyType DifficultyType => Slot?.DifficultyType ?? ChallengeDifficultyType.Undefined;

        private bool EnoughResourcesToSpeedup {
            get {
                var speedUpCost = ItemChallengeModule.GetSlotSpeedupCost(SlotIndex);
                return speedUpCost == null || ItemsRouter.IsEnough(speedUpCost.Item, speedUpCost.Value);
            }
        }

        private void Awake() {
            _timer.SetAction(OnSpeedupClicked);
            _animationEventAdapter.SubscribeOnValue("ReadyForInteraction", OnReadyForInteractionEvent);
            _animationEventAdapter.SubscribeOnValue("ReadyForUpdateVisual", OnReadyForUpdateVisualEvent);
            SubscribeOnButtons();
            _isReadyForUpdateVisual = true;
        }

        public void Init(int slotIndex) {
            SlotIndex = slotIndex;
        }
        
        public void UpdateData() {
            Slot = ItemChallengeModule.GetSlotData(SlotIndex);
            UpdateAnimatorParameters();
        }

        public void Animate(SlotAnimationType type) {
            SetViewInteractable(false);
            _isReadyForUpdateVisual = false;
            switch (type) {
                case SlotAnimationType.Fulfill:
                    AnimateFulfill();
                    break;
                case SlotAnimationType.Renew:
                    AnimateRenew();
                    break;
                case SlotAnimationType.Refuse:
                    AnimateRefuse();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void UpdateVisual() {
            if (!_isReadyForUpdateVisual) {
                return;
            }
            UpdateVisualInternal();
        }

        private void AnimateFulfill() {
            AnimateDrop();
            _animator.SetTriggerAndResetOthers(_afterCompleteAppear);
            _animator.SetTrigger(_completeTrigger);
        }
        
        private void AnimateRenew() {
            _animator.SetTriggerAndResetOthers(_afterRenewAppear);
        }
        
        private void AnimateRefuse() {
            _animator.SetTriggerAndResetOthers(_afterRefuseAppear);
        }

        private void AnimateDrop() {
            var data = RewardViews.Select(v => v.ItemData);
            foreach (var reward in data) {
                var dropStartPosition = RewardViews.Select(v => v.transform.position)
                    .Aggregate(Vector3.zero, (a, b) => a + b) / RewardViews.Count();
                var position = CameraModule.GuiToLocationPosition(dropStartPosition);
                var amount = ItemsRouter.GetResourceAmount(reward.Item);
                var itemData = new GameItem(reward.Item, amount);
                SignalBus.Fire(new CreateDropAnimationSignal(itemData, position).AsForWindow());
            }
        }

        private void OnReadyForInteractionEvent() {
            SetViewInteractable(true);
        }
        
        private void OnReadyForUpdateVisualEvent() {
            _isReadyForUpdateVisual = true;
            UpdateVisual();
        }

        private void SetViewInteractable(bool value) {
            _fulfillButton.interactable = value;
            _fulfillButton.interactable = value;
        }
        
        private void OnFulfillButtonClicked() {
            FulfillButtonClickedEvent?.Invoke(Slot.Index);
        }
        
        private void OnRefuseButtonClicked() {
            RefuseButtonClickedEvent?.Invoke(Slot.Index);
        }

        private void UpdateVisualInternal() {
            if (IsActive) {
                SetTargetItems();
                SetRewards();
            } else {
                SetTimer();
            }
            UpdateAnimatorParameters();
        }
        
        private void UpdateAnimatorParameters() {
            _animator.SetBool(_isActiveBool, IsActive);
            _animator.SetBool(_isSpecialBool, DifficultyType == ChallengeDifficultyType.Hard);
        }
        
        private void SubscribeOnButtons() {
            _fulfillButton.onClick.AddListener(OnFulfillButtonClicked);
            _refuseButton.onClick.AddListener(OnRefuseButtonClicked);
        }
        
        private void OnSpeedupClicked() {
            if (EnoughResourcesToSpeedup) {
                var process = new ChallengeCooldownProcess(SlotIndex);
                if (process.IsFree) {
                    process.SpeedUp();
                } else {
                    var props = SkipChallengeSlotCooldownWindowProps.Create(process, () => {
                        process.SpeedUp();
                    });
                    UIModule.ShowWindow(props);
                }
            } else {
                var cost = ItemChallengeModule.GetSlotSpeedupCost(Slot.Index);
                var command = new ShowNotEnoughItemsCommand(cost.Item, cost.Value);
                command.Execute();
            }
        }

        private void SetTimer() {
            var process = new ChallengeCooldownProcess(SlotIndex);
            _timer.SetTimer(process);
        }

        private void SetTargetItems() {
            _ChallengeTargets.Setup(Slot.Inputs, (item, data) => {
                item.SetData(data.Item, data.Value, CloseWindowRequested);
                item.gameObject.SetActive(true);
            });
            if (_ChallengeTargets.Count == 2) {
                _ChallengeTwoCardsLayout.TryApplyVisual();
            } else {
                _ChallengeOneCardLayout.TryApplyVisual();
            }
            
        }

        private void CloseWindowRequested() => CloseWindowRequestedEvent?.Invoke();
        
        private void SetRewards() {
            _rewards.Setup(Slot.Outputs, (item, data) => {
                var aggregatorData = new RewardViewData(data);
                item.Setup(aggregatorData);
            });
        }
    }
}
