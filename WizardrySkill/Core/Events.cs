using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using MoonShared.APIs;
using MoonShared.Attributes;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Game.Interface;
using WizardrySkill.Core.Framework.Spells;
using WizardrySkill.Objects;
using Log = MoonShared.Attributes.Log;

namespace WizardrySkill.Core
{
    [SEvent]
    public class Events
    {
        /*********
        ** Constants
        *********/
        private const string BaseModDataKey = "moonSlime.Wizardry.ActiveEffect";

        /*********
        ** Fields
        *********/
        private static Texture2D SpellBg;
        private static Texture2D ManaBg;
        private static Texture2D ManaFg;
        private static IInputHelper InputHelper;
        private static bool CastPressed;
        private static double CarryoverManaRegen;

        /// <summary>The active effects, spells, or projectiles which should be updated or drawn.</summary>
        private static readonly IList<IActiveEffect> ActiveEffects = [];

        /// <summary>The self-updating views of magic metadata for each player.</summary>
        /// <remarks>This should only be accessed through <see cref="GetSpellBook"/> or <see cref="Extensions.GetSpellBook"/> to make sure an updated instance is retrieved.</remarks>
        private static readonly IDictionary<long, SpellBook> SpellBookCache = new Dictionary<long, SpellBook>();

        /*********
        ** Caching fields
        *********/
        private static readonly Color DisabledColor = new(128, 128, 128);

        private static Point CachedViewport;
        private static IClickableMenu CachedToolbarRef;
        private static Rectangle CachedToolbarBounds;
        private static bool CachedDrawBarAboveToolbar;
        public static bool CachedSpellMenuOpen = false;
        private static Point[] CachedSpellSpots;
        private static StaticSpellDraw[] CachedStaticSpells;
        private static PreparedSpellBar LastPreparedSpellBar;
        private static string CachedHoverText;
        private static int LastMouseX, LastMouseY;

        private static int FrameCounter = 0;
        private static bool[] CachedCanCastStates;

        // Static spell data cache (layout, textures, tooltips)
        private class StaticSpellDraw
        {
            public Rectangle Bounds;
            public Texture2D Icon;
            public Texture2D LevelIcon;
            public string Tooltip;
            public Spell Spell;
            public int Level;
        }

        /*********
        ** Properties / Accessors
        *********/
        public static Wizard_Skill Skill = new();
        public static EventHandler<AnalyzeEventArgs> OnAnalyzeCast;

        private static readonly Lazy<Func<Toolbar, List<ClickableComponent>>> ToolbarButtonsGetter =
            new(() => AccessTools.DeclaredField(typeof(Toolbar), "buttons").EmitInstanceGetter<Toolbar, List<ClickableComponent>>());

        public static bool LearnedMagic =>
            Game1.player?.eventsSeen?.Contains(MagicConstants.LearnedMagicEventId.ToString()) == true;

        /*********
        ** Core spell IDs
        *********/
        private static readonly string[] CoreSpells =
        {
            "arcane:analyze",
            "elemental:magicmissle",
            "arcane:enchant",
            "arcane:disenchant"
        };

        /*********
        ** Private helpers
        *********/
        private static Toolbar? GetToolbar()
        {
            return Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
        }

        /*********
        ** Public lifecycle methods
        *********/
        public static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ModEntry.Editor = new(ModEntry.Config, ModEntry.Instance.Helper.ModContent, ModEntry.HasStardewValleyExpanded);
        }

        [SEvent.GameLaunchedLate]
        public static void GameLaunchedLate(object sender, GameLaunchedEventArgs e)
        {
            ModEntry.Editor = new(ModEntry.Config, ModEntry.Instance.Helper.ModContent, ModEntry.HasStardewValleyExpanded);

            // hook Mana Bar
            var manaBar = ModEntry.Instance.Helper.ModRegistry.GetApi<IManaBarApi>("moonslime.ManaBarApi");
            if (manaBar == null)
            {
                Log.Error("No mana bar API???");
                return;
            }
            ModEntry.Mana = manaBar;

            var helper = ModEntry.Instance.Helper;
            Log.Trace("Magic: Trying to Register skill.");
            Init(helper.Input, helper.Multiplayer.GetNewID);
        }

