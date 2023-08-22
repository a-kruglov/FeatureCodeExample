using System.Collections.Generic;
using Example.Api;

namespace Example.Locations.ItemChallenge {
    public class ChallengeCompleteEventStrategies : ICheckStrategy {

        public IReadOnlyList<IEventSignalData> GetInitialEventsSignals(EventActivationDbData evt) => null;
        
        public CheckStrategyResult CheckAndChange(EventActivationDbData evt, IExtendedGameEventSignal signal) {
            if (signal is { Value: > 0 }) {
                evt.Value -= signal.Value;
                return evt.Value <= 0 ? CheckStrategyResult.Completed : CheckStrategyResult.Changed;
            }
            
            return evt.Value <= 0 ? CheckStrategyResult.Completed : CheckStrategyResult.Unchanged;
        }
    }
}
