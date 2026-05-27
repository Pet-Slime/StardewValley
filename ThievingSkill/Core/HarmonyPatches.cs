using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.Attributes;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using xTile.Dimensions;

namespace ThievingSkill.Core
{
    internal static class LockpickManager
    {
        private const string DoorMemoryPrefix = "moonslime.Thieving.LockpickedDoor/";

        private static readonly LockpickInfo[] Lockpicks =
        {
            // Best lockpicks first. If the player is not holding a lockpick, the best available one is used.
            new LockpickInfo("moonslime.Thieving.lockpick_iridium", 0.20),
            new LockpickInfo("moonslime.Thieving.lockpick_gold", 0.40),
            new LockpickInfo("moonslime.Thieving.lockpick_iron", 0.60),
            new LockpickInfo("moonslime.Thieving.lockpick_copper", 0.80)
        };

        public static bool TryUseLockpick(Farmer farmer, string lockContext, string doorMemoryKey)
        {
            if (farmer == null || !farmer.IsLocalPlayer)
                return false;

            if (HasPickedDoorToday(farmer, doorMemoryKey))
            {
                Log.Trace($"Thieving skill: Reusing remembered lockpick access for {lockContext}.");
                return true;
            }

            if (!TryFindLockpick(farmer, out Item lockpick, out LockpickInfo lockpickInfo))
            {
                Game1.addHUDMessage(new HUDMessage("You need a lockpick.", 3));
                return false;
            }

            Log.Trace($"Thieving skill: Used {lockpickInfo.DefId} to bypass {lockContext}.");

            RememberPickedDoor(farmer, doorMemoryKey);
            Utilities.AddEXP(farmer, 5);

            Game1.playSound("stoneCrack");

            double adjustedBreakChance = GetAdjustedBreakChance(farmer, lockpickInfo);
            if (Game1.random.NextDouble() < adjustedBreakChance)
            {
                RemoveOneLockpick(farmer, lockpick);
                Game1.playSound("clank");
                Game1.addHUDMessage(new HUDMessage("Your lockpick broke.", 3));
            }

            return true;
        }

        public static void ClearDailyDoorMemory(Farmer farmer)
        {
            if (farmer == null)
                return;

            List<string> keysToRemove = farmer.modDataForSerialization.Keys.Where(key => key.StartsWith(DoorMemoryPrefix)).ToList();

            foreach (string key in keysToRemove)
                farmer.modDataForSerialization.Remove(key);
        }

        public static string BuildDoorMemoryKey(GameLocation location, string lockType, string detail)
        {
            string locationName = location?.NameOrUniqueName ?? "Unknown";
            return $"{locationName}|{lockType}|{detail}";
        }

        public static string BuildDoorMemoryKey(GameLocation location, string[] action, Location tileLocation)
        {
            string locationName = location?.NameOrUniqueName ?? "Unknown";
            string actionName = ArgUtility.Get(action, 0, "Unknown");
            string fullAction = action != null ? string.Join(" ", action) : "";
            return $"{locationName}|{actionName}|{tileLocation.X},{tileLocation.Y}|{fullAction}";
        }

        private static bool HasPickedDoorToday(Farmer farmer, string doorMemoryKey)
        {
            if (farmer == null || string.IsNullOrWhiteSpace(doorMemoryKey))
                return false;

            string key = DoorMemoryPrefix + doorMemoryKey;

            return farmer.modDataForSerialization.TryGetValue(key, out string day) && day == Game1.Date.TotalDays.ToString();
        }

        private static void RememberPickedDoor(Farmer farmer, string doorMemoryKey)
        {
            if (farmer == null || string.IsNullOrWhiteSpace(doorMemoryKey))
                return;

            string key = DoorMemoryPrefix + doorMemoryKey;
            farmer.modDataForSerialization[key] = Game1.Date.TotalDays.ToString();
        }

        private static double GetAdjustedBreakChance(Farmer farmer, LockpickInfo lockpickInfo)
        {
            int level = Utilities.GetLevel(farmer);
            double levelProtection = level * 0.01;
            double adjustedBreakChance = lockpickInfo.BreakChance - levelProtection;

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
            if (item == null)
                return false;

            return item.ItemId == defId || item.QualifiedItemId == "(O)" + defId;
        }

