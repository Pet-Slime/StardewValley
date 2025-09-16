using System;
using System.Linq;
using BirbCore.Attributes;
using SpaceCore;
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
        private static string SpringtingOn = "moonslime.AthelticSkill.sprinting";

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            BirbCore.Attributes.Log.Trace("Athletics: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Athletic_Skill());
        }



        [SEvent.ButtonsChanged]
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            var modData = Game1.player.modData;

            // Toggle sprint with key press
            if (ModEntry.Config.ToggleSprint && ModEntry.Config.Key_Cast.JustPressed())
            {
                bool isSprinting = !modData.TryGetValue(SpringtingOn, out string value) || value == "false";
                modData[SpringtingOn] = isSprinting ? "true" : "false";

                Log.Trace($"Sprint toggled: {modData[SpringtingOn]}");
                return;
            }

            // Hold-to-sprint behavior
            if (!ModEntry.Config.ToggleSprint)
            {
                modData[SpringtingOn] = ModEntry.Config.Key_Cast.IsDown() ? "true" : "false";
            }
        }





        [SEvent.UpdateTicked]
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            //Only run this code every 10 ticks, and when the player is actually in the world
            if (!e.IsMultipleOf(10) || !Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            //Get the player
            Farmer player = Game1.player;

            //If the player isn't in a sprintable enviorment, dont run the rest of the code
            if (!CanSprint(player))
                return;

            //Apply the sprint buff
            ApplySprintBuff(player);

            //figure out stamina drain based on current atheltic's level
            float energyDrainPerSecond = Math.Max(25 - Utilities.GetLevel(player), 1);

            if (player.HasCustomProfession(Athletic_Skill.Athletic10b2))
            {
                energyDrainPerSecond /= 2;
            }

            //Adjust player stamina
            player.stamina -= energyDrainPerSecond / 10;
        }

        public bool CanSprint(Farmer player)
        {
            // Early exit for simple blockers
            if (player.isRidingHorse()) return false;
            if (player.exhausted.Value) return false;
            if (!player.isMoving()) return false;
            if (!Context.IsPlayerFree) return false;

            // Must have sprint flag in modData
            if (!player.modData.TryGetValue(SpringtingOn, out string areTheySprinting)
                || areTheySprinting != "true")
                return false;

            return player.Stamina > ModEntry.Config.MinimumEnergyToSprint;
        }

        private void ApplySprintBuff(Farmer player)
        {

            // Create the buff
            Buff sprinting = new(
                id: "Athletics:sprinting",
                displayName: player.HasCustomProfession(Athletic_Skill.Athletic10a2) ? ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.displayName_Gridball") : ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.displayName"),
                description: player.HasCustomProfession(Athletic_Skill.Athletic10a2) ? ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.description_Gridball") : ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.description"),
                iconTexture: player.HasCustomProfession(Athletic_Skill.Athletic10a2) ? ModEntry.Assets.IconA : ModEntry.Assets.IconA,
                iconSheetIndex: 0,
                duration: 300,
                effects: new BuffEffects()
                {
                    //If the player has the marathoner profession, increase speed amount, else it is 1
                    Speed = { player.HasCustomProfession(Athletic_Skill.Athletic10b2) ? 2 : 1 },
                    //If the player has the Linebacker profession, increase defense, else it is 0
                    Defense = { player.HasCustomProfession(Athletic_Skill.Athletic10a2) ? (Utilities.GetLevel(player)) : 0 }
                }
            );

            // Check to see if they have the sprinting buff
            Buff buff = Game1.buffsDisplay.GetSortedBuffs().Where(x => x.id == "Athletics:sprinting").FirstOrDefault();

            if (buff is not null) // If they do just increase the duration
            {
                buff.millisecondsDuration = 300;
            }
            else // if they don't, apply the buff
            {
                player.applyBuff(sprinting);
            }
        }

        [SEvent.OneSecondUpdateTicked]
        private void OnOneSecondUpdateTicked_exp(object sender, OneSecondUpdateTickedEventArgs e)
        {
            //Only run this code every 2 seconds, and when the player is actually in the world
            if (!e.IsMultipleOf(120) || !Context.IsWorldReady)
                return;

            //Get the player
            Farmer player = Game1.player;

            //If the player has the sprinting buff, add exp!
            if (player.hasBuff("Athletics:sprinting"))
                Utilities.AddEXP(player, ModEntry.Config.ExpWhileSprinting);


        }

        [SEvent.OneSecondUpdateTicked]
        private void OnOneSecondUpdateTicked_professions(object sender, OneSecondUpdateTickedEventArgs e)
        {
            //Only run this code every 5 seconds, and when the player is actually in the world
            if (!e.IsMultipleOf(300) || !Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            //Get the player
            Farmer player = Game1.player;


            //Figure out how much to restore based on athletic's level
            int amount = ((int)Math.Floor(Utilities.GetLevel(player) * 0.5));

            //If they have the Bodybuilder profession, restore HP
            if (player.HasCustomProfession(Athletic_Skill.Athletic5a))
                player.health = Restore(player.health, player.maxHealth, amount);

            //If they have the Runner profession, restore energy
            if (player.HasCustomProfession(Athletic_Skill.Athletic5b))
                player.stamina = Restore(((int)Math.Floor(player.stamina)), player.MaxStamina, amount);
        }

        private int Restore(int current, int max, int amount)
        {
            if (current < max)
                current = Math.Min(current + amount, max);

            return current;
        }
    }
}
