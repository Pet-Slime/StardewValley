using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "LanternSpell" that creates a light source around the player
    public class LanternSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private readonly Func<long> GetNewId;
        // Function to generate a unique ID for the lantern, used for multiplayer light tracking

        /*********
        ** Public methods
        *********/
        public LanternSpell(Func<long> getNewId)
            : base(SchoolId.Nature, "lantern")
        {
            // SchoolId.Nature identifies the spell's magical school
            // "lantern" is the internal name for this spell
            this.GetNewId = getNewId;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 10 * (level + 1);
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if there isn’t already a lantern from this player
            return base.CanCast(player, level) &&
                   !Game1.currentLightSources.ContainsKey($"LanternSpell_{player.UniqueMultiplayerID}");
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Play a thunder sound effect at the player’s location
            player.currentLocation.playSound("thunder", player.Tile);

            // Give experience proportional to the spell level
            Utilities.AddEXP(player, (level + 1) * 3);

            // Apply the lantern effect, which handles creating a light source around the player
            return new LanternEffect(player, level);
        }
    }
}
