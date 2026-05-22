using System;
using System.Linq;
using StardewValley;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "LanternSpell" that creates a light source around the player.
    public class LanternSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public LanternSpell(Func<long> getNewId)
            : base(SchoolId.Nature, "lantern")
        {
            // Kept for the current SpellManager constructor call.
            // Summon identity now comes from owner multiplayer ID + summon slot instead of a generated ID.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.Summon;

        public override int GetManaCost(Farmer player, int level)
        {
            return 10 * (level + 1);
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if there isn't already a lantern summon from this player.
            return player != null
                && base.CanCast(player, level)
                && !SummonManager.GetSummonStates(player.UniqueMultiplayerID).Any(state => state.DefId == SummonManager.SummonDefs.Lantern);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should create/update durable summon state.
            if (!player.IsLocalPlayer)
                return null;

            // Local sound is safe. Other clients receive summon state and create local visuals from SummonManager.
            player.currentLocation.LocalSoundAtPixel("thunder", player.Position);

            // Give experience proportional to the spell level.
            Utilities.AddEXP(player, (level + 1) * 3);

            // SummonManager owns the durable summon state and creates/recreates the local visual instance.
            SummonManager.TryAddOrReplaceSummon(player, SummonManager.SummonDefs.Lantern, level, broadcast: true);

            // No active effect is returned here because the local visual is owned by SummonManager.
            return null;
        }
    }
}
