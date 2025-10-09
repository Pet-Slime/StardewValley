using WizardrySkill.Framework.Schools;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;
using WizardrySkill.Core;

namespace WizardrySkill.Framework.Spells
{
    public class HealSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public HealSpell()
            : base(SchoolId.Life, "heal") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 7;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.health != player.maxHealth;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            int health = 10 + 15 * level + (player.CombatLevel + 1) * 2;
            player.health += health;
            if (player.health >= player.maxHealth)
                player.health = player.maxHealth;
            player.currentLocation.debris.Add(new Debris(health, new Vector2(Game1.player.StandingPixel.X + 8, Game1.player.StandingPixel.Y), Color.Green, 1f, Game1.player));
            player.currentLocation.playSound("healSound", player.Tile);
            Utilities.AddEXP(player, health / 2);

            return null;
        }
    }
}
