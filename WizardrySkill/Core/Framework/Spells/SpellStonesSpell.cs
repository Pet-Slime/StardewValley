using MoonShared.Attributes;
using StardewModdingAPI;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;


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
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        /// <summary>
        /// Executes the spell's effects when received through the spell sync system.
        /// </summary>
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            var loc = caster.currentLocation;  // Cache current location
            var tile = caster.TilePoint;       // Cache player tile position

            // Only the actual casting player should pay inventory, health, and stamina costs.
            if (caster.IsLocalPlayer)
                this.ApplyCasterCosts(caster, level);

            // Only the host should create shared object debris in the world.
            if (Context.IsMainPlayer)
                this.CreateStones(level, tile.X, tile.Y, loc);

            return caster.IsLocalPlayer
                ? new SpellSuccess(caster, Sound, level == 3 ? 15 : 5)
                : null;
        }


        /*********
        ** Private helpers
        *********/

        /// <summary>
        /// Applies the caster-owned costs for the spell level.
        /// </summary>
        private void ApplyCasterCosts(Farmer player, int level)
        {
            switch (level)
            {
                case 0:
                    // Basic mana stone creation
                    player.Items.ReduceId(StoneItemId, 1);
                    break;

                case 1:
                    // Converts health into a health stone
                    player.Items.ReduceId(StoneItemId, 1);
                    player.takeDamage(DamageCost, true, null);
                    break;

                case 2:
                    // Converts stamina into an energy stone
                    player.Items.ReduceId(StoneItemId, 1);
                    player.Stamina -= StaminaCost;
                    break;

                case 3:
                    // Consumes both health and stamina to create all three stones
                    player.Items.ReduceId(StoneItemId, 3);
                    player.takeDamage(DamageCost, true, null);
                    player.Stamina -= StaminaCost;
                    break;

                default:
                    // Fallback behavior — always at least creates a mana stone
                    player.Items.ReduceId(StoneItemId, 1);
                    break;
            }
        }

        /// <summary>
        /// Creates the host-owned magic stone debris for the spell level.
        /// </summary>
        private void CreateStones(int level, int tileX, int tileY, GameLocation loc)
        {
            switch (level)
            {
                case 0:
                    // Basic mana stone creation
                    Game1.createObjectDebris(StoneMana, tileX, tileY, loc);
                    break;

                case 1:
                    // Converts health into a health stone
                    Game1.createObjectDebris(StoneHealth, tileX, tileY, loc);
                    break;

                case 2:
                    // Converts stamina into an energy stone
                    Game1.createObjectDebris(StoneEnergy, tileX, tileY, loc);
                    break;

                case 3:
                    // Consumes both health and stamina to create all three stones
                    Game1.createObjectDebris(StoneMana, tileX, tileY, loc);
                    Game1.createObjectDebris(StoneHealth, tileX, tileY, loc);
                    Game1.createObjectDebris(StoneEnergy, tileX, tileY, loc);
                    break;

                default:
                    // Fallback behavior — always at least creates a mana stone
                    Game1.createObjectDebris(StoneMana, tileX, tileY, loc);
                    break;
            }
        }
    }
}
