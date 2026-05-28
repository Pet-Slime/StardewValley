using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.Attributes;
using SpaceCore;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using xTile.Dimensions;

namespace ThievingSkill.Core
{
    internal static class LockpickManager
    {
        private const string DoorMemoryPrefix = "moonslime.Thieving.LockpickedDoor/";

        // Pull lockpicking balance values from the mod config
        public static int LockpickingExp => ModEntry.Config.LockpickingExp;
        public static float WitnessRadius => ModEntry.Config.LockpickingWitnessRadius;
        public static int WitnessFriendshipLoss => ModEntry.Config.LockpickingWitnessFriendshipLoss;

        private static readonly LockpickInfo[] Lockpicks =
        {
            // Best lockpicks first. If the player is not holding a lockpick, the best available one is used.
            new LockpickInfo("moonslime.Thieving.lockpick_iridium", 0.30),
            new LockpickInfo("moonslime.Thieving.lockpick_gold", 0.50),
            new LockpickInfo("moonslime.Thieving.lockpick_iron", 0.70),
            new LockpickInfo("moonslime.Thieving.lockpick_copper", 0.90)
        };

        public static bool TryUseLockpick(Farmer farmer, string lockContext, string doorMemoryKey)
        {
            // Make sure there is a valid local player before doing anything
            if (farmer == null || !farmer.IsLocalPlayer)
                return false;

            // If the player already picked this door today, let them use it again without another lockpick roll
            if (HasPickedDoorToday(farmer, doorMemoryKey))
            {
                Log.Trace($"Thieving skill: Reusing remembered lockpick access for {lockContext}.");
                return true;
            }

            // Find the lockpick the player should use
            // This respects the held lockpick first, then checks inventory from best to worst
            if (!TryFindLockpick(farmer, out Item lockpick, out LockpickInfo lockpickInfo))
            {
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.lockpick_needed"), 3));
                return false;
            }

            Log.Trace($"Thieving skill: Used {lockpickInfo.DefId} to bypass {lockContext}.");

            // Remember this door so the player does not get stuck if the lockpick breaks
            RememberPickedDoor(farmer, doorMemoryKey);

            // Give lockpicking EXP if configured
            if (LockpickingExp > 0)
                Utilities.AddEXP(farmer, LockpickingExp);

            // Phantom Reduces witness Radius
            int newWitnessRaidus = (int)WitnessRadius;
            if (farmer.HasCustomProfession(Events.Proffession10b1))
                newWitnessRaidus = newWitnessRaidus >> 1;

            // If nearby NPCs can witness lockpicking, make them lose friendship
            if (newWitnessRaidus > 0 && WitnessFriendshipLoss > 0)
                Utilities.LoseFriendshipWithNpcsInRange(farmer, newWitnessRaidus, WitnessFriendshipLoss);

            // Play feedback so the player knows the lockpick was used
            Game1.playSound("stoneCrack");

            // Calculate the lockpick's break chance after the player's Thieving level reduction
            double adjustedBreakChance = GetAdjustedBreakChance(farmer, lockpickInfo);

            // Roll to see if the lockpick breaks
            if (Game1.random.NextDouble() < adjustedBreakChance)
            {
                RemoveOneLockpick(farmer, lockpick);
                Game1.playSound("clank");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.lockpick_broke"), 3));
            }

            return true;
        }

        public static void ClearDailyDoorMemory(Farmer farmer)
        {
            // Make sure there is a farmer to clear data from
            if (farmer == null)
                return;

            // Find every remembered lockpicked door key
            List<string> keysToRemove = farmer.modDataForSerialization.Keys.Where(key => key.StartsWith(DoorMemoryPrefix)).ToList();

            // Remove all remembered doors so tomorrow requires lockpicks again
            foreach (string key in keysToRemove)
                farmer.modDataForSerialization.Remove(key);
        }

        public static string BuildDoorMemoryKey(GameLocation location, string lockType, string detail)
        {
            // Build a stable key for warp-style locked doors
            string locationName = location?.NameOrUniqueName ?? "Unknown";
            return $"{locationName}|{lockType}|{detail}";
        }

        public static string BuildDoorMemoryKey(GameLocation location, string[] action, Location tileLocation)
        {
            // Build a stable key for action/touch-action locked doors
            string locationName = location?.NameOrUniqueName ?? "Unknown";
            string actionName = ArgUtility.Get(action, 0, "Unknown");
            string fullAction = action != null ? string.Join(" ", action) : "";

            return $"{locationName}|{actionName}|{tileLocation.X},{tileLocation.Y}|{fullAction}";
        }

