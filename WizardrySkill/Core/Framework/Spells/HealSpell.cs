using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
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
            return (player.GetMaxMana() / 4) ;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.health != player.maxHealth;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            int health = (player.maxHealth / 6) + ((level + 1) * 4);
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
