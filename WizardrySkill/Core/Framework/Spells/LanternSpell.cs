using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class LanternSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private readonly Func<long> GetNewId;


        /*********
        ** Public methods
        *********/
        public LanternSpell(Func<long> getNewId)
            : base(SchoolId.Nature, "lantern")
        {
            this.GetNewId = getNewId;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return level * 3;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            int power = level switch
            {
                1 => 8,
                2 => 16,
                _ => 4
            };

            player.currentLocation.sharedLights.Add(this.GetUnusedLightSourceId(player.currentLocation), new LightSource(this.GetUnusedLightSourceId(player.currentLocation), 1, Game1.player.position.Value, power));
            player.currentLocation.playSound("furnace", player.Tile);
            Utilities.AddEXP(player, level);

            return null;
        }


        /*********
        ** Private methods
        *********/
        private string GetUnusedLightSourceId(GameLocation location)
        {
            while (true)
            {
                string id = this.GetNewId().ToString();
                if (!location.hasLightSource(id))
                    return id;
            }
        }
    }
}
