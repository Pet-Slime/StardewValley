using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "CleanseSpell" that removes debuffs from the player
    public class CleanseSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public CleanseSpell()
            : base(SchoolId.Life, "cleanse")
        {
            // SchoolId.Life identifies the spell's magical school
            // "cleanse" is the internal name for this spell
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }

        // Determines the maximum level this spell can reach
        public override int GetMaxCastingLevel()
        {
            return 2;
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run this for the local player
            if (!player.IsLocalPlayer)
                return null;

            // Level 0: remove all buffs (good and bad)
            if (level == 0)
            {
                player.ClearBuffs();
            }
            else
            {
                // Get a list of all active buffs
                var activeBuffs = player.buffs.AppliedBuffs.Values.ToList();

                foreach (Buff buff in activeBuffs)
                {
                    if (buff == null)
                        continue;

                    string id = buff.id;

                    // Check if the buff is marked as a debuff in the game data
                    if (id != null && DataLoader.Buffs(Game1.content).TryGetValue(id, out var value))
                    {
                        if (value.IsDebuff == true)
                        {
                            // Remove the debuff from the player
                            player.buffs.Remove(id);
                        }
                    }
                }
            }

            // Return a success effect with sound "debuffSpell" and grant 2 experince
            return new SpellSuccess(player, "debuffSpell", 2);
        }
    }
}
