using System;
using System.Linq;
using AthleticSkill.Objects;
using MoonShared.Attributes;
using MoonShared;
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
        private static bool HasMarathoner = false;
        private static bool HasLinebacker = false;

        private static bool HasHealthRegen = false;
        private static bool HasEnergyRegen = false;
        private static Buff CachedSprintBuff;

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
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            // Cache profession since they usually only change during night events
            HasMarathoner = player.HasCustomProfession(Athletic_Skill.Athletic10b2);
            HasLinebacker = player.HasCustomProfession(Athletic_Skill.Athletic10a2);
            HasHealthRegen = player.HasCustomProfession(Athletic_Skill.Athletic5a);
            HasEnergyRegen = player.HasCustomProfession(Athletic_Skill.Athletic5b);

            // Create the Buff once
            CachedSprintBuff = new Buff(
                    id: "Athletics:sprinting",
                    displayName: HasLinebacker ? I18NCache.SprintDisplayName_Gridball : I18NCache.SprintDisplayName,
                    description: HasLinebacker ? I18NCache.SprintDescription_Gridball : I18NCache.SprintDescription,
                    iconTexture: HasLinebacker ? ModEntry.Assets.SprintingIcon2 : ModEntry.Assets.SprintingIcon1,
                    iconSheetIndex: 0,
                    duration: ((int)(TimeChecker * 20)),
                    effects: new BuffEffects()
                    {
                        Speed = { HasMarathoner ? ModEntry.Config.SprintSpeed + 1 : ModEntry.Config.SprintSpeed },
                        Defense = { HasMarathoner ? Utilities.GetLevel(player) : 0 }
                    }
                );

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
        public void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            // Don’t run if player can’t move or game isn’t ready
            if (!Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            var modData = farmer.modData;

            // --- TOGGLE MODE ---
            if (ModEntry.Config.ToggleSprint && ModEntry.Config.Key_Cast.JustPressed())
            {
                // Flip sprint state between true and false
                modData.SetBool(SprintingOn, !modData.GetBool(SprintingOn));

                Log.Trace($"Sprint toggled: {modData[SprintingOn]}");
                return;
            }

            // --- HOLD-TO-SPRINT MODE ---
            if (!ModEntry.Config.ToggleSprint)
            {
                // Active only while the key is held down
                farmer.modData.SetBool(SprintingOn, ModEntry.Config.Key_Cast.IsDown());
            }
        }

        // SPRINT TICK HANDLER

        // Constants governing stamina drain and timing
        private const uint TimeChecker = 15;           // Number of ticks between checks (15 ticks = ~0.25s)
        private const float BaseDrain = 20f;           // Baseline stamina drain rate
        private const float ProfBonus = 5f;            // Flat bonus for profession
        private const float MinDrain = 1f;             // Lower bound for stamina drain
        private const float StaminaDivisor = 0.0375f;  // Converts per-second drain to per-tick adjustment (≈ TimeChecker / 400)

        [SEvent.UpdateTicked]
        public void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(TimeChecker) || !Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            if (!CanSprint(farmer))
                return;


            // Maintain / reapply sprint buff
            ApplySprintBuff(farmer);

            // Level modifier for drain
            float levelModifier = Utilities.GetLevel(farmer) + (HasMarathoner ? ProfBonus : 0f);
            float newDrain = BaseDrain * (BaseDrain / (BaseDrain + levelModifier));
            float energyDrainPerSecond = Math.Max(newDrain, MinDrain);

            farmer.stamina -= energyDrainPerSecond * StaminaDivisor;

            uint sprintgEvent = (uint)ModEntry.Config.SprintingExpEvent;

            if (e.IsMultipleOf(TimeChecker * sprintgEvent))
                Utilities.AddEXP(farmer, ModEntry.Config.ExpFromSprinting);
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
            return farmer.Stamina > ModEntry.Config.MinimumEnergyToSprint;
        }

        // BUFF MANAGEMENT
        public static void ApplySprintBuff(Farmer farmer)
        {
            int sprintspeed = ModEntry.Config.SprintSpeed;

            // Check for existing sprint buff
            Buff existing = null;
            foreach (var buff in farmer.buffs.AppliedBuffs.Values)
            {
                if (buff.id == "Athletics:sprinting")
                {
                    existing = buff;
                    break;
                }
            }

            if (existing is not null)
            {
                // Refresh duration to prevent expiration while sprinting
                existing.millisecondsDuration = ((int)(TimeChecker * 20));
            }
            else
            {
                CachedSprintBuff.millisecondsDuration = ((int)(TimeChecker * 20));
                // Apply new sprint buff
                farmer.applyBuff(CachedSprintBuff);
            }
        }

        // PROFESSION EFFECTS 
        [SEvent.OneSecondUpdateTicked]
        public void OnOneSecondUpdateTicked_professions(object sender, OneSecondUpdateTickedEventArgs e)
        {
            // Run every 5 seconds (60 * 5 = 300 ticks)
            if (!e.IsMultipleOf(300) || !Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            // Restore amount scales with half the athletic level
            int amount = Utilities.GetLevel(farmer) >> 1; // same as dividing by 2


            // Profession 1: Bodybuilder -> restores health
            if (HasHealthRegen)
                farmer.health = Restore(farmer.health, farmer.maxHealth, amount);

            // Profession 2: Runner -> restores stamina
            if (HasEnergyRegen)
                farmer.stamina = Restore((int)farmer.stamina, farmer.MaxStamina, amount);
        }

        // Helper: safely restore HP or stamina, capped at max
        public static int Restore(int current, int max, int amount)
        {
            return Math.Min(current + amount, max);
        }
    }
}