        private static bool HasPickedDoorToday(Farmer farmer, string doorMemoryKey)
        {
            // If the farmer or memory key is invalid, assume the door was not picked today
            if (farmer == null || string.IsNullOrWhiteSpace(doorMemoryKey))
                return false;

            // Build the full modData key
            string key = DoorMemoryPrefix + doorMemoryKey;

            // The stored value is the day number the door was picked
            return farmer.modDataForSerialization.TryGetValue(key, out string day) && day == Game1.Date.TotalDays.ToString();
        }

        private static void RememberPickedDoor(Farmer farmer, string doorMemoryKey)
        {
            // If the farmer or memory key is invalid, there is nothing to remember
            if (farmer == null || string.IsNullOrWhiteSpace(doorMemoryKey))
                return;

            // Store the current day so the door can be reused until daily memory is cleared
            string key = DoorMemoryPrefix + doorMemoryKey;
            farmer.modDataForSerialization[key] = Game1.Date.TotalDays.ToString();
        }

        private static double GetAdjustedBreakChance(Farmer farmer, LockpickInfo lockpickInfo)
        {
            // Get the player's Thieving level
            int level = Utilities.GetLevel(farmer);

            // Each Thieving level reduces the lockpick break chance by 1%
            double levelProtection = level * 0.01;

            // Subtract the level protection from the lockpick's base break chance
            double adjustedBreakChance = lockpickInfo.BreakChance - levelProtection;

            // If the player has the Rogue profession, reduce lockpick break chance by another 10%.
            if (farmer.HasCustomProfession(Events.Proffession10b2))
                adjustedBreakChance -= 0.1;

            // Make sure break chance never goes below 0%
            return Math.Max(0.0, adjustedBreakChance);
        }

