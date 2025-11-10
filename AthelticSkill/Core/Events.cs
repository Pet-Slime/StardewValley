using System;
using AthleticSkill.Objects;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.Attributes;
using SpaceCore;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using xTile.Dimensions;
using xTile.Tiles;
using Log = MoonShared.Attributes.Log;

namespace AthleticSkill.Core
{
    [SEvent]
    public class Events
    {
        // Key used to track whether sprinting is active in Farmer.modData
        private static readonly string SprintingOn = "moonslime.AthelticSkill.sprinting";

        // SPRINT TICK CONSTANTS
        private const uint TimeChecker = 15;           // Number of ticks between sprint updates (~0.25s)
        private const float BaseDrain = 20f;           // Base stamina drain per tick while sprinting
        private const float ProfBonus = 5f;            // Flat stamina drain reduction bonus from Marathoner profession
        private const float MinDrain = 1f;             // Minimum stamina drain per tick
        private const float StaminaDivisor = 0.0375f;  // Converts per-second drain to per-tick adjustment
        private const int SprintBuffDurationMs = (int)(TimeChecker * 20); // Buff duration in milliseconds (per tick)
        private const float LevelScaleFactor = 0.02f;  // Scaling factor for reducing drain per athletic level

        // Cached profession flags (set on DayStarted)
        private static bool HasMarathoner = false;     // True if player has the Marathoner profession
        private static bool HasLinebacker = false;     // True if player has the Linebacker profession
        private static bool HasHealthRegen = false;    // True if player has Bodybuilder profession (health regen)
        private static bool HasEnergyRegen = false;    // True if player has Runner profession (stamina regen)

        // Buff and sprint-related cached values
        private static Buff CachedSprintBuff;          // Reference to the sprint Buff object, reused each tick
        private static Buff BackupSprintBuff;          // A backup of the sprint buff just in case the Cached sprint buff some how vanishes
        private static bool CacheToggleSprint = false; // Cached sprint toggle mode setting
        private static uint CacheSprintingExpEvent = 0; // How often to award EXP from sprinting
        private static int CacheExpFromSprinting = 0;  // Amount of EXP awarded per sprint interval
        private static int CachedAthleticLevel = 0;    // Cached athletic skill level for this player
        private static float CacheMinimumEnergyToSprint = 20; // Minimum stamina required to sprint

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Check whether WoL (Walks of Life) or alternative profession settings are active
            // This toggles an internal flag to use the alternative profession system.
            if (ModEntry.IsWoLLoaded || ModEntry.Config.AlternativeStrongmanProfession)
            {
                ModEntry.UseAltProfession = true;
            }

            Log.Trace("Athletics: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Athletic_Skill());

            // Legacy stamina hook (commented out): was meant to trigger OnStaminaUse()
            // when the stamina value changes. Currently unused.
            // var field = ModEntry.Instance.Helper.Reflection.GetField<NetFloat>(Game1.player, "netStamina");
            // field.GetValue().fieldChangeEvent += (field, oldValue, newValue) => OnStaminaUse(oldValue, newValue);

            // Subscribe to SpaceCore’s OnItemEaten event to handle “Nauseated” food items.
            SpaceEvents.OnItemEaten += OnItemEaten;

            // String cache for optimizations

            I18NCache.Initialize();
        }

        private static class I18NCache
        {
            public static string SprintDisplayName { get; private set; }
            public static string SprintDescription { get; private set; }
            public static string SprintDisplayName_Gridball { get; private set; }
            public static string SprintDescription_Gridball { get; private set; }

            public static void Initialize()
            {
                SprintDisplayName = ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.displayName");
                SprintDescription = ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.description");
                SprintDisplayName_Gridball = ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.displayName_Gridball");
                SprintDescription_Gridball = ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.description_Gridball");
            }
        }

