using MoonShared;
using StardewValley;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.VisualOnAll;

        // Defines how much mana it costs to cast this spell.
        public override int GetManaCost(Farmer player, int level)
        {
            return 5;
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
            return base.CanCast(player, level) && player.Items.ContainsId("767", 1);
        }

        // Called when the spell is cast directly.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        // Called when the spell is received through the spell sync system.
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            // Local sound only. This avoids re-broadcasting sound from received casts.
            caster.currentLocation.LocalSoundAtPixel("batScreech", caster.Position);

            // Only the actual casting player should consume the reagent and gain EXP.
            if (caster.IsLocalPlayer)
            {
                if (caster.modData.GetBool("moonslime.Wizardry.scrollspell") == false)
                    caster.Items.ReduceId("767", 1);

                Utilities.AddEXP(caster, 10);
            }

            // Level 1: artifact/pan spot helper.
            if (level == 0)
                return new BatArtifactEffect(caster, 100);

            // Level 2: monster-tracking helper.
            return new BatMonsterEffect(caster, 100);
        }
    }
}
