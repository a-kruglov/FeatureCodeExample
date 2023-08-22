using Game.Modules;
using UnityEngine;
using Example.Api;
using Example.UI;

namespace Game.UI {
    public class IsItemChallengeCardActiveCondition : VisualStateCondition {
        private static IItemChallengeModule ItemChallengeModule => GameModules.Get<IItemChallengeModule>();

        [SerializeField] private ItemChallengeSlotView _slotView;
        protected override bool CheckInternal() => ItemChallengeModule.IsSlotActive(_slotView.SlotIndex);
    }
}
