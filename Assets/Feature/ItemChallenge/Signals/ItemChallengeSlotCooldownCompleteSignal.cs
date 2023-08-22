using Example.Api;

namespace Example.Locations.ItemChallenge  {
    public class ItemChallengeSlotCooldownCompleteSignal : IGameSignal {
        public int SlotIndex { get; }
        
        public ItemChallengeSlotCooldownCompleteSignal(int index) {
            SlotIndex = index;
        }
    }
}
