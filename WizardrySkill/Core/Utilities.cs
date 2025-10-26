using System;
using System.Collections.Generic;
using System.Linq;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;

namespace WizardrySkill.Core
{
    public class AnalyzeEventArgs
    {
        /*********
        ** Accessors
        *********/
        public int TargetX;
        public int TargetY;


        /*********
        ** Public methods
        *********/
        public AnalyzeEventArgs(int tx, int ty)
        {
            this.TargetX = tx;
            this.TargetY = ty;
        }
    }

    public class Utilities
    {
        public static void InvokeEvent(string name, IEnumerable<Delegate> handlers, object sender)
        {
            var args = new EventArgs();
            foreach (EventHandler handler in handlers.Cast<EventHandler>())
            {
                try
                {
                    handler.Invoke(sender, args);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception while handling event {name}:\n{e}");
                }
            }
        }

        public static void InvokeEvent<T>(string name, IEnumerable<Delegate> handlers, object sender, T args)
        {
            foreach (EventHandler<T> handler in handlers.Cast<EventHandler<T>>())
            {
                try
                {
                    handler.Invoke(sender, args);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception while handling event {name}:\n{e}");
                }
            }
        }

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            SpaceCore.Skills.AddExperience(Game1.GetPlayer(who.UniqueMultiplayerID), "moonslime.Wizard", amount);

        }

        public static void LearnedMagic(int EXP)
        {
            if (Game1.player.IsLocalPlayer)
            {
                Farmer player = Game1.player;
                Core.Utilities.AddEXP(player, EXP);
            }

        }

        public static List<Vector2> TilesAffected(Vector2 tileLocation, int level, Farmer who)
        {
            List<Vector2> list = new List<Vector2>();
            int targetX = (int)tileLocation.X;
            int targetY = (int)tileLocation.Y;
            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {
                    list.Add(new Vector2(tileX, tileY));
                }
            }
            return list;
        }
    }
}
