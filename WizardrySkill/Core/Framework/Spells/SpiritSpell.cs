using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // The SpiritSpell allows the player to summon a spirit effect at the cost of some health.
    public class SpiritSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: sets the spell's school (Eldritch) and its ID ("spirit")
        public SpiritSpell()
            : base(SchoolId.Eldritch, "spirit")
        {
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.VisualOnAll;

        // Defines how much mana it costs to cast this spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 50;
        }

        // Defines the maximum level at which this spell can be cast
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Checks if the spell can be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Player must meet the base spell requirements and have more than 1/5 of their max health.
            return base.CanCast(player, level) && player.health > player.maxHealth / 5;
        }

        // Original direct-cast fallback.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        // Called when a synced spell cast is received.
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            // Play a ghost sound at the caster's current tile.
            caster.currentLocation.LocalSoundAtPixel("ghost", caster.Position);

            if (caster.IsLocalPlayer)
            {
                // Cost to cast: remove 1/5 of the caster's max health.
                caster.takeDamage(caster.maxHealth / 5, false, null);

                // Give caster experience for casting.
                Utilities.AddEXP(caster, 50);
            }

            // Spawn the visual/functional effect of the spirit spell.
            return new SpiritEffect(caster, ModEntry.Config.Spirit_attack_range);
        }
    }
}
