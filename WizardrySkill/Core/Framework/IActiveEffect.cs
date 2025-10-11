using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using Log = BirbCore.Attributes.Log;

namespace WizardrySkill.Core.Framework
{
    /// <summary>An active spell, projectile, or effect which should be updated or drawn.</summary>
    public interface IActiveEffect
    {
        /*********
        ** Methods
        *********/
        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        bool Update(UpdateTickedEventArgs e);

        /// <summary>Draw the effect to the screen if needed.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        void Draw(SpriteBatch spriteBatch);
    }
}
