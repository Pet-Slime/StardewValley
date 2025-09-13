using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
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
            BirbCore.Attributes.Log.Warn("Athletics: Trying to Register skill.");
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

                Log.Warn($"Sprint toggled: {modData[SpringtingOn]}");
                return;
            }

            // Hold-to-sprint behavior
            if (!ModEntry.Config.ToggleSprint)
            {
                modData[SpringtingOn] = ModEntry.Config.Key_Cast.IsDown() ? "true" : "false";
            }
        }

        public bool CanSprint(Farmer player)
        {
            if (player.isRidingHorse())
                return false;

            if (player.exhausted.Value)
                return false;

            if (!player.isMoving())
                return false;

            return player.Stamina >= ModEntry.Config.MinimumEnergyToSprint;
        }

        private void ApplySprintBuff(Farmer player)
        {
            if (CanSprint(player))
            {
                // Create the buff
                Buff _sprintBuff = new(
                    id: "Athletics:sprinting",
                    displayName: ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.displayName"),
                    description: ModEntry.Instance.I18N.Get("moonslime.Athletics.sprinting.description"),
                    iconTexture: ModEntry.Assets.IconA,
                    iconSheetIndex: 0,
                    duration: 1000, 
                    effects: new BuffEffects()
                    {
                        Speed = { 2 }
                    }
                );

                Buff buff = Game1.buffsDisplay.GetSortedBuffs().Where(x => x.id == "Athletics:sprinting").FirstOrDefault();
                if (buff is not null)
                {
                    buff.millisecondsDuration = 1000;
                }
                else
                {
                    _sprintBuff.millisecondsDuration = 1000;
                    player.applyBuff(_sprintBuff);
                }
            }
        }

        [SEvent.OneSecondUpdateTicked]
        private void OnUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Farmer player = Game1.player;

            if (player.modData.TryGetValue(SpringtingOn, out string areTheySprinting) && areTheySprinting == "true")
                ApplySprintBuff(player);

            if (!player.hasBuff("Athletics:sprinting") || !player.isMoving() || !Context.IsPlayerFree || Game1.isFestival())
                return;

            float energyDrainPerSecond = 20f;

            energyDrainPerSecond += Math.Max(20 - Utilities.GetLevel(player), 1);

            player.Stamina -= energyDrainPerSecond / 60;
        }

    }
}
