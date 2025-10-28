using System;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class RewindSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public RewindSpell()
            : base(SchoolId.Arcane, "rewind") { }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.Items.ContainsId("336", 1) && Game1.timeOfDay != 600;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            player.Items.ReduceId("336", 1);
            var point = player.StandingPixel;

            point.X -= player.Sprite.SpriteWidth * 2;
            point.Y -= (int)(player.Sprite.SpriteHeight * 1.5);

            Game1.Multiplayer.broadcastSprites(player.currentLocation,
                new TemporaryAnimatedSprite(10,
                point.ToVector2(),
                Color.Yellow,
                10,
                Game1.random.NextDouble() < 0.5,
                70f,
                0,
                Game1.tileSize,
                100f));

            point.Y -= (int)(player.Sprite.SpriteHeight * 2.5);

            Game1.Multiplayer.broadcastSprites(player.currentLocation,
                new TemporaryAnimatedSprite(10,
                point.ToVector2(),
                Color.Yellow,
                10,
                Game1.random.NextDouble() < 0.5,
                70f,
                0,
                Game1.tileSize,
                100f));
            Game1.timeOfDay = Math.Max(600, Game1.timeOfDay - 200);





            return new SpellSuccess(player, "ticket_machine_whir", 25);
        }
    }
}
