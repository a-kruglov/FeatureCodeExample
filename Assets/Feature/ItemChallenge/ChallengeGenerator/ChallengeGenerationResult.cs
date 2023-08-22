namespace Example.Locations.ItemChallenge {
    public class ChallengeGenerationResult {
        internal ItemChallengeSlot Slot { get; }
        public string DebugInfo { get; }
        
        internal ChallengeGenerationResult(ItemChallengeSlot slot, string debugInfo) {
            Slot = slot;
            DebugInfo = debugInfo;
        }
    }
}
