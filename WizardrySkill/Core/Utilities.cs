using System;
using System.Collections.Generic;
using System.Linq;
using MoonShared.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Spells;
using static SpaceCore.Skills;
using MoonShared;
using SpaceCore;

namespace WizardrySkill.Core
{
    public class AnalyzeEventArgs
    {
        /*********
        ** Accessors
        *********/
        public int TargetX;
        public int TargetY;


        /*********
        ** Public methods
        *********/
        public AnalyzeEventArgs(int tx, int ty)
        {
            this.TargetX = tx;
            this.TargetY = ty;
        }
    }

    public class Utilities
    {
        public static void InvokeEvent(string name, IEnumerable<Delegate> handlers, object sender)
        {
            var args = new EventArgs();
            foreach (EventHandler handler in handlers.Cast<EventHandler>())
            {
                try
                {
                    handler.Invoke(sender, args);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception while handling event {name}:\n{e}");
                }
            }
        }

        public static void InvokeEvent<T>(string name, IEnumerable<Delegate> handlers, object sender, T args)
        {
            foreach (EventHandler<T> handler in handlers.Cast<EventHandler<T>>())
            {
                try
                {
                    handler.Invoke(sender, args);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception while handling event {name}:\n{e}");
                }
            }
        }

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            if (who.modData.GetBool("moonslime.Wizardry.scrollspell") == true)
            {
                amount = 0;
            }
            who.AddCustomSkillExperience("moonslime.Wizard", amount);

        }

        public static void LearnedMagic(int EXP)
        {
            if (Game1.player.IsLocalPlayer)
            {
                Farmer player = Game1.player;
                Core.Utilities.AddEXP(player, EXP);
            }

        }

        public static void LearnedSpell(string spell)
        {
            if (Game1.player.IsLocalPlayer)
            {
                Farmer player = Game1.player;
                SpellBook spellBook = player.GetSpellBook();

                if (spellBook.KnowsSpell(spell, 0))
                    return;

                Log.Debug($"Player learnt spell: {spell}");
                spellBook.LearnSpell(spell, 0, true);

                // Show message in HUD
                var item = ItemRegistry.Create("moonslime.Wizardry.HudIcon");
                var message = new HUDMessage(I18n.Spell_Learn(spellName: SpellManager.Get(spell).GetTranslatedName()))
                {
                    messageSubject = item
                };
                Game1.addHUDMessage(message);
            }

        }

        public static List<Vector2> TilesAffected(Vector2 tileLocation, int level, Farmer who, bool hollow = false)
        {
            List<Vector2> list = new List<Vector2>();
            int centerX = (int)tileLocation.X;
            int centerY = (int)tileLocation.Y;

            float radius = 1.5f + level;
            float radiusSq = radius * radius;
            float innerRadiusSq = (radius - 0.75f) * (radius - 0.75f);

            for (int x = centerX - (int)radius; x <= centerX + (int)radius; x++)
            {
                for (int y = centerY - (int)radius; y <= centerY + (int)radius; y++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= radiusSq)
                    {
                        if (!hollow || distSq >= innerRadiusSq)
                            list.Add(new Vector2(x, y));
                    }
                }
            }

            return list;
        }
    }
}
