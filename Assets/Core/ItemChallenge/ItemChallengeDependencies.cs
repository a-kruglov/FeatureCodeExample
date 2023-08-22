using Example.Api;

namespace Example.Locations.ItemChallenge {
    internal class ItemChallengeFeatureDependencies {
        internal static ItemChallengeConfigsProvider ConfigsProvider { get; private set; }
        internal static IStaticDataModule StaticDataModule { get; private set; }
        internal static IStaticDataHelperModule StaticDataHelperModule { get; private set; }
        internal static IItemChallengeModule ItemChallengeModule { get; private set; }
        internal static IGameEventsModule GameEventsModule { get; private set; }
        internal static IDepositoryModule DepositoryModule { get; private set; }
        internal static IConfigsModule ConfigsModule { get; private set; }
        internal static IRandomModule RandomModule { get; private set; }
        internal static IPlayerDossier PlayerDossier { get; private set; }
        internal static ITimersModule TimersModule { get; private set; }
        internal static IItemsRouter ItemsRouter { get; private set; }
        internal static ITimeModule TimeModule { get; private set; }
        internal static ILogModule LogModule { get; private set; }
        internal static IUIModule UIModule { get; private set; }
        internal static IGameSignalBus SignalBus { get; private set; }

        internal static void Create(
            IItemChallengeModule ItemChallengeModule,
            IStaticDataModule staticDataModule,
            IStaticDataHelperModule staticDataHelperModule,
            ItemChallengeConfigsProvider configsProvider,
            IGameEventsModule gameEventsModule,
            IDepositoryModule depositoryModule,
            IConfigsModule configsModule,
            IRandomModule randomModule,
            IPlayerDossier playerDossier,
            ITimersModule timersModule,
            IItemsRouter itemsRouter,
            ITimeModule timeModule,
            ILogModule logModule,
            IUIModule uiModule,
            IGameSignalBus signalBus
        ) {
            StaticDataModule = staticDataModule;
            StaticDataHelperModule = staticDataHelperModule;
            GameEventsModule = gameEventsModule;
            ItemChallengeModule = ItemChallengeModule;
            DepositoryModule = depositoryModule;
            ConfigsProvider = configsProvider;
            ConfigsModule = configsModule;
            RandomModule = randomModule;
            PlayerDossier = playerDossier;
            TimersModule = timersModule;
            ItemsRouter = itemsRouter;
            TimeModule = timeModule;
            LogModule = logModule;
            UIModule = uiModule;
            SignalBus = signalBus;
        }
    }
}
