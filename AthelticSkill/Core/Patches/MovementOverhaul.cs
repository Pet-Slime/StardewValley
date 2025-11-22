using System;
using HarmonyLib;
using MoonShared.Attributes;
using StardewValley;

namespace AthleticSkill.Core.Patches
{ 
    class MovementOverhaul_jump_patch
    {
        public static void Postfix(Farmer player)
        {
            try
            {
                if (ModEntry.IsMOLoaded)
                {
                    Utilities.AddEXP(player, 3);
                }
            } catch
            {

            }
        }
    }

    class MovementOverhaul_sprint_HandleActiveSprint_patch
    {
        private const string Key = ModEntry.SkillID + ".MovementOverhaul.Sprinting";

        public static void Postfix()
        {
            if (!ModEntry.IsMOLoaded || Game1.player == null)
                return;

            try
            {
                var player = Game1.player;

                if (player.isRidingHorse())
                    return;

                if (!player.modData.ContainsKey(Key))
                    player.modData[Key] = "0";

                if (!int.TryParse(player.modData[Key], out int sprintTime))
                    sprintTime = 0;

                sprintTime++;
                player.modData[Key] = sprintTime.ToString();
            }
            catch (Exception ex)
            {
                Log.Alert($"Failed to increment sprint counter: {ex}");
            }
        }
    }

    class MovementOverhaul_sprint_end_patch
    {
        private const string Key = ModEntry.SkillID + ".MovementOverhaul.Sprinting";

        public static void Postfix()
        {
            if (!ModEntry.IsMOLoaded || Game1.player == null)
                return;

            try
            {
                var player = Game1.player;

                if (!player.modData.ContainsKey(Key))
                    player.modData[Key] = "0";

                if (!int.TryParse(player.modData[Key], out int sprintTime))
                    sprintTime = 0;

                int exp = sprintTime / 120;
                Utilities.AddEXP(player, exp);
                Game1.player.modData[Key] = "0";
            }
            catch (Exception ex)
            {
                Log.Alert($"Failed to increment sprint counter: {ex}");
            }
        }
    }
}
