using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class SpellSuccess : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly Farmer Player;
        private readonly string Sound;
        private readonly int EXP;
        private const string SkillName = MagicConstants.SkillName;


        /*********
        ** Public methods
        *********/
        public SpellSuccess(Farmer player, string sound, int eXP = 0)
        {
            this.Player = player;
            this.Sound = sound;
            this.EXP = eXP;
        }

        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            var who = Game1.GetPlayer(this.Player.UniqueMultiplayerID, true);
            if (who != null)
            {
                who.currentLocation.playSound(this.Sound, this.Player.Tile);
                if (this.EXP != 0)
                {
                    who.AddCustomSkillExperience(SkillName, this.EXP);
                }
            }
            return false;
        }

        public void CleanUp()
        {

        }

        /// <summary>Draw the effect to the screen if needed.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public void Draw(SpriteBatch spriteBatch) { }
    }
}
