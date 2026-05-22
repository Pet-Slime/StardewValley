using System.Linq;
using StardewValley;
using StardewValley.Buffs;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "CleanseSpell" that removes debuffs from the player.
    public class CleanseSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public CleanseSpell()
            : base(SchoolId.Life, "cleanse")
        {
            // SchoolId.Life identifies the spell's magical school.
            // "cleanse" is the internal name for this spell.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }

        // Determines the maximum level this spell can reach.
        public override int GetMaxCastingLevel()
        {
            return 2;
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null)
                return null;

            // Only the caster's own machine should remove personal buffs/debuffs.
            if (!player.IsLocalPlayer)
                return null;

            // Level 0: remove all buffs, good and bad.
            if (level == 0)
            {
                player.ClearBuffs();
            }
            else
            {
                // Get a list of all active buffs.
                var activeBuffs = player.buffs.AppliedBuffs.Values.ToList();

                foreach (Buff buff in activeBuffs)
                {
                    if (buff == null)
                        continue;

                    string buffId = buff.id;

                    // Check if the buff is marked as a debuff in the game data.
                    if (buffId != null && DataLoader.Buffs(Game1.content).TryGetValue(buffId, out var data) && data.IsDebuff == true)
                        player.buffs.Remove(buffId);
                }
            }

            return new SpellSuccess(player, "debuffSpell", 2);
        }
    }
}
