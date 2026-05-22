using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "HealSpell" that restores health to the player.
    public class HealSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public HealSpell()
            : base(SchoolId.Life, "heal")
        {
            // SchoolId.Life identifies the spell's magical school.
            // "heal" is the internal name for this spell.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost is 25% of the player's maximum mana.
            return player.GetMaxMana() >> 2;
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if the player is not at full health.
            return base.CanCast(player, level) && player.health != player.maxHealth;
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should change personal health and local healing feedback.
            if (!player.IsLocalPlayer)
                return null;

            // Calculate amount of health to restore.
            int health = player.maxHealth / 6 + (level + 1) * 4;

            // Add health to the player, capped at max health.
            player.health = System.Math.Min(player.health + health, player.maxHealth);

            // Add a visual indicator of healing above the player.
            player.currentLocation.debris.Add(new Debris(
                health,
                new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y),
                Color.Green,
                1f,
                player
            ));

            // Spell success: plays "healSound" and grants experience proportional to health restored.
            return new SpellSuccess(player, "healSound", (int)(health >> 1));
        }
    }
}
