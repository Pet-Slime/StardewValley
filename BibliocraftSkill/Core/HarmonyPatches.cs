using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using BibliocraftSkill.Objects;
using HarmonyLib;
using MoonShared;
using SpaceCore;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.GameData.WildTrees;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using Log = MoonShared.Attributes.Log;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace BibliocraftSkill.Core
{

    [HarmonyPatch(typeof(Object), nameof(Object.performUseAction))]
    class OpenGeode_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(Object __instance)
        {
            if (__instance.HasContextTag("book_item"))
            {
                var who = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
                if (__instance.HasContextTag("mt_vapius_book"))
                {
                    Utilities.AddEXP(who, ModEntry.Config.ExperienceFromVapiusReading);
                } else
                {
                    Utilities.AddEXP(who, ModEntry.Config.ExperienceFromReading);
                }
            }
        }
    }


    [HarmonyPatch(typeof(Object), "readBook")]
    public class ReadBookPostfix_patch
    {
        public static KeyedProfession Prof_BookWorm => Book_Skill.Book5a;
        public static KeyedProfession Prof_PageFinder => Book_Skill.Book5b;
        public static KeyedProfession Prof_PageMaster => Book_Skill.Book10a1;
        public static KeyedProfession Prof_SeasonedReader => Book_Skill.Book10a2;
        public static KeyedProfession Prof_TypeSetter => Book_Skill.Book10b1;
        public static KeyedProfession Prof_BookSeller => Book_Skill.Book10b2;

        private const int BuffDurationMultiplier = 6000 * 10;
        private const int NumAttributes = 16;
        private const int MaxAttributeValue = 5;
        private const int MaxStaminaMultiplier = 16;

        // Dictionaries for bookmark effects
        private static readonly Dictionary<string, Action<BuffEffects, float>> BookmarkEffectMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["KnockbackMultiplier"] = (b, lvl) => b.KnockbackMultiplier.Value = lvl/100,
                ["WeaponSpeedMultiplier"] = (b, lvl) => b.WeaponSpeedMultiplier.Value = lvl/100,
                ["Immunity"] = (b, lvl) => b.Immunity.Value = (float)Math.Floor( Math.Max(lvl / 3, 1)),
                ["CriticalChanceMultiplier"] = (b, lvl) => b.CriticalChanceMultiplier.Value = lvl / 100,
                ["CriticalPowerMultiplier"] = (b, lvl) => b.CriticalPowerMultiplier.Value = lvl / 100,
                ["Default"] = (b, lvl) => b.AttackMultiplier.Value = lvl / 100
            };

        // Array of actions for random Bookworm attributes (1-16)
        private static readonly Action<BuffEffects, int>[] RandomAttributeActions = new Action<BuffEffects, int>[]
        {
        (b, lvl) => b.FarmingLevel.Value = lvl,              // 1
        (b, lvl) => b.FishingLevel.Value = lvl,             // 2
        (b, lvl) => b.MiningLevel.Value = lvl,              // 3
        (b, lvl) => b.LuckLevel.Value = lvl,                // 4
        (b, lvl) => b.ForagingLevel.Value = lvl,            // 5
        (b, lvl) => b.MaxStamina.Value = lvl * MaxStaminaMultiplier,  // 6
        (b, lvl) => b.MagneticRadius.Value = lvl * MaxStaminaMultiplier, // 7
        (b, lvl) => b.Defense.Value = lvl,                  // 8
        (b, lvl) => b.Attack.Value = lvl,                   // 9
        (b, lvl) => b.Speed.Value = lvl,                    // 10
        (b, lvl) => b.AttackMultiplier.Value = lvl / 100f,         // 11
        (b, lvl) => b.Immunity.Value = lvl,                 // 12
        (b, lvl) => b.KnockbackMultiplier.Value = lvl / 100f,      // 13
        (b, lvl) => b.WeaponSpeedMultiplier.Value = lvl / 100f,    // 14
        (b, lvl) => b.CriticalChanceMultiplier.Value = lvl / 100f, // 15
        (b, lvl) => b.CriticalPowerMultiplier.Value = lvl / 100f   // 16
        };

        [HarmonyPostfix]
        public static void Postfix(Object __instance, ref GameLocation location)
        {
            Farmer who = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            string itemID = __instance.ItemId;  // Get the item ID of the book
            int playerBookLevel = Utilities.GetLevel(who);  // Get the player's book level
            int buffDuration = BuffDurationMultiplier * playerBookLevel;  // Calculate buff duration

            // Log: Check if postfix was called and the player's level
            Log.Alert($"Postfix called for {itemID}. Player Book Level: {playerBookLevel}, Buff Duration: {buffDuration}");

            TryApplyBookmarkBuff(who, playerBookLevel, buffDuration);  // Apply bookmark buffs

            // Log: Check if the player has the Bookworm profession
            if (who.HasCustomProfession(Prof_BookWorm))
            {
                Log.Alert("Player has Bookworm profession, applying additional buffs.");
                TryApplyBookwormBuff(who, playerBookLevel, buffDuration);  // Apply Bookworm-specific buffs
                TryApplyPageMasterEffect(who, location, playerBookLevel);  // Apply Page Master effect
                TryApplySeasonedReaderEffect(who, location, playerBookLevel);  // Apply Seasoned Reader effect
            }
            else
            {
                Log.Alert("Player does not have Bookworm profession.");
            }

            HandleBookExperienceGain(who, __instance, itemID, playerBookLevel);  // Handle experience gain from reading the book

            // Log: Experience gain handled
            Log.Alert($"Experience gain handled for {itemID}. Book Level: {playerBookLevel}");
        }

        private static void TryApplyBookmarkBuff(Farmer who, int playerBookLevel, int buffDuration)
        {
            string prefix = "(O)moonslime.Bibliocraft.bookmark/";

            foreach (var item in who.Items)
            {
                if (item != null)
                {
                    Log.Alert($"Item: {item.Name}, ItemID: {item.QualifiedItemId}");
                }
            }

            // Log: Check if we find a bookmark item
            var foundBookmark = who.Items.FirstOrDefault(i => i is not null && i.QualifiedItemId.StartsWith(prefix));
            if (foundBookmark is not Item bookmark)
            {
                // Log: No bookmark found
                Log.Alert("No bookmark found in player's inventory.");
                return;
            }
            else
            {
                // Log: Bookmark found
                Log.Alert($"Bookmark found: {bookmark.QualifiedItemId}");
            }

            // Extract the effect name
            string effectName = bookmark.QualifiedItemId.Split("/")[1];
            // Log: Effect name parsed
            Log.Alert($"Effect name parsed: {effectName}");

            // Create the buff
            Buff buff = new(
                id: $"Bibliocraft:bookmark:{effectName}",
                displayName: ModEntry.Instance.I18N.Get("moonslime.Bibliocraft.Bookmark.buff"),
                description: null,
                iconTexture: null,
                iconSheetIndex: 0,
                duration: buffDuration,
                effects: null
            );
            BuffEffects effects = new();

            // Check if the effect name exists in the map
            if (!BookmarkEffectMap.TryGetValue(effectName, out var action))
            {
                // Log: Default effect used
                Log.Alert($"Effect for {effectName} not found in map. Using default effect.");
                action = BookmarkEffectMap["Default"];
            }
            else
            {
                // Log: Effect found in map
                Log.Alert($"Effect found in map for {effectName}.");
            }

            // Apply the effect
            action(effects, playerBookLevel);

            // Log: Effect applied
            Log.Alert($"Effect applied: {effectName} with player level {playerBookLevel}");

            // Add the effects to the buff and apply it
            buff.effects.Add(effects);
            who.applyBuff(buff);

            // Log: Buff applied to player
            Log.Alert($"Buff applied: {effectName}");

            // Reduce the item count
            who.Items.ReduceId(bookmark.QualifiedItemId, 1);

            // Log: Item reduced from inventory
            Log.Alert($"Item reduced from inventory: {bookmark.QualifiedItemId}");
        }


        private static void TryApplyBookwormBuff(Farmer who, int playerBookLevel, int buffDuration)
        {
            const string buffId = "Bibliocraft:profession:bookworm_buff";
            if (who.hasBuff(buffId)) return;

            Buff buff = new(
                id: buffId,
                displayName: ModEntry.Instance.I18N.Get("moonslime.Bibliocraft.Profession10b2.buff"),
                description: null,
                iconTexture: Assets.Bookworm_buff,
                iconSheetIndex: 0,
                duration: buffDuration,
                effects: null
            );

            int attributeBuff = Game1.random.Next(1, NumAttributes + 1);
            int attributeLevel = Game1.random.Next(1, MaxAttributeValue + 1);

            // Keep logging as-is
            Log.Trace("Bibliocraft: random attribute is: " + attributeBuff);
            Log.Trace("Bibliocraft: random level is: " + attributeLevel);

            BuffEffects randomEffect = new();
            RandomAttributeActions[attributeBuff - 1](randomEffect, attributeLevel);
            buff.effects.Add(randomEffect);

            who.applyBuff(buff);
        }

        private static void TryApplyPageMasterEffect(Farmer who, GameLocation location, int playerBookLevel)
        {
            if (!who.HasCustomProfession(Prof_PageMaster)) return;

            location.explode(
                who.Tile,
                (int)(playerBookLevel * 0.2),
                who,
                false,
                Game1.random.Choose(34, 35, 36, 37, 38, 39, 40, 41, 42),
                true
            );
            location.playSound("wind");
        }

        private static void TryApplySeasonedReaderEffect(Farmer who, GameLocation location, int playerBookLevel)
        {
            if (!who.HasCustomProfession(Prof_SeasonedReader)) return;

            List<Vector2> affected = TilesAffected(who.Tile, (int)(playerBookLevel * 0.2));

            foreach (var entry in location.terrainFeatures.Pairs)
            {
                if (entry.Value is not HoeDirt dirt || !affected.Contains(entry.Key)) continue;
                if (dirt.crop == null || dirt.crop.fullyGrown.Value) continue;

                dirt.crop.newDay(1);
            }

            location.updateMap();
        }

        private static void HandleBookExperienceGain(Farmer who, Object book, string itemID, int level)
        {
            if (itemID.StartsWith("SkillBook_")) { ApplySkillBookExp(who, itemID, level); return; }
            if (itemID == "PurpleBook") { ApplyPurpleBookExp(who, level); return; }

            if (Game1.player.stats.Get(book.itemId.Value) != 0 && itemID is not ("Book_PriceCatalogue" or "Book_AnimalCatalogue"))
                ApplyGenericBookExp(who, book, itemID, level);
        }

        private static void ApplySkillBookExp(Farmer who, string itemID, int level)
        {
            int count = who.newLevels.Count;
            int exp = (int)Math.Floor(250 * level * 0.05);
            who.gainExperience(Convert.ToInt32(itemID.Last().ToString() ?? "0"), exp);

            if (who.newLevels.Count == count || (who.newLevels.Count > 1 && count >= 1))
            {
                DelayedAction.functionAfterDelay(() =>
                {
                    Game1.showGlobalMessage(Game1.content.LoadString(
                        "Strings\\1_6_Strings:SkillBookMessage",
                        Game1.content.LoadString("Strings\\1_6_Strings:SkillName_" + itemID.Last()).ToLower()));
                }, 1000);
            }
        }

        private static void ApplyPurpleBookExp(Farmer who, int level)
        {
            int exp = (int)Math.Floor(250 * level * 0.05);
            for (int i = 0; i < 5; i++) who.gainExperience(i, exp);

            foreach (string skill in Skills.GetSkillList().Where(s => Skills.GetSkill(s).ShouldShowOnSkillsPage))
                Skills.AddExperience(who, skill, exp);
        }

        private static void ApplyGenericBookExp(Farmer who, Object book, string itemID, int level)
        {
            bool foundXpTag = false;

            foreach (string tag in book.GetContextTags())
            {
                if (!tag.StartsWithIgnoreCase("book_xp_")) continue;
                foundXpTag = true;
                string skill = tag.Split('_')[2];
                int exp = (int)Math.Floor(100 * level * 0.05);
                who.gainExperience(Farmer.getSkillNumberFromName(skill), exp);
                break;
            }

            if (!foundXpTag)
            {
                int exp = (int)Math.Floor(20 * level * 0.05);
                for (int i = 0; i < 5; i++) who.gainExperience(i, exp);
            }
        }

        private static List<Vector2> TilesAffected(Vector2 tile, int power)
        {
            List<Vector2> result = new();
            for (int x = (int)tile.X - power; x <= tile.X + power; x++)
                for (int y = (int)tile.Y - power; y <= tile.Y + power; y++)
                    result.Add(new Vector2(x, y));
            return result;
        }
    }



    //Goal of the patch is to increase wood dropping at a low chance if you have woody's secret
    [HarmonyPatch(typeof(Tree), "tickUpdate")]
    class WoodySecret_patch
    {
        [HarmonyPostfix]
        public static void Postfix(Tree __instance)
        {
            if (__instance.falling.Value)
            {
                WildTreeData data = __instance.GetData();
                Farmer farmer = Game1.GetPlayer(__instance.lastPlayerToHit.Value) ?? Game1.MasterPlayer;
                if (data.DropWoodOnChop &&
                    Utilities.GetLevel(farmer, true) >= 4 &&
                    farmer.stats.Get("Book_Woodcutting") != 0 &&
                    Game1.random.NextDouble() < 0.05)
                {
                    Vector2 tile = __instance.Tile;
                    GameLocation location = __instance.Location;
                    int num2 = (int)((farmer.professions.Contains(12) ? 1.25 : 1.0) * (12 + ExtraWoodCalculator(__instance, tile)));

                    Game1.createRadialDebris(location, 12, (int)tile.X + (__instance.shakeLeft.Value ? -4 : 4), (int)tile.Y, num2, resource: true);
                    Game1.createRadialDebris(location, 12, (int)tile.X + (__instance.shakeLeft.Value ? -4 : 4), (int)tile.Y, (int)((farmer.professions.Contains(12) ? 1.25 : 1.0) * (12 + ExtraWoodCalculator(__instance, tile))), resource: false);
                }
            }
        }

        private static int ExtraWoodCalculator(Tree tree, Vector2 tileLocation)
        {
            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, tileLocation.X * 7.0, tileLocation.Y * 11.0);
            int num = 0;
            if (random.NextDouble() < Game1.player.DailyLuck)
            {
                num++;
            }

            if (random.NextDouble() < Game1.player.ForagingLevel / 12.5)
            {
                num++;
            }

            if (random.NextDouble() < Game1.player.ForagingLevel / 12.5)
            {
                num++;
            }

            if (random.NextDouble() < Game1.player.LuckLevel / 25.0)
            {
                num++;
            }

            if (tree.treeType.Value == "3")
            {
                num++;
            }

            return num;
        }
    }


    //Goal of the patch is to increase monster drops at a low chance if you have the monster compendium
    [HarmonyPatch(typeof(GameLocation), "monsterDrop")]
    class MonsterCompedium_patch
    {
        [HarmonyPostfix]
        public static void Postfix(GameLocation __instance, ref Monster monster, ref int x, ref int y, ref Farmer who)
        {
            if (who.stats.Get("Book_Void") != 0 && Utilities.GetLevel(who, true) >= 7 && Game1.random.NextDouble() < 0.03 )
            {
                IList<string> objectsToDrop = monster.objectsToDrop;
                Vector2 vector = Utility.PointToVector2(who.StandingPixel);
                List<Item> extraDropItems = monster.getExtraDropItems();
                if (who.isWearingRing("526") && DataLoader.Monsters(Game1.content).TryGetValue(monster.Name, out string value))
                {
                    string[] array = ArgUtility.SplitBySpace(value.Split('/')[6]);
                    for (int i = 0; i < array.Length; i += 2)
                    {
                        if (Game1.random.NextDouble() < Convert.ToDouble(array[i + 1]))
                        {
                            objectsToDrop.Add(array[i]);
                        }
                    }
                }
                List<Debris> list = new List<Debris>();
                for (int j = 0; j < objectsToDrop.Count; j++)
                {
                    string text = objectsToDrop[j];
                    if (text != null && text.StartsWith('-') && int.TryParse(text, out int result))
                    {
                        list.Add(monster.ModifyMonsterLoot(new Debris(Math.Abs(result), Game1.random.Next(1, 4), new Vector2(x, y), vector)));
                    }
                    else
                    {
                        list.Add(monster.ModifyMonsterLoot(new Debris(text, new Vector2(x, y), vector)));
                    }
                }

                for (int k = 0; k < extraDropItems.Count; k++)
                {
                    list.Add(monster.ModifyMonsterLoot(new Debris(extraDropItems[k], new Vector2(x, y), vector)));
                }
                if (who.isWearingRing("526"))
                {
                    extraDropItems = monster.getExtraDropItems();
                    for (int l = 0; l < extraDropItems.Count; l++)
                    {
                        Item one = extraDropItems[l].getOne();
                        one.Stack = extraDropItems[l].Stack;
                        one.HasBeenInInventory = false;
                        list.Add(monster.ModifyMonsterLoot(new Debris(one, new Vector2(x, y), vector)));
                    }
                }
                foreach (Debris item2 in list)
                {
                    __instance.debris.Add(item2);
                }

            }
        }
    }

    //Goal is to prevent slowdown when running through grass
    [HarmonyPatch(typeof(Grass), "doCollisionAction")]
    class Slitherlegs_patch
    {
        [HarmonyPostfix]
        public static void Postfix(Grass __instance, ref Character who)
        {
            if (who is Farmer farmer)
            {
                if (farmer.stats.Get("Book_Grass") != 0 && Utilities.GetLevel(farmer, true) >= 4)
                {
                    farmer.temporarySpeedBuff = 1f;
                }
            }
        }
    }

    //diamond hunter
    [HarmonyPatch(typeof(Pickaxe), "DoFunction")]
    class DiamondHunter_patch
    {
        [HarmonyPostfix]
        public static void Postfix(GameLocation location, int x, int y, int power, Farmer who)
        {
            if (who != null && who.stats.Get("Book_Diamonds") != 0 && Utilities.GetLevel(who, true) >= 8 && Game1.random.NextDouble() < 0.0066 && Utilities.GetLevel(who, true) >= 8)
            {
                Utility.clampToTile(new Vector2(x, y));
                int num = x / 64;
                int num2 = y / 64;
                Vector2 vector = new Vector2(num, num2);
                location.Objects.TryGetValue(vector, out var value);
                if (value != null)
                {
                    if (value.IsBreakableStone() && value.MinutesUntilReady <= 0)
                    {
                        Game1.createObjectDebris("(O)72", num, num2, who.UniqueMultiplayerID, location);
                        if (who.professions.Contains(19) && Game1.random.NextBool())
                        {
                            Game1.createObjectDebris("(O)72", num, num2, who.UniqueMultiplayerID, location);
                        }
                    }
                }
            }
        }
    }

    //Goal of this patch is to increase the crabpot bonus rate when harvesting by hand
    [HarmonyPatch(typeof(StardewValley.Objects.CrabPot), "checkForAction")]
    class Blank_patch
    {
        [HarmonyTranspiler]
        public static void Postfix()
        {
            static IEnumerable<CodeInstruction> CrabPot_checkForAction_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                CodeMatcher matcher = new(instructions);
                // Old: NextDouble() < 0.25
                // New: NextDouble() < 0.25 + Patcher.GetExtraCrabPotDoublePercentage(who)
                // up to you to implement Patcher.GetExtraCrabPotDoublePercentage(who) returns a float
                //
                matcher
                  .MatchEndForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Random), nameof(Random.NextDouble))),
                    new CodeMatch(OpCodes.Ldc_R8, 0.25)
                    )
                  .ThrowIfNotMatch($"Could not find entry point for {nameof(CrabPot_checkForAction_Transpiler)}");
                matcher
                  .Advance(1)
                  .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Blank_patch), nameof(GetExtraCrabPotDoublePercentage))),
                new CodeInstruction(OpCodes.Add)
                      );
                return matcher.InstructionEnumeration();
            }
        }

        private static object GetExtraCrabPotDoublePercentage(Farmer who)
        {
            if (Utilities.GetLevel(who, true) >=8)
            {
                return 0.25;
            } else
            {
                return 0;
            }
        }
    }

    //Goal is to let the player send letters through the mail!
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.mailbox))]
    class Mailbox_patch
    {
        [HarmonyPrefix]
        public static bool Prefix(GameLocation __instance)
        {
            if (Game1.mailbox.Count == 0)
            {
                Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
                var heldItem = farmer.ActiveItem;


                if (heldItem != null && Game1.objectData.TryGetValue(heldItem.ItemId, out var data)
                        && data?.CustomFields != null
                        && data.CustomFields.TryGetValue("moonslime.Bibliocraft.mail", out string name))
                {
                    NPC NPC = Game1.getCharacterFromName(name, true, false);

                    if (NPC != null && //Make sure the NPC is not Null
                    NPC.CanReceiveGifts() &&
                    //Make sure the player has friendship data with them
                    farmer.friendshipData.ContainsKey(NPC.Name) &&
                    //Check to make sure the player has not given them two gifts this week
                    farmer.friendshipData[NPC.Name].GiftsThisWeek < 2 &&
                    //Check to make sure the player has not given them a gift today
                    farmer.friendshipData[NPC.Name].GiftsToday < 1)
                    {
                        Friendship friendship = farmer.friendshipData[NPC.Name];
                        friendship.GiftsToday++;
                        friendship.GiftsThisWeek++;
                        friendship.LastGiftDate = new WorldDate(Game1.Date);
                        data.CustomFields.TryGetValue($"moonslime.Bibliocraft.mail.friendship", out string points);
                        int friendshipGain = 65;
                        if (!string.IsNullOrEmpty(points) && int.TryParse(points, out int parsed))
                            friendshipGain = parsed;

                        friendship.Points += friendshipGain;

                        if (NPC.Birthday_Day == Game1.dayOfMonth && NPC.Birthday_Season == Game1.currentSeason)
                        {
                            friendship.Points += friendshipGain;
                        }

                        farmer.removeFirstOfThisItemFromInventory(heldItem.ItemId);
                        Game1.drawObjectDialogue(ModEntry.Instance.I18N.Get("moonslime.Bibliocraft.sent_mail") + $" {NPC.displayName}");

                        Utilities.AddEXP(farmer, ModEntry.Config.ExperienceFromMailing);

                        //don't run normal mailbox code
                        return false;
                    }
                    else
                    {
                        Game1.drawObjectDialogue(ModEntry.Instance.I18N.Get("moonslime.Bibliocraft.can_not_sent_mail"));
                        return false;
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Object), "getPriceAfterMultipliers")]
    class GetPriceAfterMultipliers_Patch
    {

        [HarmonyPostfix]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private static void IncereaseCosts(
        Object __instance, ref float __result, float startPrice, long specificPlayerID)
        {
            //Set the sale multiplier to 1
            float saleMultiplier = 1f;
            try
            {
                //For each farmer....
                foreach (var farmer in Game1.getAllFarmers())
                {
                    // If they use seperate wallets, get the seperate wallet
                    if (Game1.player.useSeparateWallets)
                    {
                        if (specificPlayerID == -1)
                        {
                            if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID || !farmer.isActive())
                            {
                                continue;
                            }
                        }
                        else if (farmer.UniqueMultiplayerID != specificPlayerID)
                        {
                            continue;
                        }
                    }
                    else if (!farmer.isActive())
                    {
                        continue;
                    }
                    // Look to see if the item has the context tag
                    if (__instance.HasContextTag("book_item"))
                    {
                        // If they have the right profession, increase the selling multipler by 1
                        if (farmer.HasCustomProfession(Book_Skill.Book10b2))
                        {
                            saleMultiplier += 1f;
                        }
                    }
                    
                    if (Utilities.GetLevel(farmer, true) >= 8 && farmer.stats.Get("Book_Artifact") != 0 && __instance.Type != null && __instance.Type.Equals("Arch"))
                    {
                        saleMultiplier += 1f;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed in {MethodBase.GetCurrentMethod()?.Name}:\n{ex}");
            }
            //Take the result, and then multiply it by the sales multiplier, along with the config to control display pricing
            __result *= saleMultiplier;
        }
    }
}
