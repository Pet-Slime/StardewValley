using System.Collections.Generic;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "HealAreaSpell" that creates a healing aura around the caster.
    public class HealAreaSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public HealAreaSpell()
            : base(SchoolId.Life, "healarea")
        {
            // SchoolId.Life identifies the spell's magical school.
            // "healarea" is the internal name for this spell.
            HealAreaEffect.Init();
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.NetworkedEffect;

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost is 25% of the player's maximum mana.
            return player.GetMaxMana() >> 2;
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can always cast if base spell conditions are met.
            return base.CanCast(player, level);
        }

        // Called when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Play immediate local feedback for the caster.
            player.currentLocation.LocalSoundAtPixel("healSound", player.Position);

            // The caster-owned effect handles aura timing, visual broadcasts, and heal pulse packets.
            return new HealAreaEffect(player, level);
        }

        // Called when another machine observes this spell cast.
        public override IActiveEffect OnRemoteCast(Farmer caster, int level, int targetX, int targetY, IDictionary<string, string> data)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            // Remote machines only play initial cast feedback.
            // Ongoing visuals are broadcast by the caster-owned HealAreaEffect,
            // and healing is handled through heal pulse packets.
            caster.currentLocation.LocalSoundAtPixel("healSound", caster.Position);

            return null;
        }
    }
}
