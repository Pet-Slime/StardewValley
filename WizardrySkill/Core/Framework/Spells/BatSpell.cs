using System.Collections.Generic;
using System.Globalization;
using MoonShared;
using StardewValley;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // The BatSpell allows the player to summon a bat that tracks different targets based on level.
    public class BatSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public BatSpell()
            : base(SchoolId.Nature, "bat")
        {
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.Summon;

        // Defines how much mana it costs to cast this spell.
        public override int GetManaCost(Farmer player, int level)
        {
            return 5;
        }

        // Returns the item cost for casting this spell.
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>
            {
                ["767"] = 1 // Bat Wing
            };
        }

        // Defines the maximum level at which this spell can be cast.
        public override int GetMaxCastingLevel()
        {
            return 2;
        }

        // Checks if the spell can be cast.
        public override bool CanCast(Farmer player, int level)
        {
            // Player must meet the base spell requirements and have 1 Bat Wing.
            return player != null && base.CanCast(player, level);
        }

        // Called when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should consume the reagent, award EXP, and create/update durable summon state.
            if (!player.IsLocalPlayer)
                return null;

            // Local sound only. Other clients receive summon state and create local visuals from SummonManager.
            player.currentLocation.LocalSoundAtPixel("batScreech", player.Position);

            // Only the actual casting player should consume the reagent and gain EXP.
            // Consume the item unless this cast came from a scroll.
            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            Utilities.AddEXP(player, 10);

            string defId = level == 0
                ? SummonManager.SummonDefs.BatArtifact
                : SummonManager.SummonDefs.BatMonster;

            Dictionary<string, string> data = new()
            {
                [SummonManager.SummonDataKeys.AttackRange] = 100f.ToString(CultureInfo.InvariantCulture)
            };

            // SummonManager owns the durable summon state and creates/recreates the local visual instance.
            SummonManager.TryAddOrReplaceSummon(player, defId, level, data: data, broadcast: true);

            // No active effect is returned here because the local visual is owned by SummonManager.
            return null;
        }
    }
}
