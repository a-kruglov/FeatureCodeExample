using Example.Api;

namespace Example.Locations.ItemChallenge  {
    public class ItemChallengeSlotCooldownCompletedSignal : IGameSignal {
        public int SlotIndex { get; }
        
        public ItemChallengeSlotCooldownCompletedSignal(int index) {
            SlotIndex = index;
        }
    }
}
