using System;
using System.Collections.Generic;
using System.Linq;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
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
        // The sound effect or visual effect that will play when the spell is cast
        private IActiveEffect sfx;

        // Constructor sets up the spell with its school and ID
        public AnalyzeSpell() : base(SchoolId.Arcane, "analyze")
        {
            // This spell can be cast even when a menu is open
            CanCastInMenus = true;
        }

        // The mana cost to cast this spell is always 0
        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        // The maximum level the spell can be cast at is 1
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // This is the main method that runs when the player casts the spell
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only the local player should run this logic (prevents multiplayer desync)
            if (!player.IsLocalPlayer)
                return null;

            // Get the player's spellbook so we can add any new spells they discover
            SpellBook spellBook = player.GetSpellBook();

            // Store all spells we discover this cast, avoid duplicates
            var spellsLearnt = new HashSet<string>();

            // Convert the clicked pixel coordinates to tile coordinates
            Vector2 tilePos = new(targetX / Game1.tileSize, targetY / Game1.tileSize);

            // Step 1: Check items for possible spells
            bool lightningRod = ProcessItemsForSpells(player, spellsLearnt);

            // Step 2: Check the world (terrain, tiles, enemies) for possible spells
            if (Game1.activeClickableMenu == null)
                ProcessWorldForSpells(player, tilePos, lightningRod, spellsLearnt);

            // Step 3: Teach the player any discovered spells
            bool learnedAny = LearnDiscoveredSpells(player, spellBook, spellsLearnt);

            // Step 4: Teach the player any tier 3 / ancient spells if they meet the conditions
            bool learnedAncient = CheckAncientSpells(player, spellBook);

            // Step 5: Combine both discoveries to determine if the player learned anything
            learnedAny = learnedAny || learnedAncient;

            // Step 6: Play success or fizzle effect depending on whether any spells were learned
            sfx = learnedAny ? new SpellSuccess(player, "secret1") : new SpellFizzle(player);

            // Step 7: Raise a custom event for other mods or code to respond to
            if (Events.OnAnalyzeCast != null)
                Utilities.InvokeEvent("OnAnalyzeCast", Events.OnAnalyzeCast.GetInvocationList(), player, new AnalyzeEventArgs(targetX, targetY));

            // Return the effect to be displayed
            return sfx;
        }

        /*********
        ** Private Helper Methods
        *********/

        // Checks the player's current items (in hand, toolbar, or hovered) for spells to discover
        private static bool ProcessItemsForSpells(Farmer player, ISet<string> spellsLearnt)
        {
            bool lightningRod = false;

            // Get the relevant items to check
            var itemsToCheck = new[]
            {
                GetItemFromMenu(Game1.activeClickableMenu),
                GetItemFromToolbar(),
                player.CurrentItem
            };

            foreach (var activeItem in itemsToCheck)
            {
                // Skip null items or items without an ID
                if (activeItem?.QualifiedItemId is null)
                    continue;

                // Check for custom spell data on the item
                if (Game1.objectData.TryGetValue(activeItem.ItemId, out var data)
                    && data?.CustomFields != null
                    && data.CustomFields.TryGetValue("moonslime.Wizardry.analyze", out string spellString))
                {
                    // Special flag for the "nature:lantern" spell
                    if (spellString == "nature:lantern")
                        lightningRod = true;
                    else
                        spellsLearnt.Add(spellString); // Add any other discovered spell
                }

                // Some spells are inferred from item type
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
                        spellsLearnt.Add("life:evac");
                        break;
                    case MeleeWeapon mw when mw.Name.Contains("Scythe", StringComparison.Ordinal):
                        spellsLearnt.Add("toil:harvest");
                        break;
                }
            }

            // Return whether the "nature:lantern" condition was found
            return lightningRod;
        }

        // Checks the environment for objects, terrain features, tiles, or enemies that could trigger spells
        private static void ProcessWorldForSpells(Farmer player, Vector2 tilePos, bool lightningRod, ISet<string> spellsLearnt)
        {
            var location = player.currentLocation;

            // Detect nearby "Thunderbug" enemies if the lightning rod is active
            if (lightningRod)
            {
                foreach (var character in location.characters)
                {
                    if (character is StardewValley.Monsters.Bug mob &&
                        Vector2.DistanceSquared(mob.Tile, tilePos) < 25f) // 5-tile radius
                    {
                        spellsLearnt.Add("nature:lantern");
                        break;
                    }
                }
            }

            // Detect crops in HoeDirt
            if (location.terrainFeatures.TryGetValue(tilePos, out var feature)
                && feature is HoeDirt { crop: not null })
            {
                spellsLearnt.Add("nature:tendrils");
            }

            // Detect meteorites in resource clumps
            foreach (var clump in location.resourceClumps)
            {
                if (clump.parentSheetIndex.Value == ResourceClump.meteoriteIndex &&
                    new Rectangle((int)clump.Tile.X, (int)clump.Tile.Y, clump.width.Value, clump.height.Value)
                        .Contains((int)tilePos.X, (int)tilePos.Y))
                {
                    spellsLearnt.Add("eldritch:meteor");
                    break;
                }
            }

            // Detect custom location-level spell data
            var data = location.GetData();
            if (data?.CustomFields != null && data.CustomFields.TryGetValue("moonslime.Wizardry.analyze", out string spellString))
            {
                spellsLearnt.Add(spellString);
            }

            // Check for specific tiles (e.g., special buildings)
            var tile = location.map.GetLayer("Buildings").Tiles[(int)tilePos.X, (int)tilePos.Y];
            if (tile?.TileIndex == 173)
                spellsLearnt.Add("elemental:descend");

            // Check for water tiles in level 100 of the Mine
            if (location is StardewValley.Locations.MineShaft { mineLevel: 100 } ms &&
                ms.waterTiles[(int)tilePos.X, (int)tilePos.Y])
            {
                spellsLearnt.Add("eldritch:bloodmana");
            }
        }

        // Teaches the player all newly discovered spells and shows HUD messages
        private bool LearnDiscoveredSpells(Farmer player, SpellBook spellBook, IEnumerable<string> spellsLearnt)
        {
            bool learnedAny = false;

            foreach (string spell in spellsLearnt)
            {
                // Skip if player already knows the spell
                if (spellBook.KnowsSpell(spell, 0))
                    continue;

                learnedAny = true;
                Log.Debug($"Player learnt spell: {spell}");
                spellBook.LearnSpell(spell, 0, true);

                // Show message in HUD
                ShowLearnMessage(I18n.Spell_Learn(spellName: SpellManager.Get(spell).GetTranslatedName()));
            }

            return learnedAny;
        }

        // Checks for tier 3 / ancient spells and teaches them if the player qualifies
        private static bool CheckAncientSpells(Farmer player, SpellBook spellBook)
        {
            bool learnedAnyAncient = false;
            bool knowsAll = true;

            foreach (string schoolId in School.GetSchoolList())
            {
                var school = School.GetSchool(schoolId);

                // Check if the player knows all tier 1 and 2 spells in the school
                bool knowsAllSchool =
                    KnowsAllSpellsInTier(spellBook, school.GetSpellsTier1()) &&
                    KnowsAllSpellsInTier(spellBook, school.GetSpellsTier2());

                if (!knowsAllSchool)
                    knowsAll = false;

                // Skip Arcane school for tier 3 logic (special case handled later)
                if (schoolId == SchoolId.Arcane)
                    continue;

                // If all tier 1 and 2 spells are known, teach any unknown tier 3 spells
                if (knowsAllSchool)
                {
                    foreach (var spell in school.GetSpellsTier3())
                    {
                        if (!spellBook.KnowsSpell(spell, 0))
                        {
                            LearnAncientSpell(spellBook, player, spell);
                            learnedAnyAncient = true;
                        }
                    }
                }
            }

            // Special Arcane ancient spell
            var rewindSpell = School.GetSchool(SchoolId.Arcane).GetSpellsTier3().FirstOrDefault();
            if (knowsAll && rewindSpell != null && !spellBook.KnowsSpell(rewindSpell, 0))
            {
                LearnAncientSpell(spellBook, player, rewindSpell);
                learnedAnyAncient = true;
            }

            return learnedAnyAncient;
        }

        // Helper: Checks if the player knows all spells in a given tier
        private static bool KnowsAllSpellsInTier(SpellBook book, IEnumerable<Spell> spells)
        {
            return spells.All(spell => book.KnowsSpell(spell, 0));
        }

        // Helper: Teaches an ancient spell and shows a HUD message
        private static void LearnAncientSpell(SpellBook book, Farmer player, Spell spell)
        {
            Log.Debug("Player learnt ancient spell: " + spell);
            book.LearnSpell(spell, 0, true);
            ShowLearnMessage(I18n.Spell_Learn_Ancient(spellName: spell.GetTranslatedName()));
        }

        // Helper: Displays a message in the game's HUD with a small icon
        private static void ShowLearnMessage(string messageText)
        {
            var item = ItemRegistry.Create("moonslime.Wizardry.HudIcon");
            var message = new HUDMessage(messageText)
            {
                messageSubject = item
            };
            Game1.addHUDMessage(message);
        }

        // Retrieves the currently hovered item from a menu (inventory or crafting)
        private static Item GetItemFromMenu(IClickableMenu menu)
        {
            var reflection = ModEntry.Instance.Helper.Reflection;

            if (menu is GameMenu gameMenu)
            {
                var pages = reflection.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();
                IClickableMenu page = pages[gameMenu.currentTab];

                if (page is InventoryPage)
                    return reflection.GetField<Item>(page, "hoveredItem").GetValue();
                if (page is CraftingPage)
                    return reflection.GetField<Item>(page, "hoverItem").GetValue();
            }
            else if (menu is MenuWithInventory invMenu)
                return invMenu.hoveredItem;

            return null;
        }

        // Retrieves the currently hovered item from the on-screen toolbar
        private static Item GetItemFromToolbar()
        {
            // Cannot get toolbar item if a menu is open
            if (Game1.activeClickableMenu != null)
                return null;

            var reflection = ModEntry.Instance.Helper.Reflection;
            Toolbar toolbar = Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
            if (toolbar == null)
                return null;

            var buttons = reflection.GetField<List<ClickableComponent>>(toolbar, "buttons").GetValue();
            int x = Game1.getMouseX(), y = Game1.getMouseY();

            // Find which toolbar slot the mouse is over
            var hoveredSlot = buttons.FirstOrDefault(slot => slot.containsPoint(x, y));

            if (hoveredSlot == null)
                return null;

            int index = buttons.IndexOf(hoveredSlot);
            return index >= 0 && index < Game1.player.Items.Count
                ? Game1.player.Items[index]
                : null;
        }
    }
}
