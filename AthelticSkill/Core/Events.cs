using System;
using AthleticSkill.Objects;
using Force.DeepCloner;
using MoonShared;
using MoonShared.Attributes;
using SpaceCore;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
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
        private static bool CacheToggleSprint = false; // Cached sprint toggle mode setting
        private static uint CacheSprintingExpEvent = 0; // How often to award EXP from sprinting
        private static int CacheExpFromSprinting = 0;  // Amount of EXP awarded per sprint interval
        private static int CachedAthleticLevel = 0;    // Cached athletic skill level for this player
        private static float CacheMinimumEnergyToSprint = 20; // Minimum stamina required to sprint

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Determine whether alternative profession system is active
            if (ModEntry.IsWoLLoaded || ModEntry.Config.AlternativeStrongmanProfession)
            {
                ModEntry.UseAltProfession = true;
            }

            Log.Trace("Athletics: Trying to Register skill.");

            // Register the Athletics skill with SpaceCore
            SpaceCore.Skills.RegisterSkill(new Athletic_Skill());

            // Legacy stamina hook (currently unused)
            // var field = ModEntry.Instance.Helper.Reflection.GetField<NetFloat>(Game1.player, "netStamina");
            // field.GetValue().fieldChangeEvent += (field, oldValue, newValue) => OnStaminaUse(oldValue, newValue);

            // Subscribe to SpaceCore item-eaten events for custom "Nauseated" tag handling
            SpaceEvents.OnItemEaten += OnItemEaten;

            // Initialize I18N string cache for sprinting display names/descriptions
            I18NCache.Initialize();
        }

        // Internal cache for localized strings used in buffs
        private static class I18NCache
        {
            public static string SprintDisplayName { get; private set; }           // Default sprint buff display name
            public static string SprintDescription { get; private set; }           // Default sprint buff description
            public static string SprintDisplayName_Gridball { get; private set; }  // Sprint display name for Linebacker
            public static string SprintDescription_Gridball { get; private set; }  // Sprint description for Linebacker

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
        }

        // Handles player eating an item
        private static void OnItemEaten(object sender, EventArgs args)
        {
            if (sender is not Farmer player)
                return;

            // Apply Nauseated debuff if the eaten item has the custom context tag
            if (player.itemToEat?.HasContextTag("moonslime.Athletic.Nauseated") == true)
            {
                player.applyBuff("25");
            }
        }

        // Placeholder method for detecting stamina use (currently unused)
        private static void OnStaminaUse(float oldValue, float newValue)
        {
        }

        [SEvent.ButtonsChanged]
        public static void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            // --- TOGGLE SPRINT MODE ---
            if (CacheToggleSprint && ModEntry.Config.Key_Cast.JustPressed())
            {
                // Flip sprint state between true/false
                farmer.modData.SetBool(SprintingOn, !farmer.modData.GetBool(SprintingOn));
                return;
            }

            // --- HOLD-TO-SPRINT MODE ---
            if (!CacheToggleSprint)
            {
                // Active only while key is held down
                farmer.modData.SetBool(SprintingOn, ModEntry.Config.Key_Cast.IsDown());
            }
        }

        [SEvent.UpdateTicked]
        public static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(TimeChecker) || !Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            // Don't sprint if conditions are invalid
            if (!CanSprint(farmer))
                return;

            // Reapply the sprint buff so it doesn't expire
            ApplySprintBuff(farmer);

            // Calculate stamina drain factoring in athletic level and profession
            float levelModifier = CachedAthleticLevel + (HasMarathoner ? ProfBonus : 0f);
            float scale = 1f - MathF.Min(levelModifier * LevelScaleFactor, 0.9f);
            float newDrain = BaseDrain * scale;
            float energyDrainPerSecond = Math.Max(newDrain, MinDrain);

            // Apply stamina drain per tick
            farmer.stamina -= energyDrainPerSecond * StaminaDivisor;

            // Award EXP at configured intervals
            if (e.IsMultipleOf(TimeChecker * CacheSprintingExpEvent))
                Utilities.AddEXP(farmer, CacheExpFromSprinting);
        }

        // Checks whether the player can currently sprint
        public static bool CanSprint(Farmer farmer)
        {
            if (farmer.isRidingHorse()) return false;               // Can't sprint on horse
            if (farmer.exhausted.Value) return false;               // Can't sprint while exhausted
            if (!farmer.isMoving()) return false;                   // Must be moving
            if (!Context.IsPlayerFree) return false;                // No menus or cutscenes
            if (!farmer.modData.GetBool(SprintingOn)) return false; // Sprint key not active
            return farmer.Stamina > CacheMinimumEnergyToSprint;     // Must have enough energy
        }

        // Applies or refreshes the sprint buff
        public static void ApplySprintBuff(Farmer farmer)
        {
            if (farmer.buffs.AppliedBuffs.TryGetValue("Athletics:sprinting", out var existing))
            {
                // Refresh duration if already applied
                existing.millisecondsDuration = SprintBuffDurationMs;
            }
            else
            {
                // Apply cached buff
                CachedSprintBuff.millisecondsDuration = SprintBuffDurationMs;
                farmer.applyBuff(CachedSprintBuff.DeepClone());
            }
        }

        [SEvent.OneSecondUpdateTicked]
        public static void OnOneSecondUpdateTicked_professions(object sender, OneSecondUpdateTickedEventArgs e)
        {
            // Run every 5 seconds
            if (!e.IsMultipleOf(300) || !Context.IsWorldReady)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            // Refresh cached athletic level and update sprint buff if changed
            int currentLevel = Utilities.GetLevel(farmer);
            if (currentLevel != CachedAthleticLevel && HasLinebacker)
            {
                CachedAthleticLevel = currentLevel;
                UpdateSprintBuff(farmer);
            }

            if (!farmer.CanMove)
                return;

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
                CachedSprintBuff.effects.Defense.Value = CachedAthleticLevel;
        }
    }
}