        private static bool TryFindLockpick(Farmer farmer, out Item lockpick, out LockpickInfo lockpickInfo)
        {
            lockpick = null;
            lockpickInfo = null;

            // If the player is actively holding a lockpick, respect that choice first.
            if (TryGetLockpickInfo(farmer.ActiveObject, out lockpickInfo))
            {
                lockpick = farmer.ActiveObject;
                return true;
            }

            // Otherwise, search the player's inventory from best lockpick to worst lockpick
            foreach (LockpickInfo info in Lockpicks)
            {
                foreach (Item item in farmer.Items)
                {
                    if (MatchesLockpick(item, info.DefId))
                    {
                        lockpick = item;
                        lockpickInfo = info;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetLockpickInfo(Item item, out LockpickInfo lockpickInfo)
        {
            lockpickInfo = null;

            // Check if this item matches any known lockpick tier
            foreach (LockpickInfo info in Lockpicks)
            {
                if (MatchesLockpick(item, info.DefId))
                {
                    lockpickInfo = info;
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesLockpick(Item item, string defId)
        {
            // Null items cannot be lockpicks
            if (item == null)
                return false;

            // Check both raw item ID and qualified object ID
            return item.ItemId == defId || item.QualifiedItemId == "(O)" + defId;
        }

        private static void RemoveOneLockpick(Farmer farmer, Item lockpick)
        {
            // If there is no lockpick item, there is nothing to remove
            if (lockpick == null)
                return;

            // Remove one from the stack
            lockpick.Stack--;

            // If the stack is empty, remove the item from the inventory
            if (lockpick.Stack <= 0)
                farmer.removeItemFromInventory(lockpick);
        }

        private sealed class LockpickInfo
        {
            public string DefId { get; }
            public double BreakChance { get; }

            public LockpickInfo(string defId, double breakChance)
            {
                DefId = defId;
                BreakChance = breakChance;
            }
        }
    }

    internal static class LockpickDoorUtility
    {
        public static bool IsLockedDoorWarp(GameLocation location, string locationName, int openTime, int closeTime, string npcName, int minFriendship)
        {
            // If there is no location or player, do not interfere with vanilla
            if (location == null || Game1.player == null)
                return false;

            // Start with the vanilla town key state
            bool townKeyWorks = Game1.player.HasTownKey;

            // The town key only works in the valley context
            if (townKeyWorks && !location.InValleyContext())
                townKeyWorks = false;

            // The town key has special Night Market behavior
            if (townKeyWorks && location is BeachNightMarket && locationName != "FishShop")
                townKeyWorks = false;

            // The vanilla method checks festival closure before normal hours.
            if (GameLocation.AreStoresClosedForFestival() && location.InValleyContext())
                return true;

            // The vanilla method lets the Town Key bypass Pierre's Wednesday closure.
            if (locationName == "SeedShop" && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Wed") && !Utility.HasAnyPlayerSeenEvent("191393") && !townKeyWorks)
                return true;

            // Willy's shop opens earlier after the player gets the mail flag
            if (locationName == "FishShop" && Game1.player.mailReceived.Contains("willyHours"))
                openTime = 800;

            // Check if vanilla time restrictions would allow entry
            bool timePassed = townKeyWorks || (Game1.timeOfDay >= openTime && Game1.timeOfDay < closeTime);

            // Check if vanilla friendship restrictions would allow entry
            bool friendshipPassed = minFriendship <= 0 || location.IsWinterHere() || HasRequiredFriendship(Game1.player, npcName, minFriendship);

            // Vanilla lets most doors open during the year-one green rain.
            if (location.IsGreenRainingHere() && Game1.year == 1 && location is not Beach && location is not Forest && locationName != "AdventureGuild")
                return false;

            // If either the time or friendship check fails, this warp is locked
            return !timePassed || !friendshipPassed;
        }

        public static bool IsDoorActionLocked(GameLocation location, string[] action, Farmer who)
        {
            // Make sure this is a valid door action
            if (location == null || action == null || who == null || action.Length <= 1 || Game1.eventUp)
                return false;

            // Only handle normal Door actions here
            if (action[0] != "Door")
                return false;

            // Check each NPC listed in the door action
            for (int i = 1; i < action.Length; i++)
            {
                string npcName = action[i];
                string doorUnlockMail = "doorUnlock" + npcName;

                // If the player already has the friendship or vanilla unlock mail, the door is not locked
                if (who.getFriendshipHeartLevelForNPC(npcName) >= 2 || who.mailReceived.Contains(doorUnlockMail))
                    return false;

                // Sebastian has special green rain handling in year one
                if (npcName == "Sebastian" && location.IsGreenRainingHere() && Game1.year == 1)
                    return false;
            }

            // If none of the listed NPC checks passed, the door is locked
            return true;
        }

        public static bool IsConditionalDoorActionLocked(GameLocation location, string[] action)
        {
            // Make sure this is a valid conditional door action
            if (location == null || action == null || action.Length <= 1 || Game1.eventUp)
                return false;

            // Only handle ConditionalDoor actions here
            if (action[0] != "ConditionalDoor")
                return false;

            // If the GameStateQuery fails, this conditional door is locked
            return !GameStateQuery.CheckConditions(ArgUtility.UnsplitQuoteAware(action, ' ', 1));
        }

        public static void OpenPhysicalDoor(GameLocation location, Location tileLocation)
        {
            // Make sure there is a location to open the door in
            if (location == null)
                return;

            // Give controller feedback and open the map door
            Rumble.rumble(0.1f, 100f);
            location.openDoor(tileLocation, playSound: true);
        }

        public static void OpenWarpDoor(GameLocation location, Farmer farmer, Point tile, string locationName)
        {
            // Make sure there is a location and farmer before warping
            if (location == null || farmer == null)
                return;

            // Stop the farmer, play feedback, and warp to the destination
            Rumble.rumble(0.15f, 200f);
            farmer.completelyStopAnimatingOrDoingAction();
            location.playSound("doorClose", farmer.Tile);
            Game1.warpFarmer(locationName, tile.X, tile.Y, flip: false);
        }

        private static bool HasRequiredFriendship(Farmer farmer, string npcName, int minFriendship)
        {
            // If the NPC name is missing, the friendship requirement cannot pass
            if (farmer == null || string.IsNullOrWhiteSpace(npcName))
                return false;

            // Vanilla LockedDoorWarp friendship uses friendship points, not hearts
            return farmer.friendshipData.TryGetValue(npcName, out Friendship friendship) && friendship.Points >= minFriendship;
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.lockedDoorWarp))]
    class LockedDoorWarp_Patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        GameLocation __instance, Point tile, string locationName, int openTime, int closeTime, string npcName, int minFriendship)
        {
            try
            {
                Farmer farmer = Game1.player;

                // Only the local player should process their own lockpicking
                if (farmer == null || !farmer.IsLocalPlayer)
                    return true;

                // If vanilla would not block this warp, let vanilla handle it normally
                if (!LockpickDoorUtility.IsLockedDoorWarp(__instance, locationName, openTime, closeTime, npcName, minFriendship))
                    return true;

                // Build a daily memory key for this specific locked warp
                string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, "LockedDoorWarp", $"{locationName}|{tile.X},{tile.Y}|{openTime}|{closeTime}|{npcName}|{minFriendship}");

                // Try to use a lockpick or reuse remembered access for this door
                if (!LockpickManager.TryUseLockpick(farmer, $"LockedDoorWarp to {locationName}", doorMemoryKey))
                    return true;

                // If lockpicking worked, manually perform the warp and skip vanilla's locked-door handling
                LockpickDoorUtility.OpenWarpDoor(__instance, farmer, tile, locationName);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed in {nameof(LockedDoorWarp_Patch)}:\n{ex}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new Type[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
    class PerformAction_LockedDoor_Patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        GameLocation __instance, string[] action, Farmer who, Location tileLocation, ref bool __result)
        {
            try
            {
                // Make sure this is a valid local action
                if (action == null || action.Length == 0 || who == null || !who.IsLocalPlayer)
                    return true;

                // Handle clicked/interacted NPC friendship doors
                if (action[0] == "Door" && LockpickDoorUtility.IsDoorActionLocked(__instance, action, who))
                {
                    // Build a daily memory key for this specific physical door
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    // Try to use a lockpick or reuse remembered access for this door
                    if (!LockpickManager.TryUseLockpick(who, "Door friendship lock", doorMemoryKey))
                        return true;

                    // Face the door and open it manually
                    who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);

                    // Tell vanilla the action succeeded and skip the locked-door message
                    __result = true;
                    return false;
                }

                // Handle clicked/interacted conditional doors
                if (action[0] == "ConditionalDoor" && LockpickDoorUtility.IsConditionalDoorActionLocked(__instance, action))
                {
                    // Build a daily memory key for this specific conditional door
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    // Try to use a lockpick or reuse remembered access for this door
                    if (!LockpickManager.TryUseLockpick(who, "ConditionalDoor lock", doorMemoryKey))
                        return true;

                    // Face the door and open it manually
                    who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);

                    // Tell vanilla the action succeeded and skip the locked-door message
                    __result = true;
                    return false;
                }

                // If this was not a locked door we handle, let vanilla continue
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed in {nameof(PerformAction_LockedDoor_Patch)}:\n{ex}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performTouchAction), new Type[] { typeof(string[]), typeof(Vector2) })]
    class PerformTouchAction_LockedDoor_Patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        GameLocation __instance, string[] action, Vector2 playerStandingPosition)
        {
            try
            {
                // Make sure this is a valid local touch action
                if (action == null || action.Length == 0 || Game1.player == null || !Game1.player.IsLocalPlayer)
                    return true;

                // Respect vanilla's touch-action ignore state
                if (__instance.IgnoreTouchActions())
                    return true;

                // Convert the player's standing position into a tile location for the door
                Location tileLocation = new Location((int)playerStandingPosition.X, (int)playerStandingPosition.Y);

                // Handle walking into NPC friendship doors
                if (action[0] == "Door" && LockpickDoorUtility.IsDoorActionLocked(__instance, action, Game1.player))
                {
                    // Build a daily memory key for this specific touch-action door
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    // Try to use a lockpick or reuse remembered access for this door
                    if (!LockpickManager.TryUseLockpick(Game1.player, "TouchAction Door friendship lock", doorMemoryKey))
                        return true;

                    // Open the physical door and skip vanilla's pushback/locked message
                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);
                    return false;
                }

                // Handle walking into conditional doors
                if (action[0] == "ConditionalDoor" && LockpickDoorUtility.IsConditionalDoorActionLocked(__instance, action))
                {
                    // Build a daily memory key for this specific touch-action conditional door
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    // Try to use a lockpick or reuse remembered access for this door
                    if (!LockpickManager.TryUseLockpick(Game1.player, "TouchAction ConditionalDoor lock", doorMemoryKey))
                        return true;

                    // Open the physical door and skip vanilla's pushback/locked message
                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);
                    return false;
                }

                // If this was not a locked touch-action door we handle, let vanilla continue
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed in {nameof(PerformTouchAction_LockedDoor_Patch)}:\n{ex}");
                return true;
            }
        }
    }
}
