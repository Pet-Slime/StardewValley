using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using SpaceCore;
using StardewValley;
using StardewValley.Menus;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells;
using WizardrySkill.Objects;

namespace WizardrySkill.Core.Framework.Game.Interface
{
    public class MagicMenu : IClickableMenu
    {
        /*********
        ** Layout constants
        *********/
        private const int WindowWidth = 800;
        private const int WindowHeight = 600;

        private const int Padding = 24;

        private const int SchoolIconSize = 32;
        private const int SpellIconSize = 64;
        private const int SelIconSize = 192;
        private const int HotbarIconSize = 48;

        private const int SchoolFrameSize = SchoolIconSize + 24;
        private const int HotbarFrameSize = HotbarIconSize + 24;

        /*********
        ** State
        *********/
        private School SelectedSchool;
        private Spell SelectedSpell;
        private PreparedSpell Dragging;

        private bool JustLeftClicked;
        private bool JustRightClicked;

        /*********
        ** Public methods
        *********/
        public MagicMenu()
            : base((Game1.viewport.Size.Width - WindowWidth) / 2, (Game1.viewport.Size.Height - WindowHeight) / 2, WindowWidth, WindowHeight, true)
        {
            SelectDefaultSchool();
        }

        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        public override void draw(SpriteBatch b)
        {
            // gather state
            SpellBook spellBook = Game1.player.GetSpellBook();
            bool hasFifthSpellSlot = Game1.player.HasCustomProfession(Wizard_Skill.Magic10a2);
            string hoverText = null;

            // draw sections
            DrawBackground(b);
            DrawSchoolIcons(b, spellBook, ref hoverText);
            DrawSpellGrid(b, spellBook, ref hoverText);
            DrawSelectedSpellInfo(b, spellBook, ref hoverText);
            DrawSpellHotbars(b, spellBook, hasFifthSpellSlot, ref hoverText);

            // dragged spell visuals
            DrawDraggedSpell(b);

            // final housekeeping
            if (!string.IsNullOrEmpty(hoverText))
                drawHoverText(b, hoverText, Game1.smallFont);

            base.draw(b);
            this.drawMouse(b);

            // reset click flags (preserve logic from original)
            if (this.JustLeftClicked)
            {
                // If the player left-clicked and didn't drop onto a hotbar, cancel dragging
                this.Dragging = null;
                this.JustLeftClicked = false;
            }
            this.JustRightClicked = false;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            this.JustLeftClicked = true;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            this.JustRightClicked = true;
        }

