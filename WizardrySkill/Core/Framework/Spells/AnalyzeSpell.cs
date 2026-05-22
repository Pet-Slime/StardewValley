using System;
using System.Collections.Generic;
using System.Linq;
using MoonShared.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class represents the "Analyze" spell, which detects nearby objects, items, terrain, and teaches the player discovered spells.
    public class AnalyzeSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private const string TeleportSpellId = "motion:teleport";
        private const string TeleportAnalyzeScannedItemsKey = "moonslime.Wizardry.analyze.motion.teleport.scannedItems";
        private const int TeleportAnalyzeRequiredItems = 5;


        /*********
        ** Public methods
        *********/

        // Constructor sets up the spell with its school and ID.
        public AnalyzeSpell()
            : base(SchoolId.Arcane, "analyze")
        {
            // This spell can be cast even when a menu is open.
            CanCastInMenus = true;
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        // The mana cost to cast this spell is always 0.
        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        // The maximum level the spell can be cast at is 1.
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // This is the main method that runs when the player casts the spell.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null)
                return null;

            // Only the local player should run this logic.
            if (!player.IsLocalPlayer)
                return null;

            // Get the player's spellbook so we can add any new spells they discover.
            SpellBook spellBook = player.GetSpellBook();

            // Store all spells we discover this cast, avoiding duplicates.
            HashSet<string> spellsLearnt = new();

            // Convert the clicked pixel coordinates to tile coordinates.
            Vector2 tilePos = new(targetX / Game1.tileSize, targetY / Game1.tileSize);

            // Step 1: Check items for possible spells.
            bool lightningRod = ProcessItemsForSpells(player, spellBook, spellsLearnt);

            // Step 2: Check the world for possible spells.
            if (Game1.activeClickableMenu == null)
                ProcessWorldForSpells(player, tilePos, lightningRod, spellsLearnt);

            // Step 3: Teach the player any discovered spells.
            bool learnedAny = this.LearnDiscoveredSpells(player, spellBook, spellsLearnt);

            // Step 4: Teach the player any tier 3 / ancient spells if they meet the conditions.
            bool learnedAncient = CheckAncientSpells(player, spellBook);

            // Step 5: Combine both discoveries to determine if the player learned anything.
            learnedAny = learnedAny || learnedAncient;

            // Step 6: Raise a custom event for other mods or code to respond to.
            if (Events.OnAnalyzeCast != null)
                Utilities.InvokeEvent("OnAnalyzeCast", Events.OnAnalyzeCast.GetInvocationList(), player, new AnalyzeEventArgs(targetX, targetY));

            // Step 7: Play success or fizzle effect depending on whether any spells were learned.
            return learnedAny
                ? new SpellSuccess(player, "secret1")
                : new SpellFizzle(player);
        }


        /*********
        ** Private helper methods
        *********/

        // Checks the player's current items (in hand, toolbar, or hovered) for spells to discover.
        private static bool ProcessItemsForSpells(Farmer player, SpellBook spellBook, ISet<string> spellsLearnt)
        {
            bool lightningRod = false;

            // Prevent one Analyze cast from processing the same item/spell source more than once.
            HashSet<string> processedAnalyzeSources = new(StringComparer.OrdinalIgnoreCase);

            // Get the relevant items to check.
            Item[] itemsToCheck =
            {
                GetItemFromMenu(Game1.activeClickableMenu),
                GetItemFromToolbar(),
                player.CurrentItem
            };

            foreach (Item activeItem in itemsToCheck)
            {
                // Skip null items or items without an ID.
                if (activeItem?.QualifiedItemId is null)
                    continue;

                // Check for custom spell data on the item.
                if (Game1.objectData.TryGetValue(activeItem.ItemId, out var data)
                    && data?.CustomFields != null
                    && data.CustomFields.TryGetValue("moonslime.Wizardry.analyze", out string spellString))
                {
                    ProcessAnalyzedSpellString(player, spellBook, spellsLearnt, processedAnalyzeSources, activeItem.QualifiedItemId, spellString, ref lightningRod);
                }

                if (Game1.bigCraftableData.TryGetValue(activeItem.ItemId, out var data2)
                    && data2?.CustomFields != null
                    && data2.CustomFields.TryGetValue("moonslime.Wizardry.analyze", out string spellString2))
                {
                    ProcessAnalyzedSpellString(player, spellBook, spellsLearnt, processedAnalyzeSources, activeItem.QualifiedItemId, spellString2, ref lightningRod);
                }

                // Some spells are inferred from item type.
                switch (activeItem)
                {
                    case Axe or Pickaxe:
                        spellsLearnt.Add("toil:cleardebris");
                        break;

                    case Hoe:
                        spellsLearnt.Add("toil:till");
                        break;

                    case WateringCan:
                        spellsLearnt.Add("toil:water");
                        break;

                    case Boots:
                        spellsLearnt.Add("motion:evac");
                        break;

                    case MeleeWeapon meleeWeapon when meleeWeapon.Name.Contains("Scythe", StringComparison.Ordinal):
                        spellsLearnt.Add("toil:cleardebris");
                        break;
                }
            }

            return lightningRod;
        }

        // Processes a spell string found on an analyzed item.
        private static void ProcessAnalyzedSpellString(Farmer player, SpellBook spellBook, ISet<string> spellsLearnt, ISet<string> processedAnalyzeSources, string itemQualifiedId, string spellString, ref bool lightningRod)
        {
            if (string.IsNullOrWhiteSpace(spellString))
                return;

            string sourceKey = $"{itemQualifiedId}:{spellString}";
            if (!processedAnalyzeSources.Add(sourceKey))
                return;

            if (spellString == "nature:lantern")
            {
                lightningRod = true;
                return;
            }

            if (spellString.Contains(TeleportSpellId, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTeleportAnalyzeProgress(player, spellBook, spellsLearnt, itemQualifiedId);
                return;
            }

            spellsLearnt.Add(spellString);
        }

        // Tracks unique teleport items scanned and learns Teleport after enough unique scans.
        private static void ProcessTeleportAnalyzeProgress(Farmer player, SpellBook spellBook, ISet<string> spellsLearnt, string itemQualifiedId)
        {
            if (player == null || spellBook == null || string.IsNullOrWhiteSpace(itemQualifiedId))
                return;

            // If the player already knows Teleport, don't keep tracking scans.
            if (spellBook.KnowsSpell(TeleportSpellId, 0))
                return;

            HashSet<string> scannedItems = GetTeleportAnalyzeScannedItems(player);

            // Scanning the same totem again should not increase progress.
            if (!scannedItems.Add(itemQualifiedId))
            {
                Log.Debug($"Teleport analyze progress unchanged. Already scanned {itemQualifiedId}. Progress: {scannedItems.Count}/{TeleportAnalyzeRequiredItems}");
                return;
            }

            player.modData[TeleportAnalyzeScannedItemsKey] = string.Join("|", scannedItems.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));

            Log.Debug($"Teleport analyze progress: {scannedItems.Count}/{TeleportAnalyzeRequiredItems}. Added {itemQualifiedId}.");

            if (scannedItems.Count >= TeleportAnalyzeRequiredItems)
                spellsLearnt.Add(TeleportSpellId);
        }

        // Gets the unique teleport items this player has already scanned.
        private static HashSet<string> GetTeleportAnalyzeScannedItems(Farmer player)
        {
            HashSet<string> scannedItems = new(StringComparer.OrdinalIgnoreCase);

            if (player?.modData == null)
                return scannedItems;

            if (!player.modData.TryGetValue(TeleportAnalyzeScannedItemsKey, out string raw) || string.IsNullOrWhiteSpace(raw))
                return scannedItems;

            foreach (string itemId in raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                scannedItems.Add(itemId);

            return scannedItems;
        }

        // Checks the environment for objects, terrain features, tiles, or enemies that could trigger spells.
        private static void ProcessWorldForSpells(Farmer player, Vector2 tilePos, bool lightningRod, ISet<string> spellsLearnt)
        {
            GameLocation location = player.currentLocation;
            if (location == null)
                return;

            // Detect nearby "Thunderbug" enemies if the lightning rod is active.
            if (lightningRod)
            {
                foreach (NPC character in location.characters)
                {
                    if (character is StardewValley.Monsters.Bug mob && Vector2.DistanceSquared(mob.Tile, tilePos) < 25f)
                    {
                        spellsLearnt.Add("nature:lantern");
                        break;
                    }
                }
            }

            // Detect crops in HoeDirt.
            if (location.terrainFeatures.TryGetValue(tilePos, out TerrainFeature feature) && feature is HoeDirt { crop: not null })
                spellsLearnt.Add("nature:tendrils");

            // Detect meteorites in resource clumps.
            foreach (ResourceClump clump in location.resourceClumps)
            {
                Rectangle clumpRect = new((int)clump.Tile.X, (int)clump.Tile.Y, clump.width.Value, clump.height.Value);
                if (clump.parentSheetIndex.Value == ResourceClump.meteoriteIndex && clumpRect.Contains((int)tilePos.X, (int)tilePos.Y))
                {
                    spellsLearnt.Add("elemental:meteor");
                    break;
                }
            }

            // Detect custom location-level spell data.
            var locationData = location.GetData();
            if (locationData?.CustomFields != null && locationData.CustomFields.TryGetValue("moonslime.Wizardry.analyze", out string spellString))
                spellsLearnt.Add(spellString);

            // Check for specific building tiles.
            var buildingsLayer = location.map.GetLayer("Buildings");
            if (buildingsLayer != null && location.isTileOnMap(tilePos))
            {
                var tile = buildingsLayer.Tiles[(int)tilePos.X, (int)tilePos.Y];
                if (tile?.TileIndex == 173)
                    spellsLearnt.Add("motion:descend");
            }

            // Check for water tiles in level 100 of the Mine.
            if (location is StardewValley.Locations.MineShaft { mineLevel: 100 } mineShaft
                && location.isTileOnMap(tilePos)
                && mineShaft.waterTiles[(int)tilePos.X, (int)tilePos.Y])
            {
                spellsLearnt.Add("eldritch:bloodmana");
            }
        }

        // Teaches the player all newly discovered spells and shows HUD messages.
        private bool LearnDiscoveredSpells(Farmer player, SpellBook spellBook, IEnumerable<string> spellsLearnt)
        {
            bool learnedAny = false;

            foreach (string spellId in spellsLearnt)
            {
                // Skip if player already knows the spell.
                if (spellBook.KnowsSpell(spellId, 0))
                    continue;

                Spell spell = SpellManager.Get(spellId);
                if (spell == null)
                    continue;

                learnedAny = true;
                Log.Debug($"Player learnt spell: {spellId}");
                spellBook.LearnSpell(spellId, 0, true);
                LearnSpellScroll(player, spellId);

                // Show message in HUD.
                ShowLearnMessage(I18n.Spell_Learn(spellName: spell.GetTranslatedName()));
            }

            return learnedAny;
        }

        // Checks for tier 3 / ancient spells and teaches them if the player qualifies.
        private static bool CheckAncientSpells(Farmer player, SpellBook spellBook)
        {
            bool learnedAnyAncient = false;
            bool knowsAll = true;

            foreach (string schoolId in School.GetSchoolList())
            {
                School school = School.GetSchool(schoolId);

                // Check if the player knows all tier 1 and 2 spells in the school.
                bool knowsAllSchool = KnowsAllSpellsInTier(spellBook, school.GetSpellsTier1()) && KnowsAllSpellsInTier(spellBook, school.GetSpellsTier2());

                if (!knowsAllSchool)
                    knowsAll = false;

                // Skip Arcane school for tier 3 logic. Special case handled later.
                if (schoolId == SchoolId.Arcane)
                    continue;

                // If all tier 1 and 2 spells are known, teach any unknown tier 3 spells.
                if (knowsAllSchool)
                {
                    foreach (Spell spell in school.GetSpellsTier3())
                    {
                        if (!spellBook.KnowsSpell(spell, 0))
                        {
                            LearnAncientSpell(spellBook, player, spell);
                            learnedAnyAncient = true;
                        }
                    }
                }
            }

            // Special Arcane ancient spell.
            Spell rewindSpell = School.GetSchool(SchoolId.Arcane).GetSpellsTier3().FirstOrDefault();
            if (knowsAll && rewindSpell != null && !spellBook.KnowsSpell(rewindSpell, 0))
            {
                LearnAncientSpell(spellBook, player, rewindSpell);
                learnedAnyAncient = true;
            }

            return learnedAnyAncient;
        }

        // Helper: Checks if the player knows all spells in a given tier.
        private static bool KnowsAllSpellsInTier(SpellBook book, IEnumerable<Spell> spells)
        {
            return spells.All(spell => book.KnowsSpell(spell, 0));
        }

        // Helper: Teaches an ancient spell and shows a HUD message.
        private static void LearnAncientSpell(SpellBook book, Farmer player, Spell spell)
        {
            Log.Debug("Player learnt ancient spell: " + spell);
            book.LearnSpell(spell, 0, true);
            LearnSpellScroll(player, spell.FullId);
            ShowLearnMessage(I18n.Spell_Learn_Ancient(spellName: spell.GetTranslatedName()));
        }

        // Helper: Displays a message in the game's HUD with a small icon.
        private static void ShowLearnMessage(string messageText)
        {
            Item item = ItemRegistry.Create("moonslime.Wizardry.HudIcon");
            HUDMessage message = new(messageText)
            {
                messageSubject = item
            };

            Game1.addHUDMessage(message);
        }

        // Retrieves the currently hovered item from a menu.
        private static Item GetItemFromMenu(IClickableMenu menu)
        {
            var reflection = ModEntry.Instance.Helper.Reflection;

            if (menu is GameMenu gameMenu)
            {
                List<IClickableMenu> pages = reflection.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();
                IClickableMenu page = pages[gameMenu.currentTab];

                if (page is InventoryPage)
                    return reflection.GetField<Item>(page, "hoveredItem").GetValue();

                if (page is CraftingPage)
                    return reflection.GetField<Item>(page, "hoverItem").GetValue();
            }
            else if (menu is MenuWithInventory inventoryMenu)
            {
                return inventoryMenu.hoveredItem;
            }

            return null;
        }

        // Retrieves the currently hovered item from the on-screen toolbar.
        private static Item GetItemFromToolbar()
        {
            // Cannot get toolbar item if a menu is open.
            if (Game1.activeClickableMenu != null)
                return null;

            var reflection = ModEntry.Instance.Helper.Reflection;
            Toolbar toolbar = Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
            if (toolbar == null)
                return null;

            List<ClickableComponent> buttons = reflection.GetField<List<ClickableComponent>>(toolbar, "buttons").GetValue();
            int x = Game1.getMouseX();
            int y = Game1.getMouseY();

            // Find which toolbar slot the mouse is over.
            ClickableComponent hoveredSlot = buttons.FirstOrDefault(slot => slot.containsPoint(x, y));

            if (hoveredSlot == null)
                return null;

            int index = buttons.IndexOf(hoveredSlot);
            return index >= 0 && index < Game1.player.Items.Count ? Game1.player.Items[index] : null;
        }

        public static void LearnSpellScroll(Farmer player, string spell)
        {
            foreach (KeyValuePair<string, string> recipePair in DataLoader.CraftingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(spell))
                    continue;

                if (conditions.Split(" ").Length < 2)
                    continue;

                int level = int.Parse(conditions.Split(" ")[1]);
                player.craftingRecipes.TryAdd(recipePair.Key, 0);
            }
        }
    }
}