        private static void RemoveOneLockpick(Farmer farmer, Item lockpick)
        {
            if (lockpick == null)
                return;

            lockpick.Stack--;

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
            if (location == null || Game1.player == null)
                return false;

            bool townKeyWorks = Game1.player.HasTownKey;

            if (townKeyWorks && !location.InValleyContext())
                townKeyWorks = false;

            if (townKeyWorks && location is BeachNightMarket && locationName != "FishShop")
                townKeyWorks = false;

            // The vanilla method checks festival closure before normal hours.
            if (GameLocation.AreStoresClosedForFestival() && location.InValleyContext())
                return true;

            // The vanilla method lets the Town Key bypass Pierre's Wednesday closure.
            if (locationName == "SeedShop" && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Wed") && !Utility.HasAnyPlayerSeenEvent("191393") && !townKeyWorks)
                return true;

            if (locationName == "FishShop" && Game1.player.mailReceived.Contains("willyHours"))
                openTime = 800;

            bool timePassed = townKeyWorks || (Game1.timeOfDay >= openTime && Game1.timeOfDay < closeTime);
            bool friendshipPassed = minFriendship <= 0 || location.IsWinterHere() || HasRequiredFriendship(Game1.player, npcName, minFriendship);

            // Vanilla lets most doors open during the year-one green rain.
            if (location.IsGreenRainingHere() && Game1.year == 1 && location is not Beach && location is not Forest && locationName != "AdventureGuild")
                return false;

            return !timePassed || !friendshipPassed;
        }

        public static bool IsDoorActionLocked(GameLocation location, string[] action, Farmer who)
        {
            if (location == null || action == null || who == null || action.Length <= 1 || Game1.eventUp)
                return false;

            if (action[0] != "Door")
                return false;

            for (int i = 1; i < action.Length; i++)
            {
                string npcName = action[i];
                string doorUnlockMail = "doorUnlock" + npcName;

                if (who.getFriendshipHeartLevelForNPC(npcName) >= 2 || who.mailReceived.Contains(doorUnlockMail))
                    return false;

                if (npcName == "Sebastian" && location.IsGreenRainingHere() && Game1.year == 1)
                    return false;
            }

            return true;
        }

        public static bool IsConditionalDoorActionLocked(GameLocation location, string[] action)
        {
            if (location == null || action == null || action.Length <= 1 || Game1.eventUp)
                return false;

            if (action[0] != "ConditionalDoor")
                return false;

            return !GameStateQuery.CheckConditions(ArgUtility.UnsplitQuoteAware(action, ' ', 1));
        }

        public static void OpenPhysicalDoor(GameLocation location, Location tileLocation)
        {
            if (location == null)
                return;

            Rumble.rumble(0.1f, 100f);
            location.openDoor(tileLocation, playSound: true);
        }

        public static void OpenWarpDoor(GameLocation location, Farmer farmer, Point tile, string locationName)
        {
            if (location == null || farmer == null)
                return;

            Rumble.rumble(0.15f, 200f);
            farmer.completelyStopAnimatingOrDoingAction();
            location.playSound("doorClose", farmer.Tile);
            Game1.warpFarmer(locationName, tile.X, tile.Y, flip: false);
        }

        private static bool HasRequiredFriendship(Farmer farmer, string npcName, int minFriendship)
        {
            if (farmer == null || string.IsNullOrWhiteSpace(npcName))
                return false;

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

                if (farmer == null || !farmer.IsLocalPlayer)
                    return true;

                if (!LockpickDoorUtility.IsLockedDoorWarp(__instance, locationName, openTime, closeTime, npcName, minFriendship))
                    return true;

                string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, "LockedDoorWarp", $"{locationName}|{tile.X},{tile.Y}|{openTime}|{closeTime}|{npcName}|{minFriendship}");

                if (!LockpickManager.TryUseLockpick(farmer, $"LockedDoorWarp to {locationName}", doorMemoryKey))
                    return true;

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
                if (action == null || action.Length == 0 || who == null || !who.IsLocalPlayer)
                    return true;

                if (action[0] == "Door" && LockpickDoorUtility.IsDoorActionLocked(__instance, action, who))
                {
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    if (!LockpickManager.TryUseLockpick(who, "Door friendship lock", doorMemoryKey))
                        return true;

                    who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);
                    __result = true;
                    return false;
                }

                if (action[0] == "ConditionalDoor" && LockpickDoorUtility.IsConditionalDoorActionLocked(__instance, action))
                {
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    if (!LockpickManager.TryUseLockpick(who, "ConditionalDoor lock", doorMemoryKey))
                        return true;

                    who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);
                    __result = true;
                    return false;
                }

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
                if (action == null || action.Length == 0 || Game1.player == null || !Game1.player.IsLocalPlayer)
                    return true;

                if (__instance.IgnoreTouchActions())
                    return true;

                Location tileLocation = new Location((int)playerStandingPosition.X, (int)playerStandingPosition.Y);

                if (action[0] == "Door" && LockpickDoorUtility.IsDoorActionLocked(__instance, action, Game1.player))
                {
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    if (!LockpickManager.TryUseLockpick(Game1.player, "TouchAction Door friendship lock", doorMemoryKey))
                        return true;

                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);
                    return false;
                }

                if (action[0] == "ConditionalDoor" && LockpickDoorUtility.IsConditionalDoorActionLocked(__instance, action))
                {
                    string doorMemoryKey = LockpickManager.BuildDoorMemoryKey(__instance, action, tileLocation);

                    if (!LockpickManager.TryUseLockpick(Game1.player, "TouchAction ConditionalDoor lock", doorMemoryKey))
                        return true;

                    LockpickDoorUtility.OpenPhysicalDoor(__instance, tileLocation);
                    return false;
                }

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
