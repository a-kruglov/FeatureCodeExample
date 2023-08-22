using Example.Api;

namespace Example.Locations.ItemChallenge {
    public class ItemChallengeBubbleSignal : IGameSignal {
        public bool NeedToShowBubble { get; }

        public ItemChallengeBubbleSignal(bool needToShowBubble) {
            NeedToShowBubble = needToShowBubble;
        }
    }
}
