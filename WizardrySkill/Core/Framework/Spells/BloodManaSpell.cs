using Microsoft.Xna.Framework;
using MoonShared;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class BloodManaSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public BloodManaSpell()
            : base(SchoolId.Eldritch, "bloodmana")
        {
            // SchoolId.Eldritch identifies which magical school this spell belongs to
            // "bloodmana" is the internal name used to reference this spell
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return player.GetCurrentMana() != player.GetMaxMana() && player.health > (player.maxHealth / 4);
        }

        // Called when the spell is actually cast
        // targetX and targetY are coordinates where the spell is aimed (not used here)
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only execute this logic for the local player (prevents multiplayer issues)
            if (!player.IsLocalPlayer)
                return null;

            // Calculate health to sacrifice: 1/4 of max health
            int health = (player.maxHealth / 4);

            if (player.modData.GetBool("moonslime.Wizardry.scrollspell") == false)
            {
                // Reduce the player's health
                player.health -= health;
            }

            // Show floating red numbers above the player indicating lost health
            player.currentLocation.debris.Add(new Debris(
                health, // amount of health lost
                new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y), // position above the player
                Color.Red, // color of debris
                1f, // scale of debris
                player // who caused the debris
            ));

            // Play a hurt sound effect
            player.currentLocation.playSound("ow", player.Tile);

            // Shake the screen to give visual feedback of taking damage
            Game1.hitShakeTimer = 100 * health;

            // Calculate mana gained: 1/6 of max mana plus extra depending on spell level
            int mana = (player.GetMaxMana() / 6) + ((level + 1) * 4);

            // Add mana to the player
            player.AddMana(mana);

            // Show floating blue numbers above the player indicating mana gained
            player.currentLocation.debris.Add(new Debris(
                mana,
                new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y),
                Color.Blue,
                1f,
                player
            ));

            // Return a "success" effect that other systems can react to
            return new SpellSuccess(player, "ow"); // "ow" can be used for sound/feedback
        }
    }
}
