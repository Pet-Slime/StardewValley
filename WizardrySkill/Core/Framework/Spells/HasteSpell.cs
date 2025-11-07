using System;
using System.Linq;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "HasteSpell" that temporarily increases the player's movement speed
    public class HasteSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public HasteSpell()
            : base(SchoolId.Motion, "haste")
        {
            // SchoolId.Life identifies the spell's magical school
            // "haste" is the internal name for this spell
        }

        // Determines if the spell can be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Player must have at least 1 item with ID "(O)433" (probably a consumable), 
            // and must not already have this haste buff active
            return base.CanCast(player, level) && player.Items.ContainsId("(O)433", 1) && !player.hasBuff($"spell:life:haste:{level}");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 40 * (level + 1); // Mana cost scales with level
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            // If the buff is already active, the spell fizzles
            if (player.hasBuff($"spell:life:haste:{level}"))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Apply a new speed buff
            player.buffs.Apply(new Buff(
                id: $"spell:life:haste:{level}", // unique identifier for this buff
                source: $"spell:life:haste:{level}", // source name
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.haste.buffDescription") + level.ToString(), // text shown to player
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds, // buff duration in milliseconds
                effects: new StardewValley.Buffs.BuffEffects
                {
                    Speed = { level + 1 } // Increase speed by (level + 1)
                }
            ));

            // Consume one required item from inventory
            player.Items.ReduceId("(O)433", 1);

            // Player shows exclamation emote and plays a powerup sound
            player.performPlayerEmote("exclamation");
            player.currentLocation.playSound("powerup", player.Tile);

            // Spell success: visual/audio feedback and grants 5 XP
            return new SpellSuccess(player, "powerup", 5);
        }
    }
}