        [SEvent.DayStarted]
        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            // Cache player professions that affect sprinting and regeneration
            HasMarathoner = player.HasCustomProfession(Athletic_Skill.Athletic10b2);
            HasLinebacker = player.HasCustomProfession(Athletic_Skill.Athletic10a2);
            HasHealthRegen = player.HasCustomProfession(Athletic_Skill.Athletic5a);
            HasEnergyRegen = player.HasCustomProfession(Athletic_Skill.Athletic5b);

            // Cache sprint-related config values
            CacheToggleSprint = ModEntry.Config.ToggleSprint;
            CacheMinimumEnergyToSprint = ModEntry.Config.MinimumEnergyToSprint;
            CacheExpFromSprinting = ModEntry.Config.ExpFromSprinting;
            CacheSprintingExpEvent = (uint)ModEntry.Config.SprintingExpEvent;
            CachedAthleticLevel = Utilities.GetLevel(player);

            // Create the sprint Buff object once and reuse it each tick
            CachedSprintBuff = new Buff(
                id: "Athletics:sprinting",
                displayName: HasLinebacker ? I18NCache.SprintDisplayName_Gridball : I18NCache.SprintDisplayName,
                description: HasLinebacker ? I18NCache.SprintDescription_Gridball : I18NCache.SprintDescription,
                iconTexture: HasLinebacker ? ModEntry.Assets.SprintingIcon2 : ModEntry.Assets.SprintingIcon1,
                iconSheetIndex: 0,
                duration: SprintBuffDurationMs,
                effects: new BuffEffects()
                {
                    Speed = { HasMarathoner ? ModEntry.Config.SprintSpeed + 1 : ModEntry.Config.SprintSpeed },
                    Defense = { HasLinebacker ? CachedAthleticLevel : 0 }
                }
            );

            CachedSprintBuff.visible = ModEntry.Config.BuffIcon;

            // Make a backup of the sprint buff object just in case something happens
            BackupSprintBuff = CachedSprintBuff;
        }

        // ITEM EVENT HANDLER
        private static void OnItemEaten(object sender, EventArgs args)
        {
            // This event fires when the player eats any item.
            if (sender is not Farmer player)
                return;

            // If the eaten item has the "Nauseated" context tag,
            // apply vanilla buff #25 (the Nauseated debuff).
            if (player.itemToEat?.HasContextTag("moonslime.Athletic.Nauseated") == true)
            {
                player.applyBuff("25");
            }
        }

        // Placeholder for possible future stamina-use detection logic.
        private static void OnStaminaUse(float oldValue, float newValue)
        {
        }

        // INPUT HANDLER 
        [SEvent.ButtonsChanged]
        public static void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            // Don’t run if player can’t move or game isn’t ready
            if (!Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            var modData = farmer.modData;

            // --- TOGGLE MODE ---
            if (CacheToggleSprint && ModEntry.Config.Key_Cast.JustPressed())
            {
                // Flip sprint state between true and false
                modData.SetBool(SprintingOn, !modData.GetBool(SprintingOn));
                return;
            }

            // --- HOLD-TO-SPRINT MODE ---
            if (!CacheToggleSprint)
            {
                // Active only while the key is held down
                farmer.modData.SetBool(SprintingOn, ModEntry.Config.Key_Cast.IsDown());
            }
        }

