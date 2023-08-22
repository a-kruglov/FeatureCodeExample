using Example.Api;

namespace Example.Locations.ItemChallenge {
    public class ItemChallengeSignalsHandler {
        private readonly IGameSignalBus _signalBus;
        private readonly IWindowsModule _windowsModule;
        private readonly ITrackItemChallenge _trackingModule;

        public ItemChallengeSignalsHandler(IGameSignalBus signalBus, IWindowsModule windowsModule, ITrackItemChallenge trackingModule) {
            _signalBus = signalBus;
            _windowsModule = windowsModule;
            _trackingModule = trackingModule;
        }

        public void Subscribe() {
            _signalBus.Subscribe<ItemChallengeChallengeCompletedSignal>(OnChallengeComplete);
            _signalBus.Subscribe<ItemChallengeChallengeRefusedSignal>(OnChallengeRefused);
            _signalBus.Subscribe<ItemChallengeChestOpenedSignal>(OnChestOpened);
        }

        public void Unsubscribe() {
            _signalBus.Unsubscribe<ItemChallengeChallengeCompletedSignal>(OnChallengeComplete);
            _signalBus.Unsubscribe<ItemChallengeChallengeRefusedSignal>(OnChallengeRefused);
            _signalBus.Unsubscribe<ItemChallengeChestOpenedSignal>(OnChestOpened);
        }

        private void OnChallengeComplete(ItemChallengeChallengeCompletedSignal signal) {
            _trackingModule.TrackItemChallengeChallengeComplete(signal.DifficultyType.ToString());
        }

        private void OnChallengeRefused(ItemChallengeChallengeRefusedSignal signal) {
            _trackingModule.TrackItemChallengeChallengeSkip(signal.DifficultyType.ToString());
        }
        
        private void OnChestOpened(ItemChallengeChestOpenedSignal signal) {
            _windowsModule.ShowItemChallengeRewardWindow(signal.Rewards);
            _trackingModule.TrackItemChallengeChestOpen();
        }
    }
}
