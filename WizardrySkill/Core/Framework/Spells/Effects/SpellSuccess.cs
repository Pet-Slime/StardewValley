using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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


        /*********
        ** Public methods
        *********/
        public SpellSuccess(Farmer player, string sound)
        {
            this.Player = player;
            this.Sound = sound;
        }

        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            this.Player.currentLocation.playSound(this.Sound, this.Player.Tile);
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
