using MoonShared.Attributes;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    /// <summary>
    /// A spell that converts stone items into magical stones with different effects.
    /// </summary>
    public class SpellStonesSpell : Spell
    {
        /*********
        ** Constants
        *********/

        // Required item ID for casting the spell
        private const string StoneItemId = "(O)390";

        // Static readonly object IDs for created magic stones (never change)
        private static readonly string StoneMana = "moonslime.Wizardry.Magic_Stone:Mana";
        private static readonly string StoneHealth = "moonslime.Wizardry.Magic_Stone:Health";
        private static readonly string StoneEnergy = "moonslime.Wizardry.Magic_Stone:Energy";
        private static readonly string Sound = "stoneCrack";

        // Static costs for consistency
        private const int DamageCost = 10;
        private const int StaminaCost = 10;

        /*********
        ** Public methods
        *********/

        /// <summary>
        /// Constructor for the Stones spell.
        /// </summary>
        public SpellStonesSpell()
            : base(SchoolId.Arcane, "spellstones") { }


        /// <summary>
        /// Defines the maximum level this spell can be cast at.
        /// </summary>
        public override int GetMaxCastingLevel()
        {
            return 4;
        }


        /// <summary>
        /// Determines whether the player can currently cast the spell at the given level.
        /// </summary>
        public override bool CanCast(Farmer player, int level)
        {
            // Must meet base requirements first
            if (!base.CanCast(player, level))
                return false;

            // Must have at least one stone in inventory
            if (!player.Items.ContainsId(StoneItemId, 1))
                return false;

            // Check level-specific requirements
            return level switch
            {
                1 => player.health > DamageCost,                            // Health cost variant
                2 => player.Stamina > StaminaCost,                          // Energy cost variant
                3 => player.health > DamageCost && player.Stamina > StaminaCost, // Both costs
                _ => true
            };
        }


        /// <summary>
        /// Returns the mana cost for the given spell level.
        /// </summary>
        public override int GetManaCost(Farmer player, int level)
        {
            return level == 3 ? 30 : 10;
        }


        /// <summary>
        /// Executes the spell's effects when cast.
        /// </summary>
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            var loc = player.currentLocation;  // Cache current location
            var tile = player.TilePoint;       // Cache player tile position

            switch (level)
            {
                case 0:
                    // Basic mana stone creation
                    player.Items.ReduceId(StoneItemId, 1);
                    Game1.createObjectDebris(StoneMana, tile.X, tile.Y, loc);
                    return new SpellSuccess(player, Sound, 5);

                case 1:
                    // Converts health into a health stone
                    player.Items.ReduceId(StoneItemId, 1);
                    player.takeDamage(DamageCost, true, null);
                    Game1.createObjectDebris(StoneHealth, tile.X, tile.Y, loc);
                    return new SpellSuccess(player, Sound, 5);

                case 2:
                    // Converts stamina into an energy stone
                    player.Items.ReduceId(StoneItemId, 1);
                    player.Stamina -= StaminaCost;
                    Game1.createObjectDebris(StoneEnergy, tile.X, tile.Y, loc);
                    return new SpellSuccess(player, Sound, 5);

                case 3:
                    // Consumes both health and stamina to create all three stones
                    player.Items.ReduceId(StoneItemId, 3);
                    player.takeDamage(DamageCost, true, null);
                    player.Stamina -= StaminaCost;
                    Game1.createObjectDebris(StoneMana, tile.X, tile.Y, loc);
                    Game1.createObjectDebris(StoneHealth, tile.X, tile.Y, loc);
                    Game1.createObjectDebris(StoneEnergy, tile.X, tile.Y, loc);
                    return new SpellSuccess(player, Sound, 15);

                default:
                    // Fallback behavior — always at least creates a mana stone
                    player.Items.ReduceId(StoneItemId, 1);
                    Game1.createObjectDebris(StoneMana, tile.X, tile.Y, loc);
                    return new SpellSuccess(player, Sound, 5);
            }
        }
    }
}
