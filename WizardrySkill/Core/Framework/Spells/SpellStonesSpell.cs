using System.Collections.Generic;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    /// <summary>A spell that converts stone items into magical stones with different effects.</summary>
    public class SpellStonesSpell : Spell
    {
        /*********
        ** Constants
        *********/

        // Required item ID for casting the spell.
        private const string StoneItemId = "(O)390";

        // Static readonly object IDs for created magic stones.
        private static readonly string StoneMana = "moonslime.Wizardry.Magic_Stone:Mana";
        private static readonly string StoneHealth = "moonslime.Wizardry.Magic_Stone:Health";
        private static readonly string StoneEnergy = "moonslime.Wizardry.Magic_Stone:Energy";
        private static readonly string Sound = "stoneCrack";

        // Static costs for consistency.
        private const int DamageCost = 10;
        private const int StaminaCost = 10;


        /*********
        ** Public methods
        *********/

        /// <summary>Constructor for the Stones spell.</summary>
        public SpellStonesSpell()
            : base(SchoolId.Arcane, "spellstones") { }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        /// <summary>Defines the maximum level this spell can be cast at.</summary>
        public override int GetMaxCastingLevel()
        {
            return 4;
        }

        /// <summary>Returns the mana cost for the given spell level.</summary>
        public override int GetManaCost(Farmer player, int level)
        {
            return level == 3 ? 30 : 10;
        }

        /// <summary>Returns the item cost for the given spell level.</summary>
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>
            {
                [StoneItemId] = level == 3 ? 3 : 1
            };
        }

        /// <summary>Determines whether the player can currently cast the spell at the given level.</summary>
        public override bool CanCast(Farmer player, int level)
        {
            if (!base.CanCast(player, level))
                return false;

            // Check level-specific health and stamina costs.
            return level switch
            {
                1 => player.health > DamageCost,
                2 => player.Stamina > StaminaCost,
                3 => player.health > DamageCost && player.Stamina > StaminaCost,
                _ => true
            };
        }

        /// <summary>Executes the spell's effects when cast.</summary>
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should consume inventory, health, stamina, and create debris.
            if (!player.IsLocalPlayer)
                return null;

            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            GameLocation location = player.currentLocation;
            var tile = player.TilePoint;

            switch (level)
            {
                case 0:
                    // Basic mana stone creation.
                    Game1.createObjectDebris(StoneMana, tile.X, tile.Y, location);
                    return new SpellSuccess(player, Sound, 5);

                case 1:
                    // Converts health into a health stone.
                    player.takeDamage(DamageCost, true, null);
                    Game1.createObjectDebris(StoneHealth, tile.X, tile.Y, location);
                    return new SpellSuccess(player, Sound, 5);

                case 2:
                    // Converts stamina into an energy stone.
                    player.Stamina -= StaminaCost;
                    Game1.createObjectDebris(StoneEnergy, tile.X, tile.Y, location);
                    return new SpellSuccess(player, Sound, 5);

                case 3:
                    // Consumes both health and stamina to create all three stones.
                    player.takeDamage(DamageCost, true, null);
                    player.Stamina -= StaminaCost;
                    Game1.createObjectDebris(StoneMana, tile.X, tile.Y, location);
                    Game1.createObjectDebris(StoneHealth, tile.X, tile.Y, location);
                    Game1.createObjectDebris(StoneEnergy, tile.X, tile.Y, location);
                    return new SpellSuccess(player, Sound, 15);

                default:
                    // Fallback behavior: always at least creates a mana stone.
                    Game1.createObjectDebris(StoneMana, tile.X, tile.Y, location);
                    return new SpellSuccess(player, Sound, 5);
            }
        }
    }
}
