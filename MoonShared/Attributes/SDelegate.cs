#nullable enable
using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.TokenizableStrings;
using StardewValley.Triggers;

namespace MoonShared.Attributes;

/// <summary>
/// Marks a class that registers delegates, actions, and queries in Stardew Valley.
/// Automatically wires methods to the appropriate game systems.
/// </summary>
public class SDelegate() : ClassHandler(2)
{
    /// <summary>
    /// Handles the delegate class itself. 
    /// Currently just passes through to the base handler.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        base.Handle(type, instance, mod);
    }

    // ──────────────────────────────────────────────
    // NESTED METHOD HANDLERS
    // Each nested class wires a method to a specific Stardew Valley delegate or action.
    // ──────────────────────────────────────────────

    /// <summary>
    /// Registers a custom event command for in-game events.
    /// </summary>
    public class EventCommand : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            Event.RegisterCommand($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<EventCommandDelegate>(instance));
        }
    }

    /// <summary>
    /// Registers a precondition for an event command.
    /// </summary>
    public class EventPrecondition : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            Event.RegisterPrecondition($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<EventPreconditionDelegate>(instance));
        }
    }

    /// <summary>
    /// Registers a query that can check the game state.
    /// </summary>
    public class GameStateQuery : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            StardewValley.GameStateQuery.Register($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<GameStateQueryDelegate>(instance));
        }
    }

    /// <summary>
    /// Registers a resolver for item queries in-game.
    /// </summary>
    public class ResolveItemQuery : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            ItemQueryResolver.ItemResolvers.Add($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<ResolveItemQueryDelegate>(instance));
        }
    }

    /// <summary>
    /// Registers a custom token parser for tokenizable strings.
    /// </summary>
    public class TokenParser : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            StardewValley.TokenizableStrings.TokenParser.RegisterParser($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<TokenParserDelegate>(instance));
        }
    }

    /// <summary>
    /// Registers a custom touch action on a game location.
    /// </summary>
    public class TouchAction : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            GameLocation.RegisterTouchAction($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<Action<GameLocation, string[], Farmer, Vector2>>(instance));
        }
    }

    /// <summary>
    /// Registers a tile action that can be triggered by the player.
    /// </summary>
    public class TileAction : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            GameLocation.RegisterTileAction($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<Func<GameLocation, string[], Farmer, Point, bool>>(instance));
        }
    }

    /// <summary>
    /// Registers a custom trigger action.
    /// </summary>
    public class TriggerAction : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            TriggerActionManager.RegisterAction($"{mod.ModManifest.UniqueID}_{method.Name}", method.InitDelegate<TriggerActionDelegate>(instance));
        }
    }
}
