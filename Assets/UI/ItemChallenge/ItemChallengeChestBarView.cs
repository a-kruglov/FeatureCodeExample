using System;
using System.Collections.Generic;
using System.Linq;
using Game.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Example.Animations;
using Example.Api;

namespace Game.UI {
    public class ItemChallengeChestBarView : MonoBehaviour {
        private static readonly int _isOpenedBool = Animator.StringToHash("IsOpened");
        private static readonly int _isReadyToOpenBool = Animator.StringToHash("IsReadyToOpen");
        private static readonly int _progressChangedTrigger = Animator.StringToHash("ProgressChanged");
        private static readonly int _stageReachedTrigger = Animator.StringToHash("StageReached");

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimatedProgressBar _progressBar;
        [SerializeField] private AnimatedCounter _counter;
        [SerializeField] private List<ChestView> _chestViews;
        [SerializeField] private TimerSlider _timer;
        
        public IChestDataSource DataSource { get; set; }

        private static ITimeModule TimeModule => GameModules.Get<ITimeModule>();
        private static IUIModule UIModule => GameModules.Get<IUIModule>();
        private static IItemChallengeModule ItemChallengeModule => GameModules.Get<IItemChallengeModule>();

        
        public void UpdateVisual(IItemChallengeChestData data) {
            SetupWithData(data);
        }
        
        public void AnimateProgress(int delta, int newProgress, IItemChallengeChestData data) {
            float sliderValueNew = NormalizeSegmentedValue(newProgress, data);
            float sliderValueOld = NormalizeSegmentedValue(newProgress - delta, data);
            _progressBar.SetValues(sliderValueOld, sliderValueNew, 1);
            _counter.SetValues(newProgress - delta, newProgress, data.MaxProgress);
            _animator.SetTrigger(_progressChangedTrigger);
        }

        public void AnimateAsReadyToOpen(int stage, IItemChallengeChestData data) {
            _chestViews[stage].Animator.SetTrigger(_stageReachedTrigger);
            UpdateAnimatorBoolParams(stage, data);
        }

        private void SetupWithData(IItemChallengeChestData data) {
            UpdateProgressBar(data);
            UpdateChestViews(data);
            UpdateTimer(data);
            UpdateCounter(data);
        }

        private void OnChestClick(int stage) {
            var data = DataSource.GetChestData();
            if (data.StageStatuses.ElementAt(stage) == ChallengeChestStageStatus.Locked) {
                ShowChestRewardTooltip(data, stage);
            } else {
                ItemChallengeModule.TryOpenChest(stage);
                data = DataSource.GetChestData();
            }
            UpdateAnimatorBoolParams(stage, data);
        }

        private void ShowChestRewardTooltip(IItemChallengeChestData data, int stage) {
            var rewards = data.Data.Stages.ElementAt(stage).Rewards;
            var props = ChestRewardTooltipProps.Create(rewards);
            UIModule.ShowTooltip(props, _chestViews[stage].TooltipContainer);
        }

        private void UpdateProgressBar(IItemChallengeChestData data) {
            _progressBar.SetValuesImmediately(data.CurrentProgress / (float) data.MaxProgress, 1);
        }

        private void UpdateChestViews(IItemChallengeChestData data) {
            for (int i = 0; i < data.Data.Stages.Count; i++) {
                UpdateChestView(i, data);
            }
        }

        private void UpdateChestView(int stage, IItemChallengeChestData data) {
            var requiredNumberOfChallengesToUnlock = data.GetRequiredNumberOfChallengesForStage(stage);

            var chestView = _chestViews[stage];
            chestView.Text.text = requiredNumberOfChallengesToUnlock.ToString();
            chestView.Button.onClick.RemoveAllListeners();
            chestView.Button.onClick.AddListener(() => OnChestClick(stage));

            UpdateAnimatorBoolParams(stage, data);
        }

        private void UpdateAnimatorBoolParams(int stage, IItemChallengeChestData data) {
            var chestView = _chestViews[stage];
            var isRewardReceived = data.StageStatuses[stage] == ChallengeChestStageStatus.Opened;
            chestView.Animator.SetBool(_isOpenedBool, isRewardReceived);

            var isReadyToOpen = data.StageStatuses[stage] == ChallengeChestStageStatus.ReadyToOpen;
            chestView.Animator.SetBool(_isReadyToOpenBool, isReadyToOpen);
        }

        private void UpdateTimer(IItemChallengeChestData data) {
            if (!ItemChallengeModule.IsChestRefreshEnabled) {
                _timer.gameObject.SetActive(false);
                return;
            }
            
            _timer.gameObject.SetActive(true);
            var currentTime = TimeModule.UtcNowTimeStamp;
            var timeLeft = (int)(data.RefreshTimestamp - currentTime);
            _timer.SetTimerByValues(timeLeft, data.RefreshTimestamp);
        }

        private void UpdateCounter(IItemChallengeChestData data) {
            _counter.SetValuesImmediately(data.CurrentProgress, data.MaxProgress);
        }
 
        private static float NormalizeSegmentedValue(float value, IItemChallengeChestData data) {
            if (value >= data.MaxProgress){
                return data.MaxProgress;
            }
            if (value <= 0) {
                return 0;
            }
            var stagesCount = data.Data.Stages.Count;
            var visualNormalizedStep = 1f / stagesCount;
            for (int i = 0; i < stagesCount; i++) {
                var segmentMin = data.GetRequiredNumberOfChallengesForStage(i - 1);
                var segmentMax = data.GetRequiredNumberOfChallengesForStage(i);
                if (value < segmentMin || value >= segmentMax) {
                    continue;
                }
                return Mathf.Clamp01(visualNormalizedStep * i + (value - segmentMin) / (segmentMax - segmentMin) * visualNormalizedStep);
            }
            return 0;
        }
        
        [Serializable]
        private class ChestView {
            public Animator Animator;
            public Button Button;
            public TMP_Text Text;
            public RectTransform TooltipContainer;
        }
    }
}
