using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using WizardrySkill.Objects;
using SpaceCore.Events;
using MoonShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using SpaceCore;
using System.Text.RegularExpressions;
using WizardrySkill.Framework.Game.Interface;
using Microsoft.Xna.Framework;
using StardewValley.Network;
using Log = BirbCore.Attributes.Log;
using MoonShared;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core
{
    [SEvent]
    public class Events
    {
        /*********
        ** Fields
        *********/
        private static Texture2D SpellBg;
        private static Texture2D ManaBg;
        private static Texture2D ManaFg;
        private static IInputHelper InputHelper;
        private static bool CastPressed;
        private static double CarryoverManaRegen;
        private static Toolbar? GetToolbar() => Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();

        /// <summary>The active effects, spells, or projectiles which should be updated or drawn.</summary>
        private static readonly IList<IActiveEffect> ActiveEffects = new List<IActiveEffect>();

        /// <summary>The self-updating views of magic metadata for each player.</summary>
        /// <remarks>This should only be accessed through <see cref="GetSpellBook"/> or <see cref="Extensions.GetSpellBook"/> to make sure an updated instance is retrieved.</remarks>
        private static readonly IDictionary<long, SpellBook> SpellBookCache = new Dictionary<long, SpellBook>();


        /*********
        ** Accessors
        *********/
        public static Wizard_Skill Skill = new();
        public static EventHandler<AnalyzeEventArgs> OnAnalyzeCast;
        public const string MsgCast = "spacechase0.Magic.Cast";

        private static readonly Lazy<Func<Toolbar, List<ClickableComponent>>> ToolbarButtonsGetter = new(() => AccessTools.DeclaredField(typeof(Toolbar), "buttons").EmitInstanceGetter<Toolbar, List<ClickableComponent>>());


        /// <summary>Whether the current player learned magic.</summary>
        public static bool LearnedMagic => Game1.player?.eventsSeen?.Contains(MagicConstants.LearnedMagicEventId.ToString()) == true;

        public static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ModEntry.Editor = new(ModEntry.Config, ModEntry.Instance.Helper.ModContent, ModEntry.HasStardewValleyExpanded);

        }

        [SEvent.GameLaunchedLate]
        public static void GameLaunchedLate(object sender, GameLaunchedEventArgs e)
        {
            ModEntry.Editor = new(ModEntry.Config, ModEntry.Instance.Helper.ModContent, ModEntry.HasStardewValleyExpanded);

            // hook Mana Bar
            {
                var manaBar = ModEntry.Instance.Helper.ModRegistry.GetApi<IManaBarApi>("moonslime.ManaBarApi");
                if (manaBar == null)
                {
                    Log.Error("No mana bar API???");
                    return;
                }
                ModEntry.Mana = manaBar;
            }

            var helper = ModEntry.Instance.Helper;
            Log.Trace("Magic: Trying to Register skill.");
            Init(helper.Events, helper.Input, helper.ModRegistry, helper.Multiplayer.GetNewID);
        }

        /*********
        ** Public methods
        *********/
        public static void Init(IModEvents events, IInputHelper inputHelper, IModRegistry modRegistry, Func<long> getNewId)
        {
            InputHelper = inputHelper;

            LoadAssets();

            SpellManager.Init(getNewId);

            SpaceEvents.OnItemEaten += OnItemEaten;
            Networking.RegisterMessageHandler(MsgCast, OnNetworkCast);

            OnAnalyzeCast += (sender, e) => ModEntry.Instance.Api.InvokeOnAnalyzeCast(sender as Farmer);

            SpaceCore.Skills.RegisterSkill(Skill);
        }

        public static void LoadAssets()
        {
            SpellBg = ModEntry.Assets.Spellbg;
            ManaBg = ModEntry.Assets.Manabg;

            Color manaCol = new Color(0, 48, 255);
            ManaFg = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            ManaFg.SetData(new[] { manaCol });
        }


        [SEvent.AssetRequested]
        public static void AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            ModEntry.Editor.TryEdit(e);
        }

        [SEvent.UpdateTicked]
        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // update active effects
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                IActiveEffect effect = ActiveEffects[i];
                if (!effect.Update(e))
                    ActiveEffects.RemoveAt(i);
            }
        }

        [SEvent.ButtonPressed]
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            bool hasFifthSpellSlot = Game1.player.HasCustomProfession(Wizard_Skill.Magic10a2);
            bool hasMenuOpen = Game1.activeClickableMenu is not null;

            if (e.Button == ModEntry.Config.Key_Cast)
                CastPressed = true;

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

                SpellBook spellBook = Game1.player.GetSpellBook();

                PreparedSpellBar prepared = spellBook.GetPreparedSpells();
                PreparedSpell slot = prepared?.GetSlot(slotIndex);
                if (slot == null)
                    return;

                Spell spell = SpellManager.Get(slot.SpellId);
                if (spell == null)
                    return;

                bool canCast =
                    spellBook.CanCastSpell(spell, slot.Level)
                    && (!hasMenuOpen || spell.CanCastInMenus);

                if (canCast)
                {
                    Log.Trace("Casting " + slot.SpellId);

                    IActiveEffect effect = spellBook.CastSpell(spell, slot.Level);
                    if (effect != null)
                        ActiveEffects.Add(effect);
                    Game1.player.AddMana(-spell.GetManaCost(Game1.player, slot.Level));
                }
            }
        }

        [SEvent.ButtonReleased]
        /// <summary>Raised after the player releases a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button == ModEntry.Config.Key_Cast)
            {
                CastPressed = false;
            }
        }

        [SEvent.TimeChanged]
        public static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            int level = Game1.player.GetCustomSkillLevel(Skill);
            double manaRegen = (level + 1) / 2 + CarryoverManaRegen; // start at +1 mana at level 1
            if (Game1.player.HasCustomProfession(Wizard_Skill.Magic10b1))
                manaRegen += level * 1;
            if (Game1.player.HasCustomProfession(Wizard_Skill.Magic5b))
                manaRegen += level * 0.5;

            Game1.player.AddMana((int)manaRegen);
            CarryoverManaRegen = manaRegen - (int)manaRegen;
        }

        [SEvent.Warped]
        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnWarped(object sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;

            // update spells
            EvacSpell.OnLocationChanged();

            int positionModifier = ModEntry.HasStardewValleyExpanded ? 17 : 5;

            // check events
            if (e.NewLocation.Name == "WizardHouse" && !LearnedMagic && Game1.player.friendshipData.TryGetValue("Wizard", out Friendship wizardFriendship) && wizardFriendship.Points >= 750)
            {
                string eventStr = "WizardSong/0 " + positionModifier + "/Wizard 8 " + positionModifier + " 0 farmer 8 " + (positionModifier + 10) + " 0/skippable/ignoreCollisions farmer/move farmer 0 -8 0/speak Wizard \"{0}#$b#{1}#$b#{2}#$b#{3}#$b#{4}#$b#{5}#$b#{6}#$b#{7}#$b#{8}\"/textAboveHead Wizard \"{9}\"/pause 750/fade 750/end";
                eventStr = string.Format(
                    eventStr,
                    I18n.Event_Wizard_1(),
                    I18n.Event_Wizard_2(),
                    I18n.Event_Wizard_3(),
                    I18n.Event_Wizard_4(),
                    I18n.Event_Wizard_5(),
                    I18n.Event_Wizard_6(),
                    I18n.Event_Wizard_7(),
                    I18n.Event_Wizard_8(),
                    I18n.Event_Wizard_9(),
                    I18n.Event_Wizard_Abovehead()
                );
                e.NewLocation.currentEvent = new Event(eventStr, null, MagicConstants.LearnedMagicEventId.ToString());
                Game1.eventUp = true;
                Game1.displayHUD = false;
                Game1.player.CanMove = false;
                Game1.player.showNotCarrying();
                Core.Utilities.AddEXP(Game1.player, Skill.ExperienceCurve[0]);
                FixMagicIfNeeded(Game1.player, overrideMagicLevel: 1); // let player start using WizardrySkill immediately
                Game1.player.eventsSeen.Add(MagicConstants.LearnedMagicEventId.ToString());
            }
        }

        /// <summary>Get a self-updating view of a player's magic metadata.</summary>
        /// <param name="player">The player whose spell book to get.</param>
        public static SpellBook GetSpellBook(Farmer player)
        {
            if (!SpellBookCache.TryGetValue(player.UniqueMultiplayerID, out SpellBook book) || !object.ReferenceEquals(player, book.Player))
                SpellBookCache[player.UniqueMultiplayerID] = book = new SpellBook(player);

            return book;
        }

        /// <summary>Fix the player's magic spells and mana pool to match their skill level if needed.</summary>
        /// <param name="player">The player to fix.</param>
        /// <param name="overrideMagicLevel">The magic skill level, or <c>null</c> to get it from the player.</param>
        public static void FixMagicIfNeeded(Farmer player, int? overrideMagicLevel = null)
        {
            // skip if player hasn't learned magic
            if (!LearnedMagic && overrideMagicLevel is not > 0)
                return;


            // get magic info
            int magicLevel = overrideMagicLevel ?? player.GetCustomSkillLevel("moonslime.Wizard");
            SpellBook spellBook = player.GetSpellBook();

            // fix mana pool
            int expectedPoints = magicLevel * MagicConstants.ManaPointsPerLevel + 100;
            if (player.HasCustomProfession(Wizard_Skill.Magic10b2))
            {
                expectedPoints += 100;
            }
            player.SetMaxMana(expectedPoints);
            player.SetManaToMax();
            if (((int)(player.Stamina)) != player.MaxStamina)
            {
                player.AddMana((int)(player.GetMaxMana() * -0.5));
            }

            // fix spell bars
            if (spellBook.Prepared.Count < MagicConstants.SpellBarCount)
            {
                spellBook.Mutate(data =>
                {
                    while (spellBook.Prepared.Count < MagicConstants.SpellBarCount)
                        data.Prepared.Add(new PreparedSpellBar());
                });
            }

            // fix learned spells
            foreach (string spellId in new[] { "arcane:analyze", "arcane:magicmissle", "arcane:enchant", "arcane:disenchant" })
                spellBook.LearnSpell(spellId, 0, true);
        }


        /*********
        ** Private methods
        *********/
        private static void OnNetworkCast(IncomingMessage msg)
        {
            Farmer player = Game1.GetPlayer(msg.FarmerID);
            if (player == null)
                return;

            IActiveEffect effect = player.GetSpellBook().CastSpell(msg.Reader.ReadString(), msg.Reader.ReadInt32(), msg.Reader.ReadInt32(), msg.Reader.ReadInt32());
            if (effect != null)
                ActiveEffects.Add(effect);
        }



        [SEvent.RenderingHud]
        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear under the HUD.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw active effects
            foreach (IActiveEffect effect in ActiveEffects)
                effect.Draw(e.SpriteBatch);

            if (Game1.activeClickableMenu != null || Game1.eventUp || !LearnedMagic || !Context.IsPlayerFree)
                return;

            SpriteBatch b = e.SpriteBatch;

            bool hasFifthSpellSlot = Game1.player.HasCustomProfession(Wizard_Skill.Magic10a2);

            var toolbar = GetToolbar();
            if (toolbar is null)
                return;

            var buttons = ToolbarButtonsGetter.Value(toolbar);
            int toolbarMinX = buttons.Select(b => b.bounds.X).Min();
            int toolbarMaxX = buttons.Select(b => b.bounds.X).Max();
            int toolbarMinY = buttons.Select(b => b.bounds.Y).Min();
            Rectangle toolbarBounds = new(toolbarMinX, toolbarMinY, toolbarMaxX - toolbarMinX + 64, 64);
            var viewportBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
            bool drawBarAboveToolbar = toolbarBounds.Center.Y >= viewportBounds.Center.Y;

            Point[] spots =
            {
                new((int)toolbarBounds.Left + 60 * ( 0 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 1 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 2 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 3 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 4 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24)
            };

            // read spell info
            SpellBook spellBook = Game1.player.GetSpellBook();
            PreparedSpellBar prepared = spellBook.GetPreparedSpells();
            if (prepared == null)
                return;

            // render empty spell slots
            for (int i = 0; i < (hasFifthSpellSlot ? 5 : 4); ++i)
                b.Draw(SpellBg, new Rectangle(spots[i].X, spots[i].Y, 50, 50), Color.White);


            // render spell bar
            string hoveredText = null;
            for (int i = 0; i < (hasFifthSpellSlot ? 5 : 4) && i < prepared.Spells.Count; ++i)
            {
                PreparedSpell prep = prepared.GetSlot(i);
                if (prep == null)
                    continue;

                Spell spell = SpellManager.Get(prep.SpellId);
                if (spell == null || spell.Icons.Length <= prep.Level || spell.Icons[prep.Level] == null)
                    continue;

                Rectangle bounds = new Rectangle(spots[i].X, spots[i].Y, 50, 50);

                b.Draw(spell.Icons[prep.Level], bounds, spellBook.CanCastSpell(spell, prep.Level) ? Color.White : new Color(128, 128, 128));
                if (bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
                    hoveredText = spell.GetTooltip(level: prep.Level);
            }

            // render hover text
            if (hoveredText != null && drawBarAboveToolbar == false)
                StardewValley.Menus.IClickableMenu.drawHoverText(b, hoveredText, Game1.smallFont);
        }


        [SEvent.RenderedHud]
        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {

            // read spell info
            SpellBook spellBook = Game1.player.GetSpellBook();
            PreparedSpellBar prepared = spellBook.GetPreparedSpells();
            if (prepared == null)
                return;

            var toolbar = GetToolbar();
            if (toolbar is null)
                return;

            var buttons = ToolbarButtonsGetter.Value(toolbar);
            int toolbarMinX = buttons.Select(b => b.bounds.X).Min();
            int toolbarMaxX = buttons.Select(b => b.bounds.X).Max();
            int toolbarMinY = buttons.Select(b => b.bounds.Y).Min();
            Rectangle toolbarBounds = new(toolbarMinX, toolbarMinY, toolbarMaxX - toolbarMinX + 64, 64);
            var viewportBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
            bool drawBarAboveToolbar = toolbarBounds.Center.Y >= viewportBounds.Center.Y;

            bool hasFifthSpellSlot = Game1.player.HasCustomProfession(Wizard_Skill.Magic10a2);
            Point[] spots =
{
                new((int)toolbarBounds.Left + 60 * ( 0 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 1 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 2 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 3 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24),
                new((int)toolbarBounds.Left + 60 * ( 4 ), drawBarAboveToolbar ? toolbarBounds.Top - 72 : toolbarBounds.Bottom + 24)
            };

            string hoveredText = null;
            for (int i = 0; i < (hasFifthSpellSlot ? 5 : 4) && i < prepared.Spells.Count; ++i)
            {
                PreparedSpell prep = prepared.GetSlot(i);
                if (prep == null)
                    continue;

                Spell spell = SpellManager.Get(prep.SpellId);
                if (spell == null || spell.Icons.Length <= prep.Level || spell.Icons[prep.Level] == null)
                    continue;

                Rectangle bounds = new Rectangle(spots[i].X, spots[i].Y, 50, 50);

                if (bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
                    hoveredText = spell.GetTooltip(level: prep.Level);
            }

            SpriteBatch b = e.SpriteBatch;
            // render hover text
            if (hoveredText != null && drawBarAboveToolbar == true)
                StardewValley.Menus.IClickableMenu.drawHoverText(b, hoveredText, Game1.smallFont);
        }

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

        /// <summary>Handle an interaction with the magic altar.</summary>
        private static void OnAltarClicked()
        {
            if (!LearnedMagic)
                Game1.drawObjectDialogue(I18n.Altar_ClickMessage());
            else
            {
                Game1.playSound("secret1");
                Game1.activeClickableMenu = new MagicMenu();
            }
        }

        /// <summary>Handle an interaction with the magic radio.</summary>
        private static void OnRadioClicked()
        {
            Game1.activeClickableMenu = new DialogueBox(GetRadioTextToday());
        }

        /// <summary>Get the radio station text to play today.</summary>
        private static string GetRadioTextToday()
        {
            // player doesn't know magic
            if (!LearnedMagic)
                return I18n.Radio_Static();

            // get base key for random hints
            string baseKey = Regex.Replace(nameof(I18n.Radio_Analyzehints_1), "_1$", "");
            if (baseKey == nameof(I18n.Radio_Analyzehints_1))
            {
                Log.Error("Couldn't get the Magic radio station analyze hint base key. This is a bug in the Magic mod."); // key format changed?
                return I18n.Radio_Static();
            }

            // choose random hint
            string[] stationTexts = typeof(I18n)
                .GetMethods()
                .Where(p => Regex.IsMatch(p.Name, $@"^{baseKey}_\d+$"))
                .Select(p => (string)p.Invoke(null, Array.Empty<object>()))
                .ToArray();
            Random random = new Random((int)Game1.stats.DaysPlayed + (int)(Game1.uniqueIDForThisGame / 2));
            return $"{I18n.Radio_Static()} {stationTexts[random.Next(stationTexts.Length)]}";
        }

        private static void OnItemEaten(object sender, EventArgs args)
        {
            if (Game1.player.itemToEat == null)
            {
                Log.Warn("No item eaten for the item eat event?!?");
                return;
            }
            if (Game1.player.itemToEat.HasContextTag("moonslime.Wizardry.mana_potion_full"))
                Game1.player.AddMana(Game1.player.GetMaxMana());

            if (Game1.player.itemToEat.HasContextTag("moonslime.Wizardry.mana_potion_half"))
                Game1.player.AddMana(Game1.player.GetMaxMana() / 2);


            if (Game1.player.itemToEat.HasContextTag("moonslime.Wizardry.mana_potion_4th"))
                Game1.player.AddMana(Game1.player.GetMaxMana() / 4);

            if (Game1.player.itemToEat.HasContextTag("moonslime.Wizardry.mana_potion_8th"))
                Game1.player.AddMana(Game1.player.GetMaxMana() / 8);
        }

        [SEvent.SaveLoaded]
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
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

    }
}