        // SPRINT TICK HANDLER
        [SEvent.UpdateTicked]
        public static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(TimeChecker) || !Context.CanPlayerMove)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            if (!CanSprint(farmer))
                return;


            // Maintain / reapply sprint buff
            ApplySprintBuff(farmer);

            // Calculate stamina drain factoring in athletic level and profession
            float levelModifier = CachedAthleticLevel + (HasMarathoner ? ProfBonus : 0f);
            float scale = 1f - MathF.Min(levelModifier * LevelScaleFactor, 0.9f);
            float newDrain = BaseDrain * scale;
            float energyDrainPerSecond = Math.Max(newDrain, MinDrain);

            farmer.stamina -= energyDrainPerSecond * StaminaDivisor;


            if (e.IsMultipleOf(TimeChecker * CacheSprintingExpEvent))
                Utilities.AddEXP(farmer, CacheExpFromSprinting);
        }

        // SPRINT VALIDATION
        public static bool CanSprint(Farmer farmer)
        {
            // These checks ensure sprinting only works in valid conditions
            if (farmer.isRidingHorse()) return false;               // Can't sprint on horse
            if (farmer.exhausted.Value) return false;               // Can't sprint when exhausted
            if (!farmer.isMoving()) return false;                   // Must be moving
            if (!Context.IsPlayerFree) return false;                // No menus or cutscenes
            if (!farmer.modData.GetBool(SprintingOn)) return false; // Must have sprint flag active

            // Ensure enough energy remains
            return farmer.Stamina > CacheMinimumEnergyToSprint;
        }

        // BUFF MANAGEMENT
        public static void ApplySprintBuff(Farmer farmer)
        {
            // if the cache sprint buff is somehow null, reset it and then apply
            if (CachedSprintBuff == null)
            {
                CachedSprintBuff = BackupSprintBuff;
                CachedSprintBuff.millisecondsDuration = SprintBuffDurationMs;
                farmer.applyBuff(CachedSprintBuff);
                Game1.Multiplayer.broadcastSprites(farmer.currentLocation,
                    new TemporaryAnimatedSprite(5,
                    farmer.Position,
                    Color.Brown,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    farmer.Position.Y / 10000f));

            } else
            {
                CachedSprintBuff.millisecondsDuration = SprintBuffDurationMs;
                farmer.applyBuff(CachedSprintBuff);
                Game1.Multiplayer.broadcastSprites(farmer.currentLocation,
                    new TemporaryAnimatedSprite(5,
                    farmer.Position,
                    Color.White,
                    3,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    farmer.Position.Y / 10000f));
            }
        }

        // PROFESSION EFFECTS 
        [SEvent.OneSecondUpdateTicked]
        public static void OnOneSecondUpdateTicked_professions(object sender, OneSecondUpdateTickedEventArgs e)
        {
            // Run every 5 seconds
            if (!e.IsMultipleOf(300) || !Context.IsPlayerFree)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            // Refresh cached athletic level and update sprint buff if changed
            int currentLevel = Utilities.GetLevel(farmer);
            if (HasLinebacker && currentLevel != CachedAthleticLevel)
            {
                CachedAthleticLevel = currentLevel;
                UpdateSprintBuff(farmer);
            }

            // Amount to restore scales with half the athletic level
            int amount = CachedAthleticLevel >> 1;

            // Restore health for Bodybuilder profession
            if (HasHealthRegen)
                farmer.health = Restore(farmer.health, farmer.maxHealth, amount);

            // Restore stamina for Runner profession
            if (HasEnergyRegen)
                farmer.stamina = Restore((int)farmer.stamina, farmer.MaxStamina, amount);
        }

        // Helper to safely restore health or stamina, capped at max value
        public static int Restore(int current, int max, int amount)
        {
            return Math.Min(current + amount, max);
        }

        // Updates the defense effect of the sprint buff based on Linebacker profession
        public static void UpdateSprintBuff(Farmer farmer)
        {
            // Update the applied buff if active
            if (farmer.buffs.AppliedBuffs.TryGetValue("Athletics:sprinting", out var activeBuff))
                activeBuff.effects.Defense.Value = CachedAthleticLevel;

            // Always update cached template so next application is correct
            if (CachedSprintBuff != null)
            {
                CachedSprintBuff = BackupSprintBuff;
                CachedSprintBuff.effects.Defense.Value = CachedAthleticLevel;
            } else
            {
                CachedSprintBuff.effects.Defense.Value = CachedAthleticLevel;
            }
        }
    }
}
