using System;
using ExcavationSkill.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.Command;
using StardewValley;
using StardewValley.Tools;

namespace ExcavationSkill
{
    [CommandClass]
    public class Command
    {
        [CommandMethod("testing spawning a water shifter")]
        public static void invokewater()
        {
            string Stringhere = "ExcavationSkill.Objects.ShifterObject/stringHere";
            string type = Stringhere.Substring(0, Stringhere.IndexOf('/'));
            string arg = Stringhere.Substring(Stringhere.IndexOf('/') + 1);

            Log.Warn(type);
            Log.Warn(arg);


            int xLocation = Game1.player.getTileX();
            int yLocation = Game1.player.getTileY();
            Game1.createItemDebris(TestingThisThing(type, arg), new Vector2((float)xLocation + 0.5f, (float)yLocation + 0.5f) * 64f, -1);
        }

        public static Item TestingThisThing(string type, string arg)
        {
            var ctor = AccessTools.Constructor(AccessTools.TypeByName(type), new[] { typeof(string) });
            return (Item)ctor.Invoke(new object[] { arg });
        }

        [CommandMethod("testing spawning a water shifter")]
        public static void createwater()
        {
            int xLocation = Game1.player.getTileX();
            int yLocation = Game1.player.getTileY();
            Game1.createItemDebris(new ShifterObject("arg"), new Vector2((float)xLocation + 0.5f, (float)yLocation + 0.5f) * 64f, -1);
        }
    }
}
