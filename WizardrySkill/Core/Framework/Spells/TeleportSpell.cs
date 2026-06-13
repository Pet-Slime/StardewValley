using System.Collections.Generic;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Checks whether the player can cast this spell
        public override bool CanCast(Farmer player, int level)
        {
            // Requirements:
            // - Base spell requirements.
            // - Must be outdoors.
            // - Must not be riding a mount.
            // - Must have a Travel Core item in inventory.
            return base.CanCast(player, level)
                && player?.currentLocation?.IsOutdoors == true
                && player.mount == null;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 10;
        }

        // Returns the item cost for casting this spell.
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>
            {
                ["moonslime.Wizardry.Travel_Core"] = 1
            };
        }

        // What happens when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null)
                return null;

            // Only the caster's own machine should open the teleport menu.
            if (!player.IsLocalPlayer)
                return null;

            if (player.currentLocation?.IsOutdoors != true)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            PlayerRoutePathfinder.EnsureCache(player);

            // Only consume the item after the route cache is ready and we know the menu can open.
            // Consume the item unless this cast came from a scroll.
            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            Game1.activeClickableMenu = new TeleportMenu(player);

            // Return a successful spell effect with a sound and grant exp.
            return new SpellSuccess(player, "wand", 50);
        }
    }
}
