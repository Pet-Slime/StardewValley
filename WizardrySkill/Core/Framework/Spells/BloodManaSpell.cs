using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class BloodManaSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private const float ManaRatioBase = 0.30f;
        private const float ManaRatioIncr = 0.05f;


        /*********
        ** Public methods
        *********/
        public BloodManaSpell()
            : base(SchoolId.Eldritch, "bloodmana") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return player.GetCurrentMana() != player.GetMaxMana() && player.health > (player.maxHealth / 4);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            int health = (player.maxHealth / 4);
            player.health -= health;
            player.currentLocation.debris.Add(new Debris(health, new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y), Color.Red, 1f, player));
            player.currentLocation.playSound("ow", player.Tile);
            Game1.hitShakeTimer = 100 * health;

            int mana = (player.GetMaxMana() / 6) + ((level + 1) * 4);
            player.AddMana(mana);
            player.currentLocation.debris.Add(new Debris(mana, new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y), Color.Blue, 1f, player));
            player.currentLocation.playSound("powerup", player.Tile);

            return null;
        }
    }
}
