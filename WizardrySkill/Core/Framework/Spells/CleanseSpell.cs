using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class CleanseSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public CleanseSpell()
            : base(SchoolId.Life, "cleanse") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }



        public override int GetMaxCastingLevel()
        {
            return 2;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (!player.IsLocalPlayer)
                return null;


            if (level == 0)
            { 
                player.ClearBuffs();
            } else
            {
                var activeBuffs = player.buffs.AppliedBuffs.Values.ToList();

                foreach (Buff buff in activeBuffs)
                {
                    if (buff == null)
                        continue;


                    string id = buff.id;
                    // The buff data pool includes a boolean property “IsDebuff”
                    // which indicates whether this buff should be treated as a debuff. :contentReference[oaicite:3]{index=3}
                    if (id != null && DataLoader.Buffs(Game1.content).TryGetValue(id, out var value))
                    {
                        if (value.IsDebuff == true)
                        {
                            // Remove the buff by its ID
                            player.buffs.Remove(id);
                        }
                    }
                }
            }

                return new SpellSuccess(player, "debuffSpell", 2);
        }
    }
}
