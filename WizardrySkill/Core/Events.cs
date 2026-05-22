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
using MoonSharedSpaceCore;
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
        ** Fields
        *********/
        public static Texture2D SpellBg;
        private static Texture2D ManaBg;
        private static Texture2D ManaFg;
        private static IInputHelper InputHelper;
        private static bool CastPressed;
        private static bool IsInitialized;
        private static double CarryoverManaRegen;
        private const int SpellCastCooldownTicks = 30;
        private static int LastSpellCastTick = -9999;

        /// <summary>The active effects, spells, or projectiles which should be updated or drawn.</summary>
        private static readonly List<IActiveEffect> ActiveEffects = [];

        /// <summary>The self-updating views of magic metadata for each player.</summary>
        /// <remarks>This should only be accessed through <see cref="GetSpellBook"/> or <see cref="Extensions.GetSpellBook"/> to make sure an updated instance is retrieved.</remarks>
        private static readonly IDictionary<long, SpellBook> SpellBookCache = new Dictionary<long, SpellBook>();

        /// <summary>Color used when a spell slot cannot currently be cast.</summary>
        private static readonly Color DisabledColor = new(128, 128, 128);


        /*********
        ** Properties / Accessors
        *********/
        public static Wizard_Skill Skill = new();
        public static EventHandler<AnalyzeEventArgs> OnAnalyzeCast;

        /// <summary>Set by the spell menu when it affects the spell bar cache.</summary>
        public static bool CachedSpellMenuOpen
        {
            get => SpellBarState.CachedSpellMenuOpen;
            set => SpellBarState.CachedSpellMenuOpen = value;
        }

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
        ** Private helper models
        *********/
        /// <summary>Cached spell bar layout, hover, and can-cast state.</summary>
        private static class SpellBarState
        {
            public static Point CachedViewport;
            public static IClickableMenu CachedToolbarRef;
            public static Rectangle CachedToolbarBounds;
            public static bool CachedDrawBarAboveToolbar;
            public static bool CachedSpellMenuOpen;
            public static Point[] CachedSpellSpots;
            public static StaticSpellDraw[] CachedStaticSpells;
            public static PreparedSpellBar LastPreparedSpellBar;
            public static string CachedHoverText;
            public static int LastMouseX;
            public static int LastMouseY;
            public static int FrameCounter;
            public static bool[] CachedCanCastStates;

            public static void Reset()
            {
                CachedViewport = Point.Zero;
                CachedToolbarRef = null;
                CachedToolbarBounds = Rectangle.Empty;
                CachedDrawBarAboveToolbar = false;
                CachedSpellMenuOpen = false;
                CachedSpellSpots = null;
                CachedStaticSpells = null;
                LastPreparedSpellBar = null;
                CachedHoverText = null;
                LastMouseX = 0;
                LastMouseY = 0;
                FrameCounter = 0;
                CachedCanCastStates = null;
            }
        }

        /// <summary>Static spell data cache used by the spell bar renderer.</summary>
        private sealed class StaticSpellDraw
        {
            public Rectangle Bounds;
            public Texture2D Icon;
            public Texture2D LevelIcon;
            public string Tooltip;
            public Spell Spell;
            public int Level;
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
            ClearActiveEffects();
            SpellBookCache.Clear();
            ResetSpellBarCache();
            SummonManager.Reset();
            NetworkEvents.Reset();
            LastSpellCastTick = -9999;
            CastPressed = false;
        }

        [SEvent.SaveLoaded]
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            ResetSpellBarCache();

            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                if (farmer.eventsSeen.Contains("90001") && !farmer.mailReceived.Contains("moonslimeWizardryLearnedMagic"))
                    farmer.mailReceived.Add("moonslimeWizardryLearnedMagic");

                farmer.modData["moonslime.Wizardry.scrollspell"] = "no";
            }

            Farmer player = Game1.player;

            SpaceUtilities.LearnRecipesOnLoad(player, "moonslime.Wizard");
            AddKnownSpellCraftingRecipes(player);

            SpellBg = CopySprite(Game1.mouseCursors, new Rectangle(293, 360, 24, 24));

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
            LastSpellCastTick = -9999;
            SummonManager.OnDayStarted();

            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID) ?? Game1.player;

            // fix player's magic info if needed
            FixMagicIfNeeded(player);
        }

        [SEvent.DayEnding]
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            SummonManager.OnDayEnding();
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
            if (IsInitialized)
                return;

            IsInitialized = true;
            InputHelper = inputHelper;

            LoadAssets();
            SpellManager.Init(getNewId);
            NetworkEvents.Init();

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

            SummonManager.Update(e);
            UpdateActiveEffects(e);
        }

        /// <summary>Add an active spell effect to the shared update/draw list.</summary>
        /// <param name="effect">The effect returned by a spell, or <c>null</c> if the spell has no active effect.</param>
        internal static void AddActiveEffect(IActiveEffect effect)
        {
            if (effect != null)
                ActiveEffects.Add(effect);
        }

        [SEvent.ButtonPressed]
        public static void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            Farmer player = Game1.player;
            bool hasMenuOpen = Game1.activeClickableMenu is not null;

            if (e.Button == ModEntry.Config.Key_Cast)
                CastPressed = true;

            if (CastPressed && e.Button == ModEntry.Config.Key_SwapSpells && !hasMenuOpen)
            {
                player.GetSpellBook().SwapPreparedSet();
                InputHelper.Suppress(e.Button);
                return;
            }

            if (CastPressed && IsSpellSlotButton(e.Button, GetAvailableSpellSlotCount(player), out int slotIndex))
            {
                InputHelper.Suppress(e.Button);
                TryCastPreparedSpell(slotIndex, hasMenuOpen);
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
            if (!e.IsLocalPlayer)
                return;

            EvacSpell.OnLocationChanged();
            SummonManager.OnLocalWarped(e);
            if (e.NewLocation.IsOutdoors && !e.Player.modData.ContainsKey("moonslime.Wizardry.TeleportTo." + e.NewLocation.Name))
                e.Player.modData.Add("moonslime.Wizardry.TeleportTo." + e.NewLocation.Name, "");
        }

        public static SpellBook GetSpellBook(Farmer player)
        {
            if (!SpellBookCache.TryGetValue(player.UniqueMultiplayerID, out SpellBook book) || !object.ReferenceEquals(player, book.Player))
                SpellBookCache[player.UniqueMultiplayerID] = book = new SpellBook(player);

            return book;
        }

        public static void FixMagicIfNeeded(Farmer player, int? overrideMagicLevel = null, bool fixMana = false)
        {
            if (player == null)
                return;

            if (!LearnedMagic && overrideMagicLevel is not > 0)
                return;

            int magicLevel = overrideMagicLevel ?? player.GetCustomSkillLevel("moonslime.Wizard");
            SpellBook spellBook = player.GetSpellBook();

            // TODO: Restore mana recalculation here if this flag is still needed.
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
                    Log.Trace($"Player now has Profession mod data: {modDataKey}");
                }
            }

            foreach (string spellId in CoreSpells)
            {
                if (!spellBook.KnowsSpell(spellId, 0))
                    spellBook.LearnSpell(spellId, 0, true);
            }
        }


        /*********
        ** Spell casting
        *********/
        /// <summary>Try to cast a spell from the prepared spell bar.</summary>
        private static void TryCastPreparedSpell(int slotIndex, bool hasMenuOpen)
        {
            Farmer player = Game1.player;
            SpellBook spellBook = player.GetSpellBook();
            PreparedSpellBar prepared = spellBook.GetPreparedSpells();
            PreparedSpell slot = prepared?.GetSlot(slotIndex);
            if (slot == null)
                return;

            Spell spell = SpellManager.Get(slot.SpellId);
            if (spell == null)
                return;

            bool canCast = spellBook.CanCastSpell(spell, slot.Level) && (!hasMenuOpen || spell.CanCastInMenus);
            if (!canCast)
                return;

            if (!IsSpellCastCooldownReady())
                return;

            LastSpellCastTick = Game1.ticks;

            Log.Trace($"Casting {slot.SpellId} with sync mode {spell.SyncMode}");

            player.AddMana(-spell.GetManaCost(player, slot.Level));
            player.modData["moonslime.Wizardry.scrollspell"] = "no";

            Point pos = new Point(Game1.getMouseX() + Game1.viewport.X, Game1.getMouseY() + Game1.viewport.Y);
            NetworkEvents.DispatchSpellCast(player, spellBook, spell, slot.Level, pos);
        }

        /// <summary>Map configured spell buttons to a prepared spell slot index.</summary>
        private static bool IsSpellSlotButton(SButton button, int availableSlots, out int slotIndex)
        {
            slotIndex = 0;

            if (button == ModEntry.Config.Key_Spell1)
                return availableSlots >= 1;

            if (button == ModEntry.Config.Key_Spell2)
            {
                slotIndex = 1;
                return availableSlots >= 2;
            }

            if (button == ModEntry.Config.Key_Spell3)
            {
                slotIndex = 2;
                return availableSlots >= 3;
            }

            if (button == ModEntry.Config.Key_Spell4)
            {
                slotIndex = 3;
                return availableSlots >= 4;
            }

            if (button == ModEntry.Config.Key_Spell5)
            {
                slotIndex = 4;
                return availableSlots >= 5;
            }

            return false;
        }

        private static bool IsSpellCastCooldownReady()
        {
            return Game1.ticks - LastSpellCastTick >= SpellCastCooldownTicks;
        }

        private static int GetAvailableSpellSlotCount(Farmer player)
        {
            return player.HasCustomProfession(Wizard_Skill.Magic10a2) ? 5 : 4;
        }


        /*********
        ** Active effects
        *********/
        /// <summary>Update all active spell effects, cleaning them up before removal.</summary>
        /// <param name="e">The update tick event args.</param>
        private static void UpdateActiveEffects(UpdateTickedEventArgs e)
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                IActiveEffect effect = ActiveEffects[i];
                if (effect == null)
                {
                    ActiveEffects.RemoveAt(i);
                    continue;
                }

                if (!effect.Update(e))
                    RemoveActiveEffectAt(i);
            }
        }

        /// <summary>Clear all active spell effects and call cleanup on each one.</summary>
        private static void ClearActiveEffects()
        {
            foreach (IActiveEffect effect in ActiveEffects)
                effect?.CleanUp();

            ActiveEffects.Clear();
        }

        /// <summary>Remove an active effect after safely cleaning it up.</summary>
        /// <param name="index">The active effect index.</param>
        private static void RemoveActiveEffectAt(int index)
        {
            ActiveEffects[index]?.CleanUp();
            ActiveEffects.RemoveAt(index);
        }

        /// <summary>
        /// Draws all currently active spell effects (visual overlays or auras).
        /// </summary>
        private static void DrawActiveEffects(SpriteBatch b)
        {
            foreach (IActiveEffect effect in ActiveEffects)
                effect?.Draw(b);

            SummonManager.Draw(b);
        }


        /*********
        ** HUD rendering
        *********/
        [SEvent.RenderingHud]
        private static void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // Skip drawing if menus are open, events are active, or the player can't act
            if (!LearnedMagic || !Context.IsPlayerFree || Game1.farmEvent != null || Game1.displayHUD == false)
                return;

            SpriteBatch b = e.SpriteBatch;
            Rectangle viewport = Game1.graphics.GraphicsDevice.Viewport.Bounds;

            // 1. Draw all active visual spell effects (e.g. ongoing auras)
            DrawActiveEffects(b);

            // 2. Try to get toolbar info (toolbar instance + button list)
            if (!TryGetToolbarInfo(out Toolbar toolbar, out IList<ClickableComponent> buttons))
                return; // toolbar not ready — skip drawing

            // Determine number of available spell slots
            int totalSlots = GetAvailableSpellSlotCount(Game1.player);

            // Detect any changes that require position recalculation
            bool viewportChanged = CheckViewportChanged(viewport);
            Rectangle toolbarBounds = GetToolbarBounds(buttons);
            bool drawBarAboveToolbar = ShouldDrawAboveToolbar(toolbarBounds, viewport);
            bool toolbarMoved = CheckToolbarChanged(toolbar, toolbarBounds, drawBarAboveToolbar, totalSlots, viewportChanged, SpellBarState.CachedSpellMenuOpen);

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
            if (SpellBarState.CachedStaticSpells == null || spellBarChanged || toolbarMoved)
                RebuildStaticSpellCache(prepared, totalSlots);

            // Refresh CanCast states every few frames (for mana/inventory updates)
            UpdateCanCastCache(spellBook, totalSlots);

            // Draw empty background slots
            DrawEmptySpellSlots(b, totalSlots);

            // Draw actual spells + hover text
            DrawSpellsAndHover(b, totalSlots, toolbarMoved, viewportChanged, spellBarChanged);
        }

        [SEvent.RenderedHud]
        private static void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Skip if player isn't in active control
            if (Game1.activeClickableMenu != null || Game1.eventUp || !LearnedMagic || !Context.IsPlayerFree)
                return;

            // Draw hover tooltip *above* the toolbar (if applicable)
            if (SpellBarState.CachedHoverText != null && SpellBarState.CachedDrawBarAboveToolbar)
                IClickableMenu.drawHoverText(e.SpriteBatch, SpellBarState.CachedHoverText, Game1.smallFont);
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
        /// Gets the on-screen toolbar if one is currently being drawn.
        /// </summary>
        private static Toolbar? GetToolbar()
        {
            return Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
        }

        /// <summary>
        /// Detects if the game window or viewport has changed size since last frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckViewportChanged(Rectangle viewportBounds)
        {
            bool changed = viewportBounds.Width != SpellBarState.CachedViewport.X || viewportBounds.Height != SpellBarState.CachedViewport.Y;
            if (changed)
            {
                SpellBarState.CachedViewport.X = viewportBounds.Width;
                SpellBarState.CachedViewport.Y = viewportBounds.Height;
            }
            return changed;
        }

        /// <summary>
        /// Computes the full bounding box of the toolbar based on its button layout.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Rectangle GetToolbarBounds(IList<ClickableComponent> buttons)
        {
            Rectangle first = buttons[0].bounds;
            int minX = first.X;
            int maxX = first.X;
            int minY = first.Y;

            for (int i = 1; i < buttons.Count; i++)
            {
                Rectangle bounds = buttons[i].bounds;
                if (bounds.X < minX)
                    minX = bounds.X;
                if (bounds.X > maxX)
                    maxX = bounds.X;
                if (bounds.Y < minY)
                    minY = bounds.Y;
            }

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
        private static bool CheckToolbarChanged(Toolbar toolbar, Rectangle bounds, bool drawAbove, int slots, bool viewportChanged, bool cachedSpellMenuOpen)
        {
            return SpellBarState.CachedToolbarRef != toolbar
                || bounds != SpellBarState.CachedToolbarBounds
                || SpellBarState.CachedDrawBarAboveToolbar != drawAbove
                || (SpellBarState.CachedSpellSpots?.Length ?? 0) != slots
                || viewportChanged
                || cachedSpellMenuOpen;
        }

        /// <summary>
        /// Rebuilds spell bar layout coordinates relative to the toolbar.
        /// </summary>
        private static void RecalculateSpellBar(Toolbar toolbar, Rectangle toolbarBounds, bool drawAbove, int totalSlots)
        {
            int offsetX = ModEntry.Config.SpellBarOffset_X;
            int offsetY = ModEntry.Config.SpellBarOffset_Y;

            SpellBarState.CachedSpellSpots = new Point[totalSlots];
            for (int i = 0; i < totalSlots; i++)
            {
                int x = toolbarBounds.Left + 60 * i + offsetX;
                int y = drawAbove ? toolbarBounds.Top - 72 - offsetY : toolbarBounds.Bottom + 24 + offsetY;
                SpellBarState.CachedSpellSpots[i] = new Point(x, y);
            }

            SpellBarState.CachedToolbarRef = toolbar;
            SpellBarState.CachedToolbarBounds = toolbarBounds;
            SpellBarState.CachedDrawBarAboveToolbar = drawAbove;
            SpellBarState.CachedSpellMenuOpen = false;
            SpellBarState.CachedHoverText = null;
        }

        /// <summary>
        /// Detects when the player switches to a different prepared spell bar.
        /// </summary>
        private static bool DetectSpellBarChange(PreparedSpellBar prepared)
        {
            bool changed = prepared != SpellBarState.LastPreparedSpellBar;
            if (changed)
                SpellBarState.LastPreparedSpellBar = prepared;
            return changed;
        }

        /// <summary>
        /// Builds a static cache of all visible spells (icons, tooltips, bounds).
        /// Called when the spell bar or layout changes.
        /// </summary>
        private static void RebuildStaticSpellCache(PreparedSpellBar prepared, int totalSlots)
        {
            SpellBarState.CachedStaticSpells = new StaticSpellDraw[totalSlots];
            for (int i = 0; i < totalSlots && i < prepared.Spells.Count; i++)
            {
                PreparedSpell prep = prepared.GetSlot(i);
                if (prep == null)
                    continue;

                Spell spell = SpellManager.Get(prep.SpellId);
                if (spell == null || spell.SpellLevels.Length <= prep.Level || spell.SpellLevels[prep.Level] == null)
                    continue;

                SpellBarState.CachedStaticSpells[i] = new StaticSpellDraw
                {
                    Bounds = new Rectangle(SpellBarState.CachedSpellSpots[i].X, SpellBarState.CachedSpellSpots[i].Y, 50, 50),
                    Icon = spell.Icon,
                    LevelIcon = spell.SpellLevels[prep.Level],
                    Tooltip = spell.GetTooltip(prep.Level),
                    Spell = spell,
                    Level = prep.Level
                };
            }

            SpellBarState.CachedCanCastStates = null;
        }

        /// <summary>
        /// Updates CanCast states every 5 frames to reflect mana, inventory, and cooldown updates.
        /// </summary>
        private static void UpdateCanCastCache(SpellBook book, int totalSlots)
        {
            SpellBarState.FrameCounter++;
            if (SpellBarState.FrameCounter % 5 != 0 && SpellBarState.CachedCanCastStates != null)
                return; // skip most frames to save CPU

            if (SpellBarState.CachedStaticSpells == null)
                return;

            bool cooldownReady = IsSpellCastCooldownReady();

            SpellBarState.CachedCanCastStates = new bool[totalSlots];
            for (int i = 0; i < totalSlots && i < SpellBarState.CachedStaticSpells.Length; i++)
            {
                StaticSpellDraw s = SpellBarState.CachedStaticSpells[i];
                SpellBarState.CachedCanCastStates[i] = s != null && cooldownReady && book.CanCastSpell(s.Spell, s.Level);
            }
        }

        /// <summary>
        /// Draws the empty spell slot backgrounds.
        /// </summary>
        private static void DrawEmptySpellSlots(SpriteBatch b, int totalSlots)
        {
            for (int i = 0; i < totalSlots; i++)
                b.Draw(SpellBg, new Rectangle(SpellBarState.CachedSpellSpots[i].X, SpellBarState.CachedSpellSpots[i].Y, 50, 50), Color.White);
        }

        /// <summary>
        /// Draws each spell icon, applies color tint based on CanCast state,
        /// and handles tooltip display logic.
        /// </summary>
        private static void DrawSpellsAndHover(SpriteBatch b, int totalSlots, bool toolbarMoved, bool viewportChanged, bool spellBarChanged)
        {
            int mouseX = Game1.getOldMouseX();
            int mouseY = Game1.getOldMouseY();
            bool mouseMoved = mouseX != SpellBarState.LastMouseX || mouseY != SpellBarState.LastMouseY;
            SpellBarState.LastMouseX = mouseX;
            SpellBarState.LastMouseY = mouseY;

            string hoveredText = null;
            for (int i = 0; i < totalSlots && i < SpellBarState.CachedStaticSpells.Length; i++)
            {
                StaticSpellDraw s = SpellBarState.CachedStaticSpells[i];
                if (s == null)
                    continue;

                bool canCast = SpellBarState.CachedCanCastStates != null && i < SpellBarState.CachedCanCastStates.Length && SpellBarState.CachedCanCastStates[i];
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
                SpellBarState.CachedHoverText = hoveredText;

            // Draw tooltip below toolbar (the above-toolbar case is drawn in OnRenderedHud)
            if (SpellBarState.CachedHoverText != null && !SpellBarState.CachedDrawBarAboveToolbar)
                IClickableMenu.drawHoverText(b, SpellBarState.CachedHoverText, Game1.smallFont);
        }

        /// <summary>Reset all cached spell bar layout, hover, and can-cast state.</summary>
        private static void ResetSpellBarCache()
        {
            SpellBarState.Reset();
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


        /*********
        ** Save-load helpers
        *********/
        /// <summary>Add known-spell crafting recipes to the player on save load.</summary>
        /// <param name="player">The local player.</param>
        private static void AddKnownSpellCraftingRecipes(Farmer player)
        {
            SpellBook book = player.GetSpellBook();
            IDictionary<string, string> craftingRecipes = DataLoader.CraftingRecipes(Game1.content);

            foreach (var knownSpell in book.KnownSpells)
            {
                string spellDefId = knownSpell.Key;
                foreach (KeyValuePair<string, string> recipePair in craftingRecipes)
                {
                    string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                    if (!conditions.Contains(spellDefId))
                        continue;

                    string[] conditionParts = conditions.Split(' ');
                    if (conditionParts.Length < 2)
                        continue;

                    player.craftingRecipes.TryAdd(recipePair.Key, 0);
                }
            }
        }


        /*********
        ** Texture helpers
        *********/
        public static Texture2D CopySprite(Texture2D sourceTexture, Rectangle sourceRect)
        {
            // 1. Read pixel data from the source
            Color[] data = new Color[sourceRect.Width * sourceRect.Height];
            sourceTexture.GetData(0, sourceRect, data, 0, data.Length);

            // 2. Create a new texture with the exact size of the sprite
            Texture2D newTexture = new Texture2D(Game1.graphics.GraphicsDevice, sourceRect.Width, sourceRect.Height);

            // 3. Write the copied pixels into the new texture
            newTexture.SetData(data);

            return newTexture;
        }
    }
}
