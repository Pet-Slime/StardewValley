using System;
using System.Linq;
using AthleticSkill.Objects;
using BirbCore.Attributes;
using MoonShared;
using SpaceCore;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using Log = BirbCore.Attributes.Log;

namespace AthleticSkill.Core
{
    [SEvent]
    public class Events
    {
        // Key used to track whether sprinting is active in Farmer.modData
        private static readonly string SprintingOn = "moonslime.AthelticSkill.sprinting";

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

            // Cache profession checks once per tick
            bool hasMarathoner = farmer.HasCustomProfession(Athletic_Skill.Athletic10b2);
            bool hasLinebacker = farmer.HasCustomProfession(Athletic_Skill.Athletic10a2);

            // Maintain / reapply sprint buff
            ApplySprintBuff(farmer, hasMarathoner, hasLinebacker);

            // Level modifier for drain
            float levelModifier = Utilities.GetLevel(farmer) + (hasMarathoner ? ProfBonus : 0f);
            float newDrain = BaseDrain * (BaseDrain / (BaseDrain + levelModifier));
            float energyDrainPerSecond = Math.Max(newDrain, MinDrain);

            farmer.stamina -= energyDrainPerSecond * StaminaDivisor;

            if (e.IsMultipleOf(TimeChecker * ModEntry.Config.SprintingExpEvent))
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
        public static void ApplySprintBuff(Farmer farmer, bool hasMarathoner, bool hasLinebacker)
        {
            int sprintspeed = ModEntry.Config.SprintSpeed;

            // Create or refresh the sprint buff
            Buff sprinting = new(
                id: "Athletics:sprinting",
                displayName: hasLinebacker
                    ? ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.displayName_Gridball")
                    : ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.displayName"),
                description: hasLinebacker
                    ? ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.description_Gridball")
                    : ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.description"),
                iconTexture: hasLinebacker
                    ? ModEntry.Assets.SprintingIcon2
                    : ModEntry.Assets.SprintingIcon1,
                iconSheetIndex: 0,
                duration: ((int)(TimeChecker * 20)), // lasts ~5 seconds (15 ticks * 20 / 60)
                effects: new BuffEffects()
                {
                    // Speed bonus is +3 for Marathoner, +2 otherwise
                    Speed = { hasMarathoner ? sprintspeed+1 : sprintspeed },
                    // Defense bonus scales with level for Linebacker profession
                    Defense = { hasMarathoner ? Utilities.GetLevel(farmer) : 0 }
                }
            );

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
                // Apply new sprint buff
                farmer.applyBuff(sprinting);
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
            int amount = (int)Math.Floor(Utilities.GetLevel(farmer) * 0.5);

            // Profession 1: Bodybuilder -> restores health
            if (farmer.HasCustomProfession(Athletic_Skill.Athletic5a))
                farmer.health = Restore(farmer.health, farmer.maxHealth, amount);

            // Profession 2: Runner -> restores stamina
            if (farmer.HasCustomProfession(Athletic_Skill.Athletic5b))
                farmer.stamina = Restore((int)Math.Floor(farmer.stamina), farmer.MaxStamina, amount);
        }

        // Helper: safely restore HP or stamina, capped at max
        public static int Restore(int current, int max, int amount)
        {
            return Math.Min(current + amount, max);
        }
    }
}
