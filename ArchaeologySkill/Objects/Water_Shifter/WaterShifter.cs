using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using MoonShared;
using Netcode;
using SpaceCore;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ArchaeologySkill.Objects.Water_Shifter
{
    [XmlType("Mods_moonslime.Archaeology.water_shifter")]
    public class WaterShifter : CrabPot
    {
        public const int LidFlapTimerInterval = 60;

        [XmlIgnore]
        private float YBob;

        [XmlElement("Water_shifter_TITS")]
        public readonly NetInt Water_shifter_TITS = new NetInt(0);

        [XmlIgnore]
        public int TileIndexToShow
        {
            get => Water_shifter_TITS.Value;
            set => Water_shifter_TITS.Value = value;
        }

        [XmlIgnore]
        private bool LidFlapping;

        [XmlIgnore]
        private bool LidClosing;

        [XmlIgnore]
        private float LidFlapTimer;

        [XmlIgnore]
        private float ShakeTimer;

        [XmlIgnore]
        private Vector2 Shake;

        public WaterShifter() : base() { }

        public WaterShifter(Vector2 TileLocation, int stack = 1) : this()
        {
            TileLocation = Vector2.Zero;
            ParentSheetIndex = 0;
            base.itemId.Value = "moonslime.Archaeology.water_shifter";
            name = ModEntry.Instance.I18N.Get("moonslime.Archaeology.water_shifter.name");
            CanBeSetDown = true;
            CanBeGrabbed = false;
            IsSpawnedObject = false;
            this.Type = "interactive";
            this.TileIndexToShow = 0;
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            base.NetFields.AddField(Water_shifter_TITS, "Water_shifter_TITS");
        }

        public override void DayUpdate()
        {
            this.performRemoveAction();
        }

        public static Item OutputCask(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = 1440;
            Object @object = (Object)inputItem.getOne();

            var who = Game1.GetPlayer(machine.owner.Value);
            //Player Can get artifacts from the shift if they have the Trowler Profession
            bool flag = who != null && who.HasCustomProfession(Archaeology_Skill.Archaeology10b1);
            //Player Can get artifacts from the shift if they have the Trowler Profession
            bool flag2 = who != null && who.mailReceived.Contains("willyBoatFixed");

            //Generate the list of loot
            List<string> list =
            [
                //Populate the loot list
                .. ModEntry.WaterSifterLootTable,
            ];

            //If flag is true, add in the Ginger island loot table to the list
            if (flag2)
            {
            }

            //If flag is true, add in the artifact loot table to the list
            if (flag)
            {
                //Get the location of the water shifter
                string location = machine.Location.Name;
                //Get the artifact loot table
                foreach (string thing in ModEntry.ArtifactLootTable)
                {
                    //Get the game data for each object in the artifact loot table
                    if (Game1.objectData.TryGetValue(thing, out var value))
                    {
                        //Check the keys of the artifact spot chances to see if they match the machine's location
                        if (value.ArtifactSpotChances != null && value.ArtifactSpotChances.ContainsKey(location))
                        {
                            //Finally add it to the list of possible artifacts
                            list.Add(thing);
                        }
                    }
                }
            }


            //Shuffle the list so it's in a random order!
            Random random = Game1.random;
            list.Shuffle(random);

            //Choose a random item from the list. If the list somehow ended up empty (it shouldn't but just in case), give coal to the player.
            string item = list.RandomChoose(random, "382");
            if (item != null && Game1.objectData.TryGetValue(item, out ObjectData data) && data != null && data.Type != null)
            {
                int randomQuality = data.Type == "Arch" ? random.Choose(1) : random.Choose(1, 2, 3, 4, 5);
                @object = new Object(item, randomQuality);
            }

            return @object;
        }


    }
}
