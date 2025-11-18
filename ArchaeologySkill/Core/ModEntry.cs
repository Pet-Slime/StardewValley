using System;
using System.Collections.Generic;
using ArchaeologySkill.Objects;
using MoonShared;
using MoonShared.APIs;
using MoonShared.Attributes;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace ArchaeologySkill.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;

        internal static Config Config;
        internal static Assets Assets;
        internal static bool XPDisplayLoaded => Instance.Helper.ModRegistry.IsLoaded("Shockah.XPDisplay");
        internal static bool JsonAssetsLoaded => Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
        internal static bool DynamicGameAssetsLoaded => Instance.Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets");

        public const string SkillID = "moonslime.Archaeology";


        internal readonly List<Func<Item, (int? SkillIndex, string SpaceCoreSkillName)?>> ToolSkillMatchers =
        [
            o => o is Hoe ? (null, SkillID) : null,
            o => o is Pan ? (null, SkillID) : null
        ];

        public ITranslationHelper I18N => this.Helper.Translation;
        internal static IJsonAssetsApi JAAPI;
        internal static IDynamicGameAssetsApi DGAAPI;
        internal static IXPDisplayApi XpAPI;

        public static Dictionary<string, List<string>> ItemDefinitions;
        public static readonly IList<string> BonusLootTable = [];
        public static readonly IList<string> ArtifactLootTable = [];
        public static readonly IList<string> WaterSifterLootTable = [];
        public static readonly IList<string> BonusLootTable_GI = [];
        public static readonly IList<string> WaterSifterLootTable_GI = [];



        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Parser.InitEvents(helper);
            Parser.ParseAll(this);


            ItemQueryResolver.Register(SkillID + ".ARCH_ITEM", ARCH_ITEM);
            ItemQueryResolver.Register(SkillID + ".WATER_SIFTER", WATER_SIFTER);
        }

        public static IEnumerable<ItemQueryResult> ARCH_ITEM(
                                         string key,
                                         string arguments,
                                         ItemQueryContext context,
                                         bool avoidRepeat,
                                         HashSet<string> avoidItemIds,
                                         Action<string, string> logError
                                     )
        {
            Random random = context.Random ?? Game1.random;
            Farmer player = context.Player ?? Game1.player;
            string location = context.Location.Name;
            int chanceMultiplier = 1;
            if (player != null)
            {
                chanceMultiplier = Utilities.GetLevel(player) + 1;
            }
            foreach (ParsedItemData data in ItemRegistry.GetObjectTypeDefinition().GetAllData())
            {
                if (!(data.ObjectType != "Arch"))
                {
                    ObjectData objectData = data.RawData as ObjectData;
                    Dictionary<string, float> dropChances = (objectData != null) ? objectData.ArtifactSpotChances : null;
                    if (dropChances != null && dropChances.TryGetValue(location, out float chance) && random.NextBool((float)chanceMultiplier * chance))
                    {
                        return new ItemQueryResult[]
                        {
                                new ItemQueryResult(ItemRegistry.Create(data.QualifiedItemId, 1, 0, false))
                        };
                    }
                }
            }
            return new ItemQueryResult[]
            {
                new ItemQueryResult(ItemRegistry.Create("(O)101", 1, 0, false))
            };
        }

        public static IEnumerable<ItemQueryResult> WATER_SIFTER(
                                 string key,
                                 string arguments,
                                 ItemQueryContext context,
                                 bool avoidRepeat,
                                 HashSet<string> avoidItemIds,
                                 Action<string, string> logError
                             )
        {
            Random random = context.Random ?? Game1.random;
            Farmer player = context.Player ?? Game1.player;
            string location = context.Location.Name;
            //Player Can get artifacts from the shift if they have the Trowler Profession
            bool flag = player != null && player.HasCustomProfession(Archaeology_Skill.Archaeology10b1);
            //Player Can get artifacts from the shift if they have the Trowler Profession
            bool flag2 = player != null && player.mailReceived.Contains("willyBoatFixed");

            //Generate the list of loot
            List<string> list =
            [
                //Populate the loot list
                .. ModEntry.WaterSifterLootTable,
            ];

            //If flag is true, add in the Ginger island loot table to the list
            if (flag2)
            {
                foreach(string thing in ModEntry.WaterSifterLootTable_GI)
                {
                    list.Add(thing);
                }
            }

            //If flag is true, add in the artifact loot table to the list
            if (flag)
            {
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
            list.Shuffle(random);

            //Choose a random item from the list. If the list somehow ended up empty (it shouldn't but just in case), give coal to the player.
            string item = list.RandomChoose(random, "384");
            if (item != null && Game1.objectData.TryGetValue(item, out ObjectData data) && data != null && data.Type != null)
            {
                int randomQuality = data.Type == "Arch" ? random.Choose(1) : random.Choose(1, 2, 3, 4, 5);
                return new ItemQueryResult[]
                {
                                new ItemQueryResult(ItemRegistry.Create(item, randomQuality, 0, false))
                };
            }
            return new ItemQueryResult[]
            {
                            new ItemQueryResult(ItemRegistry.Create(item, 1, 0, false))
            };
        }
    }
}
