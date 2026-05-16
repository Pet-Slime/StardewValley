using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "HealSpell" that restores health to the player
    public class HealSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public HealSpell()
            : base(SchoolId.Life, "heal")
        {
            // SchoolId.Life identifies the spell's magical school
            // "heal" is the internal name for this spell
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        // Heal only affects the caster's own health and local healing feedback.
        // It should never be executed from a received multiplayer spell packet.
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            return null;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost is 25% of the player's maximum mana
            return (player.GetMaxMana() / 4);
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if the player is not at full health
            return base.CanCast(player, level) && player.health != player.maxHealth;
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            // Calculate amount of health to restore
            int health = (player.maxHealth / 6) + ((level + 1) * 4);

            // Add health to the player
            player.health += health;

            // Ensure health does not exceed maximum
            if (player.health >= player.maxHealth)
                player.health = player.maxHealth;

            // Add a visual indicator of healing above the player
            player.currentLocation.debris.Add(new Debris(
                health,
                new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y),
                Color.Green,
                1f,
                player
            ));

            // Spell success: plays "healSound" and grants experience proportional to health restored
            return new SpellSuccess(player, "healSound", (int)(health * 0.5));
        }
    }
}
