#nullable enable
using System;
using System.Reflection;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace MoonShared.Attributes;

/// <summary>
/// Marks a class as an SMAPI event handler container.
/// Each nested class represents a specific event type that the mod can subscribe to.
/// This allows methods to be annotated and automatically wired into SMAPI’s event system
/// without manual subscription in the mod’s main entry point.
/// </summary>
public class SEvent() : ClassHandler(9)
{
    /// <summary>
    /// Entry point for handling an event container class.
    /// Logs a warning if priority is too low since parsing events too early may cause issues.
    /// Calls base handler to parse nested methods.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        if (this.Priority < 2)
        {
            Log.Warn("Parsing events before parsing all other annotations might have unexpected results");
        }

        base.Handle(type, instance, mod);
    }

    // ───────────────────────────────────────────────────────────────
    // EVENT HANDLER CLASSES
    // Each nested class represents one type of SMAPI event and wires a method
    // annotated with that event to the appropriate SMAPI subscription.
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires after the GameLaunched event but delayed until the first UpdateTick.
    /// Useful for mods that need all other mods initialized before running.
    /// </summary>
    public class GameLaunchedLate : MethodHandler
    {
        private MethodInfo? _method;
        private IMod? _mod;
        private object? _instance;

        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            this._method = method;
            this._mod = mod;
            this._instance = instance;

            // Subscribe temporarily to UpdateTicked to delay execution
            mod.Helper.Events.GameLoop.UpdateTicked += this.DoAfterTick;
        }

        private void DoAfterTick(object? sender, UpdateTickedEventArgs e)
        {
            if (this._mod is null || this._method is null)
            {
                Log.Error("DoAfterTick had null Mod or Method");
                return;
            }

            // Unsubscribe immediately to run only once
            this._mod.Helper.Events.GameLoop.UpdateTicked -= this.DoAfterTick;

            // Invoke the annotated method
            this._method.Invoke(this._instance, [this, new GameLaunchedEventArgs()]);
        }
    }

    /// <summary>
    /// Triggers when a player stat changes.
    /// Automatically tracks old vs new value and passes delta to the annotated method.
    /// </summary>
    /// <param name="stat">The player stat to track, e.g., "Luck", "FarmingLevel".</param>
    public class StatChanged(string stat) : MethodHandler
    {
        private MethodInfo? _method;
        private object? _instance;

        // Per-screen storage ensures multiplayer compatibility
        private readonly PerScreen<uint> _currentStat = new();

        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            this._method = method;
            this._instance = instance;

            // Subscribe to load and periodic tick to track stat changes
            mod.Helper.Events.GameLoop.SaveLoaded += this.DoOnLoad;
            mod.Helper.Events.GameLoop.OneSecondUpdateTicked += this.DoOnOneSecondUpdateTicked;
        }

        private void DoOnLoad(object? sender, SaveLoadedEventArgs e)
        {
            // Initialize tracked value from player stats on save load
            this._currentStat.Value = Game1.player.stats.Get(stat);
        }

        private void DoOnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            uint newStat = Game1.player.stats.Get(stat);

            // Only trigger if the stat changed
            if (this._currentStat.Value == newStat)
                return;

            this._method?.Invoke(this._instance,
                [this, new EventArgs(this._currentStat.Value, newStat, (int)(newStat - this._currentStat.Value))]);

            this._currentStat.Value = newStat;
        }

        /// <summary>
        /// Encapsulates old, new, and delta values for a stat change.
        /// Passed automatically to the annotated method.
        /// </summary>
        public class EventArgs(uint oldStat, uint newStat, int delta) : System.EventArgs
        {
            public uint OldStat { get; } = oldStat;
            public uint NewStat { get; } = newStat;
            public int Delta { get; } = delta;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // AUTO-WIRED GAME LOOP EVENTS
    // Each of these classes subscribes a method to a SMAPI GameLoop event
    // using reflection to automatically convert the method to a delegate.
    // ───────────────────────────────────────────────────────────────

    public class UpdateTicking : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.UpdateTicking +=
                method.InitDelegate<EventHandler<UpdateTickingEventArgs>>(instance);
        }
    }

    public class UpdateTicked : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.UpdateTicked +=
                method.InitDelegate<EventHandler<UpdateTickedEventArgs>>(instance);
        }
    }

    public class OneSecondUpdateTicking : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.OneSecondUpdateTicking +=
                method.InitDelegate<EventHandler<OneSecondUpdateTickingEventArgs>>(instance);
        }
    }

    public class OneSecondUpdateTicked : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.OneSecondUpdateTicked +=
                method.InitDelegate<EventHandler<OneSecondUpdateTickedEventArgs>>(instance);
        }
    }

    public class SaveCreating : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.SaveCreating +=
                method.InitDelegate<EventHandler<SaveCreatingEventArgs>>(instance);
        }
    }

    public class SaveCreated : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.SaveCreated += method.InitDelegate<EventHandler<SaveCreatedEventArgs>>(instance);
        }
    }

    public class Saving : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.Saving += method.InitDelegate<EventHandler<SavingEventArgs>>(instance);
        }
    }

    public class Saved : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.Saved += method.InitDelegate<EventHandler<SavedEventArgs>>(instance);
        }
    }

    public class SaveLoaded : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.SaveLoaded += method.InitDelegate<EventHandler<SaveLoadedEventArgs>>(instance);
        }
    }

    public class DayStarted : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.DayStarted += method.InitDelegate<EventHandler<DayStartedEventArgs>>(instance);
        }
    }

    public class DayEnding : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.DayEnding += method.InitDelegate<EventHandler<DayEndingEventArgs>>(instance);
        }
    }

    public class TimeChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.TimeChanged += method.InitDelegate<EventHandler<TimeChangedEventArgs>>(instance);
        }
    }

    public class ReturnedToTitle : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.GameLoop.ReturnedToTitle +=
                method.InitDelegate<EventHandler<ReturnedToTitleEventArgs>>(instance);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // INPUT EVENTS
    // ───────────────────────────────────────────────────────────────

    public class ButtonsChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Input.ButtonsChanged +=
                method.InitDelegate<EventHandler<ButtonsChangedEventArgs>>(instance);
        }
    }

    public class ButtonPressed : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Input.ButtonPressed +=
                method.InitDelegate<EventHandler<ButtonPressedEventArgs>>(instance);
        }
    }

    public class ButtonReleased : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Input.ButtonReleased +=
                method.InitDelegate<EventHandler<ButtonReleasedEventArgs>>(instance);
        }
    }

    public class CursorMoved : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Input.CursorMoved += method.InitDelegate<EventHandler<CursorMovedEventArgs>>(instance);
        }
    }

    public class MouseWheelScrolled : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Input.MouseWheelScrolled +=
                method.InitDelegate<EventHandler<MouseWheelScrolledEventArgs>>(instance);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // MULTIPLAYER EVENTS
    // ───────────────────────────────────────────────────────────────

    public class PeerContextReceived : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Multiplayer.PeerContextReceived +=
                method.InitDelegate<EventHandler<PeerContextReceivedEventArgs>>(instance);
        }
    }

    public class PeerConnected : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Multiplayer.PeerConnected +=
                method.InitDelegate<EventHandler<PeerConnectedEventArgs>>(instance);
        }
    }

    public class ModMessageReceived : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Multiplayer.ModMessageReceived +=
                method.InitDelegate<EventHandler<ModMessageReceivedEventArgs>>(instance);
        }
    }

    public class PeerDisconnected : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Multiplayer.PeerDisconnected +=
                method.InitDelegate<EventHandler<PeerDisconnectedEventArgs>>(instance);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // PLAYER EVENTS
    // ───────────────────────────────────────────────────────────────

    public class InventoryChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Player.InventoryChanged +=
                method.InitDelegate<EventHandler<InventoryChangedEventArgs>>(instance);
        }
    }

    public class LevelChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Player.LevelChanged += method.InitDelegate<EventHandler<LevelChangedEventArgs>>(instance);
        }
    }

    public class Warped : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Player.Warped += method.InitDelegate<EventHandler<WarpedEventArgs>>(instance);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // WORLD EVENTS
    // ───────────────────────────────────────────────────────────────

    public class LocationListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.LocationListChanged +=
                method.InitDelegate<EventHandler<LocationListChangedEventArgs>>(instance);
        }
    }

    public class BuildingListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.BuildingListChanged +=
                method.InitDelegate<EventHandler<BuildingListChangedEventArgs>>(instance);
        }
    }

    public class ChestInventoryChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.ChestInventoryChanged +=
                method.InitDelegate<EventHandler<ChestInventoryChangedEventArgs>>(instance);
        }
    }

    public class DebrisListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.DebrisListChanged +=
                method.InitDelegate<EventHandler<DebrisListChangedEventArgs>>(instance);
        }
    }

    public class FurnitureListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.FurnitureListChanged +=
                method.InitDelegate<EventHandler<FurnitureListChangedEventArgs>>(instance);
        }
    }

    public class LargeTerrainFeatureListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.LargeTerrainFeatureListChanged +=
                method.InitDelegate<EventHandler<LargeTerrainFeatureListChangedEventArgs>>(instance);
        }
    }

    public class NpcListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.NpcListChanged +=
                method.InitDelegate<EventHandler<NpcListChangedEventArgs>>(instance);
        }
    }

    public class ObjectListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.ObjectListChanged +=
                method.InitDelegate<EventHandler<ObjectListChangedEventArgs>>(instance);
        }
    }

    public class TerrainFeatureListChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.World.TerrainFeatureListChanged +=
                method.InitDelegate<EventHandler<TerrainFeatureListChangedEventArgs>>(instance);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // DISPLAY EVENTS
    // ───────────────────────────────────────────────────────────────

    public class MenuChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.MenuChanged += method.InitDelegate<EventHandler<MenuChangedEventArgs>>(instance);
        }
    }

    public class Rendering : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.Rendering += method.InitDelegate<EventHandler<RenderingEventArgs>>(instance);
        }
    }

    public class Rendered : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.Rendered += method.InitDelegate<EventHandler<RenderedEventArgs>>(instance);
        }
    }

    public class RenderingWorld : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.RenderingWorld +=
                method.InitDelegate<EventHandler<RenderingWorldEventArgs>>(instance);
        }
    }

    public class RenderedWorld : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.RenderedWorld +=
                method.InitDelegate<EventHandler<RenderedWorldEventArgs>>(instance);
        }
    }

    public class RenderingActiveMenu : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.RenderingActiveMenu +=
                method.InitDelegate<EventHandler<RenderingActiveMenuEventArgs>>(instance);
        }
    }

    public class RenderedActiveMenu : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.RenderedActiveMenu +=
                method.InitDelegate<EventHandler<RenderedActiveMenuEventArgs>>(instance);
        }
    }

    public class RenderingHud : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.RenderingHud +=
                method.InitDelegate<EventHandler<RenderingHudEventArgs>>(instance);
        }
    }

    public class RenderedHud : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.RenderedHud += method.InitDelegate<EventHandler<RenderedHudEventArgs>>(instance);
        }
    }

    public class WindowResized : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Display.WindowResized +=
                method.InitDelegate<EventHandler<WindowResizedEventArgs>>(instance);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // CONTENT EVENTS
    // ───────────────────────────────────────────────────────────────

    public class AssetRequested : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Content.AssetRequested +=
                method.InitDelegate<EventHandler<AssetRequestedEventArgs>>(instance);
        }
    }

    public class AssetsInvalidated : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Content.AssetsInvalidated +=
                method.InitDelegate<EventHandler<AssetsInvalidatedEventArgs>>(instance);
        }
    }

    public class AssetReady : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Content.AssetReady += method.InitDelegate<EventHandler<AssetReadyEventArgs>>(instance);
        }
    }

    public class LocaleChanged : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            mod.Helper.Events.Content.LocaleChanged +=
                method.InitDelegate<EventHandler<LocaleChangedEventArgs>>(instance);
        }
    }
}
