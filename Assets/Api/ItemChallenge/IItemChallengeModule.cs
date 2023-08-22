namespace Example.Api {
    public interface IItemChallengeModule : IModule, IInitializableWithChallenge, ITerminable {
        public bool IsChestRefreshEnabled { get; }
        public bool HasNewChallenges { get; }
        public bool HasCompletableChallenges { get; }


        
        public IItemChallengeSlot GetSlotData(int index);
        public IItemChallengeChestData GetCurrentChestData();
        public GameItem GetSlotSpeedupCost(int index);

        public bool IsSlotActive(int slotIndex);

        public bool TryFulfillSlot(int slotIndex);
        public bool TryRefuseSlot(int slotIndex);
        public bool TrySpeedupSlot(int index);
        public bool TryForceFulfillSlot(int slotIndex);
        public bool TryOpenChest(int stage);
        public bool TryTakeChestRewardsFromDeposit();
        
        public long GetSlotSpeedUpFreeTime();
        public long GetSlotCooldownTimestamp(int slotIndex);
        public int GetSlotCooldownDuration(int slotIndex);

        public void ForceChestRefresh(); }
}
