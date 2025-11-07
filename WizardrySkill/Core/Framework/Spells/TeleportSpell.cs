using StardewValley;
using WizardrySkill.Core.Framework.Game.Interface;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // TeleportSpell lets the player teleport to a selected location using a special menu.
    public class TeleportSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: sets the spell's school and ID
        public TeleportSpell()
            : base(SchoolId.Motion, "teleport") { }


        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Checks whether the player can cast this spell
        public override bool CanCast(Farmer player, int level)
        {
            // Requirements:
            // - Base spell requirements (mana, cooldown, etc.)
            // - Must be outdoors
            // - Must not be riding a mount
            // - Must have a "Travel Core" item in inventory
            return base.CanCast(player, level)
                   && player.currentLocation.IsOutdoors
                   && player.mount == null
                   && player.Items.ContainsId("moonslime.Wizardry.Travel_Core");
        }


        public override int GetManaCost(Farmer player, int level)
        {
            return 10;
        }

        // What happens when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only allow local player to cast (prevents multiplayer desync)
            if (!player.IsLocalPlayer)
                return null;

            // Open the teleport menu if local player
            if (player.IsLocalPlayer)
                Game1.activeClickableMenu = new TeleportMenu(player);

            // Return a successful spell effect with a sound and grant exp
            return new SpellSuccess(player, "wand", 50);
        }
    }
}
