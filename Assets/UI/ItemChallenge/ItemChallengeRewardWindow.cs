using UnityEngine;
using UnityEngine.UI;
using Example.UI;

namespace Game.UI {
    public class ItemChallengeRewardWindow : Window<ItemChallengeRewardWindowProps> {
        [SerializeField] private Button _okButton;
        [SerializeField] private RewardsRowsList _rewards;

        protected override void OnConstructed() {
            base.OnConstructed();
            _okButton.onClick.AddListener(Close);
        }

        protected override void OnPropsApplied() {
            base.OnPropsApplied();
            _rewards.Setup(Props.Rewards, RewardItemTooltipProps.Create);
        }

        protected override void BeforeClose() {
            base.BeforeClose();
            _rewards.Clear();
        }
    }
}
