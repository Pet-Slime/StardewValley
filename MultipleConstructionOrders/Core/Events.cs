using System;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewModdingAPI.Events;
using StardewValley;

namespace MultipleConstructionOrders.Core
{
    [SEvent]
    public class Events
    {
        internal static bool RobinCanAcceptConstructionOrdersToday { get; private set; }

        public const string ConstructionWorkerSpriteAsset = "Mods/moonslime.MultipleConstructionOrders/textures";
        public const string ConstructionWorkerPortraitPrefix = "Portraits/moonslime_MCO_ConstructionWorker_";

        [SEvent.SaveLoaded]
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            RefreshConstructionState();
        }

        [SEvent.DayStarted]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            RefreshConstructionState();
            ConstructionOrderManager.SpawnTemporaryWorkers();
        }

        [SEvent.DayEnding]
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            // Important:
            // Do not let today's "Robin can take orders" value leak into tomorrow morning.
            RobinCanAcceptConstructionOrdersToday = false;

            ConstructionOrderManager.ClearRobinOrderDay();
            ConstructionOrderManager.RemoveTemporaryWorkers();
            ConstructionOrderManager.AssignConstructionWorkers();
        }

        [SEvent.ReturnedToTitle]
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            RobinCanAcceptConstructionOrdersToday = false;

            ConstructionOrderManager.ClearRobinOrderDay();
            ConstructionOrderManager.RemoveTemporaryWorkers();
        }

        internal static void RefreshConstructionState()
        {
            ConstructionOrderManager.CleanFinishedConstructionTags();
            ConstructionOrderManager.AssignConstructionWorkers();

            RobinCanAcceptConstructionOrdersToday = ConstructionOrderManager.CanRobinAcceptOrdersToday();

            Log.Trace($"Can Robin accept multiple orders today: {RobinCanAcceptConstructionOrdersToday}");
        }


        [SEvent.AssetRequested]
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {

            // This prevents the game from logging missing portrait warnings for temporary worker NPCs.
            if (e.NameWithoutLocale.Name.StartsWith(ConstructionWorkerPortraitPrefix, StringComparison.Ordinal))
            {
                e.LoadFrom(
                    () => Game1.content.Load<Texture2D>("Portraits/Robin"),
                    AssetLoadPriority.Exclusive
                );
            }
        }
    }
}