        /*********
        ** Drawing helpers
        *********/
        private void DrawBackground(SpriteBatch b)
        {
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), this.xPositionOnScreen, this.yPositionOnScreen, WindowWidth, WindowHeight, Color.White);
            // left half background
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), this.xPositionOnScreen, this.yPositionOnScreen, WindowWidth / 2, WindowHeight, Color.White);
        }

        private void DrawSchoolIcons(SpriteBatch b, SpellBook spellBook, ref string hoverText)
        {
            int x = this.xPositionOnScreen - SchoolIconSize - Padding;
            int y = this.yPositionOnScreen;

            foreach (string schoolId in School.GetSchoolList())
            {
                School school = School.GetSchool(schoolId);
                bool knowsSchool = spellBook.KnowsSchool(school);

                float alpha = knowsSchool ? 1f : 0.2f;
                Rectangle iconBounds = new Rectangle(x + Padding /2, y + Padding/2, SchoolIconSize, SchoolIconSize);

                // draw frame
                Color frameColor = this.SelectedSchool == school ? Color.Green : Color.White;
                drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                    x, y, SchoolFrameSize, SchoolFrameSize, frameColor, 1f, false);

                // draw icon (semi transparent if unknown)
                b.Draw(school.Icon, iconBounds, Color.White * alpha);

                // hover + click handling
                if (iconBounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
                {
                    hoverText = knowsSchool ? school.DisplayName : "???";

                    if (this.JustLeftClicked && knowsSchool)
                    {
                        SelectSchool(schoolId, spellBook);
                        this.JustLeftClicked = false;
                    }
                }

                y += SchoolIconSize + Padding;
            }
        }

        private void DrawSpellGrid(SpriteBatch b, SpellBook spellBook, ref string hoverText)
        {
            if (this.SelectedSchool == null)
                return;

            Spell[][] tiers = this.SelectedSchool.GetAllSpellTiers().ToArray();
            int rows = tiers.Length + 1;

            for (int t = 0; t < tiers.Length; ++t)
            {
                Spell[] tier = tiers[t];
                if (tier == null)
                    continue;

                int y = this.yPositionOnScreen + (WindowHeight - 24) / rows * (t + 1);
                int cols = tier.Length + 1;

                for (int s = 0; s < tier.Length; ++s)
                {
                    Spell spell = tier[s];
                    if (spell == null || !spellBook.KnowsSpell(spell, 0))
                        continue;

                    int x = this.xPositionOnScreen + (WindowWidth / 2 - 24) / cols * (s + 1);
                    Rectangle iconBounds = new Rectangle(x - SpellIconSize / 2, y - SpellIconSize / 2, SpellIconSize, SpellIconSize);

                    // hover
                    if (iconBounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
                    {
                        hoverText = spell.GetTooltip();

                        if (this.JustLeftClicked)
                        {
                            this.SelectedSpell = spell;
                            this.JustLeftClicked = false;
                        }
                    }

                    // selection frame
                    if (spell == this.SelectedSpell)
                    {
                        drawTextureBox(b, iconBounds.Left - 12, iconBounds.Top - 12, iconBounds.Width + 24, iconBounds.Height + 24, Color.Green);
                    }


                    b.Draw(ModEntry.Assets.SpellMenubg, iconBounds, Color.White);

                    // draw icon (use highest-level icon available)
                    Texture2D icon = spell.Icons[0];
                    b.Draw(icon, iconBounds, Color.White);
                }
            }
        }

        private void DrawSelectedSpellInfo(SpriteBatch b, SpellBook spellBook, ref string hoverText)
        {
            if (this.SelectedSpell == null)
                return;

            // Title
            string title = this.SelectedSpell.GetTranslatedName();
            Vector2 titlePos = new Vector2(this.xPositionOnScreen + WindowWidth / 2 + (WindowWidth / 2 - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + 30);
            b.DrawString(Game1.dialogueFont, title, titlePos, Color.Black);

            // Big icon
            var icon = this.SelectedSpell.Icons[0];
            Rectangle bigIconRect = new Rectangle(this.xPositionOnScreen + WindowWidth / 2 + (WindowWidth / 2 - SelIconSize) / 2, this.yPositionOnScreen + 85, SelIconSize, SelIconSize);
            b.Draw(icon, bigIconRect, Color.White);

            // Description (wrapped)
            string desc = WrapText(this.SelectedSpell.GetTranslatedDescription(), (int)(WindowWidth / 2 / 0.75f));
            Vector2 descPos = new Vector2(this.xPositionOnScreen + WindowWidth / 2 + 12, this.yPositionOnScreen + 280);
            b.DrawString(Game1.dialogueFont, desc, descPos, Color.Black, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            // Level icons (tiers/levels of spell)
            int sx = this.SelectedSpell.Icons.Length + 1;
            for (int i = 0; i < this.SelectedSpell.Icons.Length; ++i)
            {
                int x = this.xPositionOnScreen + WindowWidth / 2 + WindowWidth / 2 / sx * (i + 1);
                int y = this.yPositionOnScreen + WindowHeight - 12 - SpellIconSize - 32 - 40;
                Rectangle bounds = new Rectangle(x - SpellIconSize / 2, y, SpellIconSize, SpellIconSize);
                bool isHovered = bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());

                bool isKnown = spellBook.KnowsSpell(this.SelectedSpell, i);
                bool hasPreviousLevels = isKnown || i == 0 || spellBook.KnowsSpell(this.SelectedSpell, i - 1);

                Color stateCol;
                if (isKnown)
                {
                    if (isHovered)
                        hoverText = I18n.Tooltip_Spell_Known(spell: I18n.Tooltip_Spell_NameAndLevel(title, level: i + 1));
                    stateCol = Color.Green;
                }
                else if (hasPreviousLevels)
                {
                    if (isHovered)
                        hoverText = spellBook.FreePoints > 0
                            ? I18n.Tooltip_Spell_CanLearn(spell: I18n.Tooltip_Spell_NameAndLevel(title, level: i + 1))
                            : I18n.Tooltip_Spell_NeedFreePoints(spell: I18n.Tooltip_Spell_NameAndLevel(title, level: i + 1));
                    stateCol = Color.White;
                }
                else
                {
                    if (isHovered)
                        hoverText = I18n.Tooltip_Spell_NeedPreviousLevels();
                    stateCol = Color.Gray;
                }

                if (isKnown)
                {
                    drawTextureBox(b, bounds.Left - 12, bounds.Top - 12, bounds.Width + 24, bounds.Height + 24, Color.Green);
                }

                float alpha = hasPreviousLevels ? 1f : 0.5f;
                b.Draw(ModEntry.Assets.SpellMenubg, bounds, Color.White * alpha);
                b.Draw(this.SelectedSpell.Icons[i], bounds, Color.White * alpha);

                // click handling (learn/forget/start drag)
                if (isHovered && (this.JustLeftClicked || this.JustRightClicked))
                {
                    if (this.JustLeftClicked && isKnown)
                    {
                        // begin dragging this known level
                        this.Dragging = new PreparedSpell(this.SelectedSpell.FullId, i);
                        this.JustLeftClicked = false;
                    }
                    else if (hasPreviousLevels)
                    {
                        if (this.JustLeftClicked && spellBook.FreePoints > 0)
                        {
                            spellBook.Mutate(_ => spellBook.LearnSpell(this.SelectedSpell, i));
                            this.JustLeftClicked = false;
                        }
                        else if (this.JustRightClicked && i != 0)
                        {
                            spellBook.Mutate(_ => spellBook.ForgetSpell(this.SelectedSpell, i));
                            this.JustRightClicked = false;
                        }
                    }
                }
            }

            // free points text
            b.DrawString(Game1.dialogueFont, $"Free points: {spellBook.FreePoints}", new Vector2(this.xPositionOnScreen + WindowWidth / 2 + 12 + 24, this.yPositionOnScreen + WindowHeight - 12 - 32 - 20), Color.Black);
        }

        private void DrawSpellHotbars(SpriteBatch b, SpellBook spellBook, bool hasFifthSlot, ref string hoverText)
        {
            // calculate layout
            int hotbarCount = hasFifthSlot ? 5 : 4;
            int hotbarHeight = 12 + HotbarIconSize * hotbarCount + 12 * (hotbarCount - 1) + 12;
            int gap = (WindowHeight - hotbarHeight * 2) / 3 + (hasFifthSlot ? 25 : 0);

            int y = this.yPositionOnScreen + gap + -32 + (hasFifthSlot ? -32 : 0);
            foreach (var spellBar in spellBook.Prepared)
            {
                for (int i = 0; i < hotbarCount; ++i)
                {
                    PreparedSpell prep = spellBar.GetSlot(i);
                    Rectangle bounds = new Rectangle(this.xPositionOnScreen + WindowWidth + 12, y, HotbarIconSize, HotbarIconSize);
                    bool isHovered = bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());

                    // right click clears slot
                    if (isHovered && this.JustRightClicked)
                    {
                        spellBook.Mutate(_ => spellBar.SetSlot(i, null));
                        this.JustRightClicked = false;
                    }

                    // left click drop (set slot to dragging)
                    if (isHovered && this.JustLeftClicked)
                    {
                        spellBook.Mutate(_ => spellBar.SetSlot(i, this.Dragging));
                        this.Dragging = null;
                        this.JustLeftClicked = false;
                    }

                    // draw frame
                    drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                        bounds.X - 12, y - 12, HotbarFrameSize, HotbarFrameSize, Color.White, 1f, false);

                    if (prep != null)
                    {
                        Spell spell = SpellManager.Get(prep.SpellId);
                        Texture2D[] icons = spell?.Icons;
                        if (icons?.Length > prep.Level && icons[prep.Level] != null)
                        {
                            Texture2D icon = icons[prep.Level];
                            b.Draw(icon, bounds, Color.White);
                        }

                        if (isHovered)
                            hoverText = spell.GetTooltip(level: prep.Level);
                    }

                    y += HotbarIconSize + 24;
                }

                y += gap + 12;
            }
        }

        private void DrawDraggedSpell(SpriteBatch b)
        {
            if (this.Dragging == null)
                return;

            Spell spell = SpellManager.Get(this.Dragging.SpellId);
            Texture2D[] icons = spell?.Icons;
            if (icons != null && icons.Length > this.Dragging.Level && icons[this.Dragging.Level] != null)
            {
                Texture2D icon = icons[this.Dragging.Level];
                Rectangle drawRect = new Rectangle(Game1.getOldMouseX() - 24, Game1.getOldMouseY() - 24, HotbarIconSize, HotbarIconSize);
                b.Draw(ModEntry.Assets.SpellMenubg, drawRect, Color.White);
                b.Draw(icon, drawRect, Color.White);
            }
        }

        /*********
        ** Private methods (logic)
        *********/
        /// <summary>Set the selected school to the first one the player knows spells for.</summary>
        private void SelectDefaultSchool()
        {
            SpellBook spellBook = Game1.player.GetSpellBook();
            string firstKnownId = School.GetSchoolList().FirstOrDefault(id => spellBook.KnowsSchool(School.GetSchool(id)));
            if (firstKnownId != null)
                SelectSchool(firstKnownId, spellBook);
        }

        /// <summary>Set the selected school for which to show spells.</summary>
        /// <param name="id">The school ID.</param>
        private void SelectSchool(string id, SpellBook spellbook)
        {
            var school = School.GetSchool(id);
            this.SelectedSchool = school;

            if (school != null)
            {
                this.SelectedSpell = school.GetAllSpellTiers()
                    .SelectMany(p => p ?? new Spell[0])
                    .FirstOrDefault(s => s != null && spellbook.KnowsSpell(s, 0));
            }
            else
            {
                this.SelectedSpell = null;
            }
        }

        // https://gist.github.com/Sankra/5585584
        // TODO: A better version that handles me doing newlines correctly
        private string WrapText(string text, int maxLineWidth)
        {
            if (Game1.dialogueFont.MeasureString(text).X < maxLineWidth)
            {
                return text;
            }

            string[] words = text.Split(' ', '\n');
            var wrappedText = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = Game1.dialogueFont.MeasureString(" ").X;
            foreach (string word in words)
            {
                Vector2 size = Game1.dialogueFont.MeasureString(word);
                if (lineWidth + size.X < maxLineWidth)
                {
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    wrappedText.Append("\n");
                    lineWidth = size.X + spaceWidth;
                }
                wrappedText.Append(word);
                wrappedText.Append(" ");
            }

            return wrappedText.ToString();
        }
    }
}
