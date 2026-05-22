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
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level)
                && player.GetCurrentMana() != player.GetMaxMana()
                && player.health > player.maxHealth / 4;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should change health, mana, debris, and shake feedback.
            if (!player.IsLocalPlayer)
                return null;

            int healthCost = player.maxHealth / 4;

            if (player.modData.GetBool("moonslime.Wizardry.scrollspell") == false)
                player.health -= healthCost;

            player.currentLocation.debris.Add(new Debris(healthCost, new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y), Color.Red, 1f, player));
            player.currentLocation.playSound("ow", player.Tile);

            Game1.hitShakeTimer = 100 * healthCost;

            int manaGain = player.GetMaxMana() / 6 + (level + 1) * 4;
            player.AddMana(manaGain);

            player.currentLocation.debris.Add(new Debris(manaGain, new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y), Color.Blue, 1f, player));

            return new SpellSuccess(player, "cavedrip");
        }
    }
}
