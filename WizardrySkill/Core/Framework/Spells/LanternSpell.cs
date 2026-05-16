using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "LanternSpell" that creates a light source around the player.
    public class LanternSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private readonly Func<long> GetNewId;


        /*********
        ** Public methods
        *********/
        public LanternSpell(Func<long> getNewId)
            : base(SchoolId.Nature, "lantern")
        {
            this.GetNewId = getNewId;
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            return 10 * (level + 1);
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if there isn’t already a lantern from this player.
            return base.CanCast(player, level) &&
                   !Game1.currentLightSources.ContainsKey($"LanternSpell_{player.UniqueMultiplayerID}");
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            // Local sound is safe. Every client receiving the cast may play it locally.
            caster.currentLocation.LocalSoundAtPixel("thunder", caster.Position);

            if (caster.IsLocalPlayer)
            {
                // Give experience proportional to the spell level.
                Utilities.AddEXP(caster, (level + 1) * 3);
            }

            // Apply the lantern effect. Visual/light behavior runs locally on each client.
            // World/object mutation inside the effect is host-only.
            return new LanternEffect(caster, level);
        }
    }
}