        [SEvent.ReturnedToTitle]
        public static void ReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            foreach (var effect in ActiveEffects)
            {
                effect.CleanUp();
            }
            ActiveEffects.Clear();
        }

        [SEvent.SaveLoaded]
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            foreach (var farmers in Game1.getAllFarmers())
            {
                if (farmers.eventsSeen.Contains("90001") &&
                    !farmers.mailReceived.Contains("moonslimeWizardryLearnedMagic"))
                {
                    farmers.mailReceived.Add("moonslimeWizardryLearnedMagic");
                }

                farmers.modData["moonslime.Wizardry.scrollspell"] = "no";
            }

            Farmer player = Game1.player;
            string Id = "moonslime.Wizard";
            int skillLevel = player.GetCustomSkillLevel(Id);
            foreach (KeyValuePair<string, string> recipePair in DataLoader.CraftingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(Id))
                    continue;
                if (conditions.Split(" ").Length < 2)
                    continue;

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                    continue;

                player.craftingRecipes.TryAdd(recipePair.Key, 0);
            }

            foreach (KeyValuePair<string, string> recipePair in DataLoader.CookingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 3, "");
                if (!conditions.Contains(Id))
                {
                    continue;
                }
                if (conditions.Split(" ").Length < 2)
                {
                    continue;
                }

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                {
                    continue;
                }

                if (Game1.player.cookingRecipes.TryAdd(recipePair.Key, 0) &&
                    !Game1.player.hasOrWillReceiveMail("robinKitchenLetter"))
                {
                    Game1.mailbox.Add("robinKitchenLetter");
                }
            }

            SpellBook book = player.GetSpellBook();
            foreach (var spell in book.KnownSpells)
            {
                Id = spell.Key;
                foreach (KeyValuePair<string, string> recipePair in DataLoader.CraftingRecipes(Game1.content))
                {
                    string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                    if (!conditions.Contains(Id))
                        continue;
                    if (conditions.Split(" ").Length < 2)
                        continue;

                    int level = int.Parse(conditions.Split(" ")[1]);


                    player.craftingRecipes.TryAdd(recipePair.Key, 0);
                }
            }

            if (!Context.IsMainPlayer)
                return;

            try
            {
                ModEntry.LegacyDataMigrator.OnSaveLoaded();
            }
            catch (Exception ex)
            {
                Log.Warn($"Exception migrating legacy save data: {ex}");
            }
        }

        [SEvent.DayStarted]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            // fix player's magic info if needed
            FixMagicIfNeeded(player);
        }

        [SEvent.Saving]
        private void OnSaving(object sender, SavingEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            ModEntry.LegacyDataMigrator.OnSaved();
            ModEntry.Instance.Helper.Events.GameLoop.Saving -= this.OnSaving;
        }

        /*********
        ** Public methods
        *********/
        public static void Init(IInputHelper inputHelper, Func<long> getNewId)
        {
            InputHelper = inputHelper;

            LoadAssets();
            SpellManager.Init(getNewId);

            OnAnalyzeCast += (sender, e) => ModEntry.Instance.Api.InvokeOnAnalyzeCast(sender as Farmer);

            SpaceCore.Skills.RegisterSkill(Skill);
        }

        public static void LoadAssets()
        {
            SpellBg = ModEntry.Assets.Spellbg;
            ManaBg = ModEntry.Assets.Manabg;

            Color manaCol = new(0, 48, 255);
            ManaFg = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            ManaFg.SetData([manaCol]);
        }

        [SEvent.AssetRequested]
        public static void AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            ModEntry.Editor.TryEdit(e);
        }

        [SEvent.UpdateTicked]
        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Farm farm = Game1.getFarm();
            string playerKey = $"{BaseModDataKey}/{Game1.player.UniqueMultiplayerID}";

            if (farm.modData.TryGetValue(playerKey, out string rawData) && !string.IsNullOrWhiteSpace(rawData))
            {
                var messages = ReadAndClearActiveEffects(farm, playerKey);
                Log.Trace($"Got data to {Game1.player.displayName}");
                foreach (var msg in messages)
                {
                    Farmer caster = Game1.GetPlayer(msg.CasterId);
                    if (caster == null) continue;

                    IActiveEffect effect = caster.GetSpellBook()
                        .CastSpell(msg.SpellFullId, msg.Level, msg.X, msg.Y);
                    if (effect != null)
                        ActiveEffects.Add(effect);
                }
            }

            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                IActiveEffect effect = ActiveEffects[i];
                if (!effect.Update(e))
                    ActiveEffects.RemoveAt(i);
            }
        }

        public static List<(long CasterId, string SpellFullId, int Level, int X, int Y)>
            ReadAndClearActiveEffects(Farm farm, string newKey)
        {
            var results = new List<(long, string, int, int, int)>();
            if (farm == null) return results;

            if (!farm.modData.TryGetValue(newKey, out string raw) || string.IsNullOrWhiteSpace(raw))
                return results;

            foreach (string entry in raw.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(',');
                if (parts.Length < 5) continue;
                if (!long.TryParse(parts[0], out long casterId)) continue;

                string spellFullId = parts[1];

                if (!int.TryParse(parts[2], out int level)) continue;
                if (!int.TryParse(parts[3], out int x)) continue;
                if (!int.TryParse(parts[4], out int y)) continue;

                results.Add((casterId, spellFullId, level, x, y));
            }

            farm.modData[newKey] = "";
            return results;
        }

        [SEvent.ButtonPressed]
        public static void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            bool hasFifthSpellSlot = Game1.player.HasCustomProfession(Wizard_Skill.Magic10a2);
            bool hasMenuOpen = Game1.activeClickableMenu is not null;

            if (e.Button == ModEntry.Config.Key_Cast) CastPressed = true;

            if (CastPressed && e.Button == ModEntry.Config.Key_SwapSpells && !hasMenuOpen)
            {
                Game1.player.GetSpellBook().SwapPreparedSet();
                InputHelper.Suppress(e.Button);
            }
            else if (CastPressed &&
                     (e.Button == ModEntry.Config.Key_Spell1 || e.Button == ModEntry.Config.Key_Spell2 ||
                      e.Button == ModEntry.Config.Key_Spell3 || e.Button == ModEntry.Config.Key_Spell4 ||
                      (e.Button == ModEntry.Config.Key_Spell5 && hasFifthSpellSlot)))
            {
                int slotIndex = 0;
                if (e.Button == ModEntry.Config.Key_Spell1) slotIndex = 0;
                else if (e.Button == ModEntry.Config.Key_Spell2) slotIndex = 1;
                else if (e.Button == ModEntry.Config.Key_Spell3) slotIndex = 2;
                else if (e.Button == ModEntry.Config.Key_Spell4) slotIndex = 3;
                else if (e.Button == ModEntry.Config.Key_Spell5) slotIndex = 4;

                InputHelper.Suppress(e.Button);
                Farmer player = Game1.player;
                SpellBook spellBook = player.GetSpellBook();
                PreparedSpellBar prepared = spellBook.GetPreparedSpells();
                PreparedSpell slot = prepared?.GetSlot(slotIndex);
                if (slot == null) return;

                Spell spell = SpellManager.Get(slot.SpellId);
                if (spell == null) return;

                bool canCast =
                    spellBook.CanCastSpell(spell, slot.Level) &&
                    (!hasMenuOpen || spell.CanCastInMenus);

                if (canCast)
                {
                    Log.Alert("Casting " + slot.SpellId);
                    player.AddMana(-spell.GetManaCost(player, slot.Level));
                    player.modData["moonslime.Wizardry.scrollspell"] = "no";
                    Point pos = new Point(Game1.getMouseX() + Game1.viewport.X, Game1.getMouseY() + Game1.viewport.Y);
                    string entry = $"{player.UniqueMultiplayerID},{spell.FullId},{slot.Level},{pos.X},{pos.Y}";

                    Farm farm = Game1.getFarm();
                    foreach (var who in Game1.getOnlineFarmers())
                    {
                        string playerKey = $"{BaseModDataKey}/{who.UniqueMultiplayerID}";
                        Log.Trace($"Sending data to {who.displayName}");
                        if (!farm.modData.TryGetValue(playerKey, out string existing))
                            existing = "";

                        if (!string.IsNullOrEmpty(existing))
                            existing += "/";

                        farm.modData[playerKey] = existing + entry;
                    }
                }
            }
        }

        [SEvent.ButtonReleased]
        public static void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button == ModEntry.Config.Key_Cast)
                CastPressed = false;
        }

        [SEvent.TimeChanged]
        public static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            Farmer player = Game1.player;
            int level = player.GetCustomSkillLevel(Skill);
            double manaRegen = 0;

            if (ModEntry.Config.EnableBaseManaRegen)
                manaRegen = (level + 1) / 2;

            if (player.HasCustomProfession(Wizard_Skill.Magic10b1))
                manaRegen += level * 0.5;
            if (player.HasCustomProfession(Wizard_Skill.Magic5b))
                manaRegen += level * 0.5;

            manaRegen += CarryoverManaRegen;

            player.AddMana((int)manaRegen);
            CarryoverManaRegen = manaRegen - (int)manaRegen;
        }

        [SEvent.Warped]
        public static void OnWarped(object sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer) return;

            EvacSpell.OnLocationChanged();

            if (e.NewLocation.IsOutdoors && !e.Player.modData.ContainsKey("moonslime.Wizardry.TeleportTo." + e.NewLocation.Name))
                e.Player.modData.Add("moonslime.Wizardry.TeleportTo." + e.NewLocation.Name, "");
        }

        public static SpellBook GetSpellBook(Farmer player)
        {
            if (!SpellBookCache.TryGetValue(player.UniqueMultiplayerID, out SpellBook book) ||
                !object.ReferenceEquals(player, book.Player))
                SpellBookCache[player.UniqueMultiplayerID] = book = new SpellBook(player);

            return book;
        }

        public static void FixMagicIfNeeded(Farmer player, int? overrideMagicLevel = null, bool fixMana = false)
        {
            if (!LearnedMagic && overrideMagicLevel is not > 0)
                return;

            int magicLevel = overrideMagicLevel ?? player.GetCustomSkillLevel("moonslime.Wizard");
            SpellBook spellBook = player.GetSpellBook();

            if (fixMana) { }

            if (spellBook.Prepared.Count < MagicConstants.SpellBarCount)
            {
                spellBook.Mutate(data =>
                {
                    while (spellBook.Prepared.Count < MagicConstants.SpellBarCount)
                        data.Prepared.Add(new PreparedSpellBar());
                });
            }

            Skills.Skill skill = Skills.GetSkill("moonslime.Wizard");
            foreach (var profession in skill.Professions)
            {
                if (!player.professions.Contains(profession.GetVanillaId()))
                    continue;

                string modDataKey = $"{skill.Id}.{profession.Id}";
                if (!player.modData.ContainsKey(modDataKey))
                {
                    player.modData.SetBool(modDataKey, true);
                    MoonShared.Attributes.Log.Trace($"Player now has Profession mod data: {modDataKey}");
                }
            }

            foreach (string spellId in CoreSpells)
            {
                if (!spellBook.KnowsSpell(spellId, 0))
                    spellBook.LearnSpell(spellId, 0, true);
            }
        }


        [SEvent.RenderingHud]
        private static void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // Skip drawing if menus are open, events are active, or the player can't act
            if (Game1.activeClickableMenu != null || Game1.eventUp || !LearnedMagic || !Context.IsPlayerFree)
                return;

            SpriteBatch b = e.SpriteBatch;
            var viewport = Game1.graphics.GraphicsDevice.Viewport.Bounds;

            // 1. Draw all active visual spell effects (e.g. ongoing auras)
            DrawActiveEffects(b);

            // 2. Try to get toolbar info (toolbar instance + button list)
            if (!TryGetToolbarInfo(out Toolbar toolbar, out var buttons))
                return; // toolbar not ready — skip drawing

            // Determine number of available spell slots
            bool hasFifthSpellSlot = Game1.player.HasCustomProfession(Wizard_Skill.Magic10a2);
            int totalSlots = hasFifthSpellSlot ? 5 : 4;

            // Detect any changes that require position recalculation
            bool viewportChanged = CheckViewportChanged(viewport);
            var toolbarBounds = GetToolbarBounds(buttons);
            bool drawBarAboveToolbar = ShouldDrawAboveToolbar(toolbarBounds, viewport);
            bool toolbarMoved = CheckToolbarChanged(toolbar, toolbarBounds, drawBarAboveToolbar, totalSlots, viewportChanged, CachedSpellMenuOpen);

            // If toolbar or viewport changed → recalculate slot layout
            if (toolbarMoved)
                RecalculateSpellBar(toolbar, toolbarBounds, drawBarAboveToolbar, totalSlots);

            // 3. Draw spell icons and manage hover text
            SpellBook spellBook = Game1.player.GetSpellBook();
            PreparedSpellBar prepared = spellBook.GetPreparedSpells();
            if (prepared == null)
                return;

            // Detect if player switched to a different spell bar
            bool spellBarChanged = DetectSpellBarChange(prepared);

            // If toolbar, spell bar, or cache changed → rebuild static icon cache
            if (CachedStaticSpells == null || spellBarChanged || toolbarMoved)
                RebuildStaticSpellCache(spellBook, prepared, totalSlots);

            // Refresh CanCast states every few frames (for mana/inventory updates)
            UpdateCanCastCache(spellBook, totalSlots);

            // Draw empty background slots
            DrawEmptySpellSlots(b, totalSlots);

            // Draw actual spells + hover text
            DrawSpellsAndHover(b, spellBook, totalSlots, toolbarMoved, viewportChanged, spellBarChanged);
        }

        [SEvent.RenderedHud]
        private static void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Skip if player isn't in active control
            if (Game1.activeClickableMenu != null || Game1.eventUp || !LearnedMagic || !Context.IsPlayerFree)
                return;

            // Draw hover tooltip *above* the toolbar (if applicable)
            if (CachedHoverText != null && CachedDrawBarAboveToolbar)
                IClickableMenu.drawHoverText(e.SpriteBatch, CachedHoverText, Game1.smallFont);
        }

        /// <summary>
        /// Draws all currently active spell effects (visual overlays or auras).
        /// </summary>
        private static void DrawActiveEffects(SpriteBatch b)
        {
            foreach (IActiveEffect effect in ActiveEffects)
                effect.Draw(b);
        }

        /// <summary>
        /// Safely retrieves the toolbar and its clickable buttons.
        /// Returns false if toolbar isn’t ready.
        /// </summary>
        private static bool TryGetToolbarInfo(out Toolbar toolbar, out IList<ClickableComponent> buttons)
        {
            toolbar = GetToolbar();
            buttons = toolbar != null ? ToolbarButtonsGetter.Value(toolbar) : null;
            return toolbar != null && buttons != null && buttons.Count > 0;
        }

        /// <summary>
        /// Detects if the game window or viewport has changed size since last frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckViewportChanged(Rectangle viewportBounds)
        {
            bool changed = viewportBounds.Width != CachedViewport.X || viewportBounds.Height != CachedViewport.Y;
            if (changed)
            {
                CachedViewport.X = viewportBounds.Width;
                CachedViewport.Y = viewportBounds.Height;
            }
            return changed;
        }

        /// <summary>
        /// Computes the full bounding box of the toolbar based on its button layout.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Rectangle GetToolbarBounds(IList<ClickableComponent> buttons)
        {
            int minX = buttons.Min(b => b.bounds.X);
            int maxX = buttons.Max(b => b.bounds.X);
            int minY = buttons.Min(b => b.bounds.Y);
            return new Rectangle(minX, minY, maxX - minX + 64, 64);
        }

        /// <summary>
        /// Determines whether to draw the spell bar above or below the toolbar
        /// based on the toolbar’s vertical position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldDrawAboveToolbar(Rectangle toolbarBounds, Rectangle viewport)
        {
            return toolbarBounds.Center.Y >= viewport.Center.Y;
        }

        /// <summary>
        /// Checks whether toolbar layout, viewport, or slot count changed.
        /// </summary>
        private static bool CheckToolbarChanged(Toolbar toolbar, Rectangle bounds, bool drawAbove, int slots, bool viewportChanged, bool CachedSpellMenuOpen)
        {
            return CachedToolbarRef != toolbar
                || bounds != CachedToolbarBounds
                || CachedDrawBarAboveToolbar != drawAbove
                || (CachedSpellSpots?.Length ?? 0) != slots
                || viewportChanged
                || CachedSpellMenuOpen;
        }

        /// <summary>
        /// Rebuilds spell bar layout coordinates relative to the toolbar.
        /// </summary>
        private static void RecalculateSpellBar(Toolbar toolbar, Rectangle toolbarBounds, bool drawAbove, int totalSlots)
        {
            int offsetX = ModEntry.Config.SpellBarOffset_X;
            int offsetY = ModEntry.Config.SpellBarOffset_Y;

            CachedSpellSpots = new Point[totalSlots];
            for (int i = 0; i < totalSlots; i++)
            {
                int x = toolbarBounds.Left + 60 * i + offsetX;
                int y = drawAbove ? toolbarBounds.Top - 72 - offsetY : toolbarBounds.Bottom + 24 + offsetY;
                CachedSpellSpots[i] = new Point(x, y);
            }

            CachedToolbarRef = toolbar;
            CachedToolbarBounds = toolbarBounds;
            CachedDrawBarAboveToolbar = drawAbove;
            CachedSpellMenuOpen = false;
            CachedHoverText = null;
        }

        /// <summary>
        /// Detects when the player switches to a different prepared spell bar.
        /// </summary>
        private static bool DetectSpellBarChange(PreparedSpellBar prepared)
        {
            bool changed = prepared != LastPreparedSpellBar;
            if (changed)
                LastPreparedSpellBar = prepared;
            return changed;
        }

        /// <summary>
        /// Builds a static cache of all visible spells (icons, tooltips, bounds).
        /// Called when the spell bar or layout changes.
        /// </summary>
        private static void RebuildStaticSpellCache(SpellBook book, PreparedSpellBar prepared, int totalSlots)
        {
            CachedStaticSpells = new StaticSpellDraw[totalSlots];
            for (int i = 0; i < totalSlots && i < prepared.Spells.Count; i++)
            {
                PreparedSpell prep = prepared.GetSlot(i);
                if (prep == null)
                    continue;

                Spell spell = SpellManager.Get(prep.SpellId);
                if (spell == null || spell.SpellLevels.Length <= prep.Level || spell.SpellLevels[prep.Level] == null)
                    continue;

                CachedStaticSpells[i] = new StaticSpellDraw
                {
                    Bounds = new Rectangle(CachedSpellSpots[i].X, CachedSpellSpots[i].Y, 50, 50),
                    Icon = spell.Icon,
                    LevelIcon = spell.SpellLevels[prep.Level],
                    Tooltip = spell.GetTooltip(prep.Level),
                    Spell = spell,
                    Level = prep.Level
                };
            }
        }

        /// <summary>
        /// Updates CanCast states every 5 frames to reflect mana or inventory changes.
        /// </summary>
        private static void UpdateCanCastCache(SpellBook book, int totalSlots)
        {
            FrameCounter++;
            if (FrameCounter % 5 != 0)
                return; // skip most frames to save CPU

            if (CachedStaticSpells == null)
                return;

            CachedCanCastStates = new bool[totalSlots];
            for (int i = 0; i < totalSlots && i < CachedStaticSpells.Length; i++)
            {
                var s = CachedStaticSpells[i];
                CachedCanCastStates[i] = s != null && book.CanCastSpell(s.Spell, s.Level);
            }
        }

        /// <summary>
        /// Draws the empty spell slot backgrounds.
        /// </summary>
        private static void DrawEmptySpellSlots(SpriteBatch b, int totalSlots)
        {
            for (int i = 0; i < totalSlots; i++)
                b.Draw(SpellBg, new Rectangle(CachedSpellSpots[i].X, CachedSpellSpots[i].Y, 50, 50), Color.White);
        }

        /// <summary>
        /// Draws each spell icon, applies color tint based on CanCast state,
        /// and handles tooltip display logic.
        /// </summary>
        private static void DrawSpellsAndHover(SpriteBatch b, SpellBook spellBook, int totalSlots, bool toolbarMoved, bool viewportChanged, bool spellBarChanged)
        {
            int mouseX = Game1.getOldMouseX();
            int mouseY = Game1.getOldMouseY();
            bool mouseMoved = mouseX != LastMouseX || mouseY != LastMouseY;
            LastMouseX = mouseX;
            LastMouseY = mouseY;

            string hoveredText = null;
            for (int i = 0; i < totalSlots && i < CachedStaticSpells.Length; i++)
            {
                var s = CachedStaticSpells[i];
                if (s == null)
                    continue;

                bool canCast = CachedCanCastStates != null && i < CachedCanCastStates.Length && CachedCanCastStates[i];
                Color drawColor = canCast ? Color.White : DisabledColor;

                // Draw the spell’s main icon and its level overlay
                b.Draw(s.Icon, s.Bounds, drawColor);
                b.Draw(s.LevelIcon, s.Bounds, drawColor);

                // Detect mouse hover for tooltip
                if (s.Bounds.Contains(mouseX, mouseY))
                    hoveredText = s.Tooltip;
            }

            // Update cached hover text only when needed
            if (mouseMoved || toolbarMoved || viewportChanged || spellBarChanged)
                CachedHoverText = hoveredText;

            // Draw tooltip below toolbar (the above-toolbar case is drawn in OnRenderedHud)
            if (CachedHoverText != null && !CachedDrawBarAboveToolbar)
                IClickableMenu.drawHoverText(b, CachedHoverText, Game1.smallFont);
        }





        /*********
        ** Interaction Handlers
        *********/


        internal static bool HandleMagicAltar(GameLocation location, string[] args, Farmer player, Microsoft.Xna.Framework.Point point)
        {
            OnAltarClicked();
            return true;
        }

        internal static bool HandleMagicRadio(GameLocation location, string[] args, Farmer player, Microsoft.Xna.Framework.Point point)
        {
            OnRadioClicked();
            return true;
        }

        private static void OnAltarClicked()
        {
            Log.Trace("Magic Altar clicked!");
            if (!LearnedMagic)
            {
                Log.Trace("Does not know Wizardry, not opening spell menu.");
                Game1.drawObjectDialogue(I18n.Altar_ClickMessage());
            }
            else
            {
                Log.Trace("Knows wizardry, can open spell menu");
                Game1.playSound("secret1");
                Game1.activeClickableMenu = new MagicMenu();
            }
        }

        private static void OnRadioClicked()
        {
            Game1.activeClickableMenu = new DialogueBox(GetRadioTextToday());
        }

        private static string GetRadioTextToday()
        {
            if (!LearnedMagic)
                return ModEntry.Instance.I18N.Get("radio.static");

            string baseKey = Regex.Replace(nameof(I18n.Radio_Analyzehints_1), "_1$", "");
            if (baseKey == nameof(I18n.Radio_Analyzehints_1))
            {
                Log.Error("Couldn't get the Magic radio station analyze hint base key. This is a bug in the Magic mod.");
                return ModEntry.Instance.I18N.Get("radio.static");
            }

            string[] stationTexts = typeof(I18n)
                .GetMethods()
                .Where(p => Regex.IsMatch(p.Name, $@"^{baseKey}_\d+$"))
                .Select(p => (string)p.Invoke(null, Array.Empty<object>()))
                .ToArray();

            Random random = new Random((int)Game1.stats.DaysPlayed + (int)(Game1.uniqueIDForThisGame / 2));
            return $"{I18n.Radio_Static()} {stationTexts[random.Next(stationTexts.Length)]}";
        }
    }
}
