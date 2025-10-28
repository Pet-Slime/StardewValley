using System.Collections.Generic;
using System.Linq;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    public class AnalyzeSpell : Spell
    {
        private IActiveEffect sfx;

        /*********
** Public methods
*********/
        public AnalyzeSpell()
            : base(SchoolId.Arcane, "analyze")
        {
            this.CanCastInMenus = true;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            SpellBook spellBook = player.GetSpellBook();
            List<string> spellsLearnt = new();
            // ReSharper disable twice PossibleLossOfFraction
            Vector2 tilePos = new(targetX / Game1.tileSize, targetY / Game1.tileSize);

            bool lightningRod = false;

            // get spells from item
            foreach (var activeItem in new[] { this.GetItemFromMenu(Game1.activeClickableMenu) ?? this.GetItemFromToolbar(), player.CurrentItem })
            {
                if (activeItem is not null && activeItem.QualifiedItemId != null)
                {
                    // by item type
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
                        case MeleeWeapon:
                            if (activeItem.Name.Contains("Scythe"))
                            {
                                spellsLearnt.Add("toil:harvest");
                            }
                            break;
                    }

                    switch (activeItem.QualifiedItemId)
                    {
                        case "(O)395" or "(O)253": // coffee
                            spellsLearnt.Add("life:haste");
                            break;

                        case "(O)773": // life elixir
                            spellsLearnt.Add("life:heal");
                            break;

                        case "(O)86": // earth crystal
                            spellsLearnt.Add("nature:shockwave");
                            break;

                        case "(O)82": // fire quartz
                            spellsLearnt.Add("elemental:fireball");
                            break;

                        case "(O)161": // ice pip
                            spellsLearnt.Add("elemental:frostbolt");
                            break;

                        case "(O)169" or "(O)388": // driftwood or wood
                            spellsLearnt.Add("elemental:kiln");
                            break;

                        case "(O)879": // monster musk
                            spellsLearnt.Add("eldritch:charm");
                            break;

                        case "(O)921": // Squid Ink Ravioli
                            spellsLearnt.Add("life:cleanse");
                            break;

                        case "(O)703" or "(O)519": // magnet, magnet ring
                            spellsLearnt.Add("nature:magnetic_force");
                            break;

                        case "(BC)9": // Squid Ink Ravioli
                            lightningRod = true;
                            break;
                    }
                }
            }

            // get spells from world
            if (Game1.activeClickableMenu == null)
            {
                // Thunderbug
                foreach (var charcter in player.currentLocation.characters)
                {
                    if (charcter is Bug mob && Vector2.Distance(mob.Tile, tilePos) < 5f && lightningRod)
                    {
                        spellsLearnt.Add("nature:lantern");
                        break;
                    }
                }

                // terrain features
                {
                    if (player.currentLocation.terrainFeatures.TryGetValue(tilePos, out TerrainFeature feature) && (feature as HoeDirt)?.crop != null)
                        spellsLearnt.Add("nature:tendrils");

                    foreach (ResourceClump clump in player.currentLocation.resourceClumps)
                    {
                        if (clump.parentSheetIndex.Value == ResourceClump.meteoriteIndex && new Rectangle((int)clump.Tile.X, (int)clump.Tile.Y, clump.width.Value, clump.height.Value).Contains((int)tilePos.X, (int)tilePos.Y))
                        {
                            spellsLearnt.Add("eldritch:meteor");
                            break;
                        }
                    }
                }

                // map tile
                {
                    // TODO: Add proper tilesheet check
                    var tile = player.currentLocation.map.GetLayer("Buildings").Tiles[(int)tilePos.X, (int)tilePos.Y];
                    if (tile?.TileIndex == 173)
                        spellsLearnt.Add("elemental:descend");

                    if (player.currentLocation.doesTileHaveProperty((int)tilePos.X, (int)tilePos.Y, "Action", "Buildings") == "EvilShrineLeft")
                        spellsLearnt.Add("eldritch:lucksteal");
                    if (player.currentLocation is StardewValley.Locations.MineShaft { mineLevel: 100 } ms && ms.waterTiles[(int)tilePos.X, (int)tilePos.Y])
                        spellsLearnt.Add("eldritch:bloodmana");
                }
            }

            // learn spells
            bool learnedAny = false;
            foreach (string spell in spellsLearnt)
            {
                if (spellBook.KnowsSpell(spell, 0))
                    continue;

                if (!learnedAny)
                {
                    this.sfx = new SpellSuccess(player, "secret1");
                    learnedAny = true;
                }

                Log.Debug($"Player learnt spell: {spell}");
                spellBook.LearnSpell(spell, 0, true);
                var item = ItemRegistry.Create("moonslime.Wizardry.HudIcon");
                var message = new HUDMessage(I18n.Spell_Learn(spellName: SpellManager.Get(spell).GetTranslatedName()));
                message.messageSubject = item;

                Game1.addHUDMessage(message);
            }

            // learn hidden spell if players knows all of the other spells for a school
            // TODO: add dungeons to get these
            {
                bool knowsAll = true;
                foreach (string schoolId in School.GetSchoolList())
                {
                    var school = School.GetSchool(schoolId);

                    bool knowsAllSchool = true;
                    foreach (var spell in school.GetSpellsTier1())
                    {
                        if (!spellBook.KnowsSpell(spell, 0))
                        {
                            knowsAll = knowsAllSchool = false;
                            break;
                        }
                    }
                    foreach (var spell in school.GetSpellsTier2())
                    {
                        if (!spellBook.KnowsSpell(spell, 0))
                        {
                            knowsAll = knowsAllSchool = false;
                            break;
                        }
                    }

                    // Have to know all other spells for the arcane one
                    if (schoolId == SchoolId.Arcane)
                        continue;


                    foreach (var spell in school.GetSpellsTier3())
                    {
                        var ancientSpell = spell;
                        if (knowsAllSchool && !spellBook.KnowsSpell(ancientSpell, 0))
                        {
                            Log.Debug("Player learnt ancient spell: " + ancientSpell);
                            spellBook.LearnSpell(ancientSpell, 0, true);

                            var item = ItemRegistry.Create("moonslime.Wizardry.HudIcon");
                            var message = new HUDMessage(I18n.Spell_Learn_Ancient(spellName: ancientSpell.GetTranslatedName()));
                            message.messageSubject = item;

                            Game1.addHUDMessage(message);
                        }
                    }


                }

                var rewindSpell = School.GetSchool(SchoolId.Arcane).GetSpellsTier3()[0];
                if (knowsAll && !spellBook.KnowsSpell(rewindSpell, 0))
                {
                    Log.Debug("Player learnt ancient spell: " + rewindSpell);
                    spellBook.LearnSpell(rewindSpell, 0, true);

                    var item = ItemRegistry.Create("moonslime.Wizardry.HudIcon");
                    var message = new HUDMessage(I18n.Spell_Learn_Ancient(spellName: rewindSpell.GetTranslatedName()));
                    message.messageSubject = item;

                    Game1.addHUDMessage(message);
                }
            }

            if (learnedAny == false)
            {
                this.sfx = new SpellFizzle(player);
            }

            // raise event
            if (Events.OnAnalyzeCast != null)
                Utilities.InvokeEvent("OnAnalyzeCast", Events.OnAnalyzeCast.GetInvocationList(), player, new AnalyzeEventArgs(targetX, targetY));

            return this.sfx;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the hovered item from an arbitrary menu.</summary>
        /// <param name="menu">The menu whose hovered item to find.</param>
        private Item GetItemFromMenu(IClickableMenu menu)
        {
            //
            // Copied from CJB Show Item Sell Price by CJBok and Pathoschild: https://github.com/CJBok/SDV-Mods
            // Released under the MIT License.
            //

            var reflection = ModEntry.Instance.Helper.Reflection;

            // game menu
            if (menu is GameMenu gameMenu)
            {
                IClickableMenu page = reflection.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue()[gameMenu.currentTab];
                if (page is InventoryPage)
                    return reflection.GetField<Item>(page, "hoveredItem").GetValue();
                if (page is CraftingPage)
                    return reflection.GetField<Item>(page, "hoverItem").GetValue();
            }

            // from inventory UI
            else if (menu is MenuWithInventory inventoryMenu)
                return inventoryMenu.hoveredItem;

            return null;
        }

        /// <summary>Get the hovered item from the on-screen toolbar.</summary>
        private Item GetItemFromToolbar()
        {
            //
            // Derived from CJB Show Item Sell Price by CJBok and Pathoschild: https://github.com/CJBok/SDV-Mods
            // Released under the MIT License.
            //

            var reflection = ModEntry.Instance.Helper.Reflection;

            // get toolbar
            if (Game1.activeClickableMenu is not null)
                return null;
            Toolbar toolbar = Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
            var toolbarSlots = toolbar != null ? reflection.GetField<List<ClickableComponent>>(toolbar, "buttons").GetValue() : null;
            if (toolbarSlots is null)
                return null;

            // find hovered slot
            int x = Game1.getMouseX();
            int y = Game1.getMouseY();
            ClickableComponent hoveredSlot = toolbarSlots.FirstOrDefault(slot => slot.containsPoint(x, y));
            if (hoveredSlot == null)
                return null;

            // get inventory index
            int index = toolbarSlots.IndexOf(hoveredSlot);
            if (index < 0 || index > Game1.player.Items.Count - 1)
                return null;

            // get hovered item
            return Game1.player.Items[index];
        }
    }
}
