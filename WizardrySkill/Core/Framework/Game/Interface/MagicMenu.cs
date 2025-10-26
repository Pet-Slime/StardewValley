using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells;
using WizardrySkill.Objects;

namespace WizardrySkill.Core.Framework.Game.Interface
{
    // TO DO: Go through and comment every single part. job for 
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
        private const string Select = "smallSelect";
        private const string Deselect = "smallSelect";
        private string UnkownSchool = ModEntry.Instance.I18N.Get("moonslime.Wizardry.school.uknown.name");

        private static readonly Rectangle MenuBoxSource = new(0, 256, 60, 60);

        /*********
        ** State
        *********/
        private School SelectedSchool;
        private Spell SelectedSpell;
        private PreparedSpell Dragging;
        private readonly string SchoolTitleCache = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spellmenu.leftsideTitle");

        private bool JustLeftClicked;
        private bool JustRightClicked;

        private int NewBaseX;
        private int NewBaseY;
        private int BottomBarX;
        private int BottomBarY;

        /*********
        ** Constructor
        *********/
        public MagicMenu()
            : base((Game1.viewport.Size.Width - WindowWidth) / 2, (Game1.viewport.Size.Height - WindowHeight) / 2, WindowWidth, WindowHeight, true)
        {
            SelectDefaultSchool();
            UpdateLayoutPositions();
            this.upperRightCloseButton.bounds.Y = this.NewBaseY - 8;
        }

        /*********
        ** Menu overrides
        *********/
        public override bool overrideSnappyMenuCursorMovementBan() => true;

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            this.JustLeftClicked = true;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true) => this.JustRightClicked = true;

        public override void draw(SpriteBatch b)
        {
            // cache per-frame data
            int mouseX = Game1.getOldMouseX();
            int mouseY = Game1.getOldMouseY();
            SpellBook spellBook = Game1.player.GetSpellBook();
            bool hasFifthSlot = Game1.player.HasCustomProfession(Wizard_Skill.Magic10a2);
            string hoverText = null;

            // draw layout
            DrawBackground(b, hasFifthSlot);
            DrawSchoolIcons(b, spellBook, ref hoverText, mouseX, mouseY);
            DrawSpellGrid(b, spellBook, ref hoverText, mouseX, mouseY);
            DrawSelectedSpellInfo(b, spellBook, ref hoverText, mouseX, mouseY);
            DrawSpellHotbars(b, spellBook, hasFifthSlot, ref hoverText, mouseX, mouseY);
            DrawDraggedSpell(b, mouseX, mouseY);

            // hover text & cursor
            if (!string.IsNullOrEmpty(hoverText) && this.Dragging == null)
                drawHoverText(b, hoverText, Game1.smallFont);

            base.draw(b);
            this.drawMouse(b);

            // reset click flags
            if (this.JustLeftClicked || this.JustRightClicked)
            {
                this.Dragging = null;
                this.JustLeftClicked = false;
                this.JustRightClicked = false;
            }

            UpdateLayoutPositions();
        }

        /*********
        ** Layout helpers
        *********/
        private void UpdateLayoutPositions()
        {
            this.NewBaseX = this.xPositionOnScreen;
            this.NewBaseY = this.yPositionOnScreen - 50;
            this.BottomBarX = this.NewBaseX;
            this.BottomBarY = this.NewBaseY + WindowHeight;

            this.upperRightCloseButton.bounds.X = this.NewBaseX + WindowWidth - 36;
            this.upperRightCloseButton.bounds.Y = this.NewBaseY - 8;
        }

        /*********
        ** Draw sections
        *********/
        private void DrawBackground(SpriteBatch b, bool hasFifthSlot)
        {
            // draw main background square
            drawTextureBox(b, Game1.menuTexture, MenuBoxSource, this.NewBaseX, this.NewBaseY, WindowWidth, WindowHeight, Color.White);
            // left half background
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), this.NewBaseX, this.NewBaseY, WindowWidth / 2, WindowHeight, Color.White);
            // draw bottom spell bar background
            drawTextureBox(b, Game1.menuTexture, MenuBoxSource, this.BottomBarX, this.BottomBarY, WindowWidth, HotbarFrameSize + 24, Color.White);
        }

        private void DrawSchoolIcons(SpriteBatch b, SpellBook book, ref string hoverText, int mouseX, int mouseY)
        {
            int x = this.NewBaseX - SchoolIconSize - Padding;
            int y = this.NewBaseY;

            foreach (string id in School.GetSchoolList())
            {
                School school = School.GetSchool(id);
                bool knowsSchool = book.KnowsSchool(school);
                float alpha = knowsSchool ? 1f : 0.2f;
                Rectangle iconRect = new(x + Padding / 2, y + Padding / 2, SchoolIconSize, SchoolIconSize);
                Color frameColor = this.SelectedSchool == school ? Color.Green : Color.White;

                drawTextureBox(b, Game1.menuTexture, MenuBoxSource, x, y, SchoolFrameSize, SchoolFrameSize, frameColor);
                b.Draw(school.Icon, iconRect, Color.White * alpha);

                if (iconRect.Contains(mouseX, mouseY))
                {
                    hoverText = knowsSchool
                        ? school.DisplayName
                        : this.UnkownSchool;

                    if (this.JustLeftClicked)
                    {
                        SelectSchool(id, book);
                        Game1.playSound(Select);
                        this.JustLeftClicked = false;
                    }
                }

                y += SchoolIconSize + Padding;
            }
        }

        private void DrawSpellGrid(SpriteBatch b, SpellBook book, ref string hoverText, int mouseX, int mouseY)
        {
            if (this.SelectedSchool == null)
                return;

            var tiers = this.SelectedSchool.GetAllSpellTiers().ToArray();
            int rows = tiers.Length + 1;


            // Title
            bool knowsSchool = book.KnowsSchool(this.SelectedSchool);
            string title = knowsSchool
                ? $"{this.SelectedSchool.DisplayName} {this.SchoolTitleCache}"
                : $"{this.UnkownSchool} {this.SchoolTitleCache}";
            float centerX = this.NewBaseX + (WindowWidth / 4f);
            int titleWidth = SpriteText.getWidthOfString(title);
            SpriteText.drawString(b, title, (int)(centerX - titleWidth / 2f), this.NewBaseY + 30, scroll_text_alignment: SpriteText.ScrollTextAlignment.Center);

            // Spells
            for (int t = 0; t < tiers.Length; t++)
            {
                Spell[] tier = tiers[t];
                if (tier == null) continue;

                int y = this.NewBaseY + (WindowHeight - 24) / rows * (t + 1);
                int cols = tier.Length + 1;

                for (int s = 0; s < tier.Length; s++)
                {
                    Spell spell = tier[s];
                    if (spell == null) continue;

                    int x = this.NewBaseX + (WindowWidth / 2 - 24) / cols * (s + 1);
                    Rectangle rect = new(x - SpellIconSize / 2, y - SpellIconSize / 2, SpellIconSize, SpellIconSize);
                    bool hovered = rect.Contains(mouseX, mouseY);
                    bool known = book.KnowsSpell(spell, 0);

                    // Draw the select background if it is selected
                    if (spell == this.SelectedSpell)
                    {
                        b.Draw(ModEntry.Assets.SpellMenubg, new Rectangle(rect.Left - 12, rect.Top - 12, rect.Width + 24, rect.Height + 24), Color.Green);
                    }
                    // Draw the spell background
                    b.Draw(ModEntry.Assets.SpellMenubg, rect, Color.White);
                    // Draw the spell icon
                    b.Draw(known ? spell.Icons[0] : ModEntry.Assets.UnknownSpellBg, rect, Color.White);


                    if (!hovered) continue;

                    // Displaying the tooltip
                    // If the spell is known, get the spell's tooltip
                    // If the spell is unkown, get the hint for the spell
                    hoverText = known
                        ? spell.GetTooltip()
                        : ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.{spell.FullId}.hint");

                    if (this.JustLeftClicked && known)
                    {
                        this.SelectedSpell = spell;
                        Game1.playSound(Select);
                        this.JustLeftClicked = false;
                    }
                }
            }
        }

        private void DrawSelectedSpellInfo(SpriteBatch b, SpellBook book, ref string hoverText, int mouseX, int mouseY)
        {
            if (this.SelectedSpell == null)
                return;

            // Title
            string title = this.SelectedSpell.GetTranslatedName();
            float centerX = this.NewBaseX + WindowWidth * 0.75f;
            int titleWidth = SpriteText.getWidthOfString(title);
            SpriteText.drawString(b, title, (int)(centerX - titleWidth / 2f), this.NewBaseY + 30, scroll_text_alignment: SpriteText.ScrollTextAlignment.Center);

            // Big Icon
            Texture2D icon = this.SelectedSpell.Icons[0];
            Rectangle iconRect = new(this.NewBaseX + WindowWidth / 2 + (WindowWidth / 2 - SelIconSize) / 2, this.NewBaseY + 65, SelIconSize, SelIconSize);
            b.Draw(icon, iconRect, Color.White);

            // Description
            string desc = WrapText(this.SelectedSpell.GetTranslatedDescriptionForSpellMenu(), (int)(WindowWidth / 1.5f) + 50);
            b.DrawString(Game1.smallFont, desc, new Vector2(this.NewBaseX + WindowWidth / 2 + 6, this.NewBaseY + 260), Color.Black);

            // Levels
            this.DrawSpellLevels(b, book, ref hoverText, mouseX, mouseY, title);
        }

        private void DrawSpellLevels(SpriteBatch b, SpellBook book, ref string hoverText, int mouseX, int mouseY, string title)
        {
            var icons = this.SelectedSpell.Icons;
            int levelCount = icons.Length;
            int spacing = WindowWidth / 2 / (levelCount + 1);

            for (int i = 0; i < levelCount; i++)
            {
                int x = this.NewBaseX + WindowWidth / 2 + spacing * (i + 1);
                int y = this.NewBaseY + WindowHeight - SpellIconSize - 84;
                Rectangle rect = new(x - SpellIconSize / 2, y, SpellIconSize, SpellIconSize);
                bool hovered = rect.Contains(mouseX, mouseY);
                bool known = book.KnowsSpell(this.SelectedSpell, i);
                bool unlockable = known || i == 0 || book.KnowsSpell(this.SelectedSpell, i - 1);
                float alpha = unlockable ? 1f : 0.5f;

                // Frame color & tooltip
                Color frame = Color.Gray;
                if (known)
                {
                    frame = Color.Green;
                    if (hovered)
                        hoverText = I18n.Tooltip_Spell_Known(spell: I18n.Tooltip_Spell_NameAndLevel(title, i + 1));
                }
                else if (unlockable)
                {
                    frame = Color.White;
                    if (hovered)
                        hoverText = book.FreePoints > 0
                            ? I18n.Tooltip_Spell_CanLearn(spell: I18n.Tooltip_Spell_NameAndLevel(title, i + 1))
                            : I18n.Tooltip_Spell_NeedFreePoints(spell: I18n.Tooltip_Spell_NameAndLevel(title, i + 1));
                }
                else if (hovered)
                    hoverText = I18n.Tooltip_Spell_NeedPreviousLevels();

                if (known)
                    b.Draw(ModEntry.Assets.SpellMenubg, new Rectangle(rect.Left - 12, rect.Top - 12, rect.Width + 24, rect.Height + 24), Color.Green);

                b.Draw(ModEntry.Assets.SpellMenubg, rect, Color.White * alpha);
                b.Draw(icons[i], rect, Color.White * alpha);

                // Click handling
                if (!hovered) continue;

                if (this.JustLeftClicked && this.Dragging == null)
                {
                    if (known)
                        this.Dragging = new PreparedSpell(this.SelectedSpell.FullId, i);
                    else if (unlockable && book.FreePoints > 0)
                        book.Mutate(_ => book.LearnSpell(this.SelectedSpell, i));
                    this.JustLeftClicked = false;
                    if (this.Dragging != null)
                    {
                        Game1.playSound("select");
                    } else
                    {
                        Game1.playSound(Select);
                    }
                }
                else if (this.JustRightClicked && i != 0 && known)
                {
                    if (this.Dragging == null)
                    {
                        book.Mutate(_ => book.ForgetSpell(this.SelectedSpell, i));
                        this.JustRightClicked = false;
                        Game1.playSound(Deselect);
                    } else
                    {
                        this.Dragging = null;
                    }
                }
            }

            // Free points
            string freeText = $"{ModEntry.Instance.I18N.Get("moonslime.Wizardry.spell.spellpoints")}{book.FreePoints}";
            int width = SpriteText.getWidthOfString(freeText);
            SpriteText.drawString(b, freeText, (int)(this.NewBaseX + WindowWidth * 0.75f - width / 2f),
                this.NewBaseY + WindowHeight - 64);
        }

        private void DrawSpellHotbars(SpriteBatch b, SpellBook book, bool hasFifth, ref string hoverText, int mouseX, int mouseY)
        {
            int slots = hasFifth ? 5 : 4;
            int y = this.BottomBarY + 24;
            int x = 14;
            int barNum = 1;

            foreach (var bar in book.Prepared)
            {
                SpriteText.drawString(b, barNum.ToString(), this.BottomBarX + x, y);
                x += SpriteText.getWidthOfString(barNum.ToString()) + 9;

                for (int i = 0; i < slots; i++)
                {
                    Rectangle rect = new(this.BottomBarX + x, y, HotbarIconSize, HotbarIconSize);
                    PreparedSpell prep = bar.GetSlot(i);
                    bool hovered = rect.Contains(mouseX, mouseY);

                    // click logic
                    if (hovered)
                    {
                        if (this.JustRightClicked)
                        {
                            if (this.Dragging != null)
                            {
                                this.Dragging = null;
                                Game1.playSound(Deselect);
                            } else
                            {
                                book.Mutate(_ => bar.SetSlot(i, this.Dragging == null ? null : null));
                                if (prep != null)
                                    Game1.playSound(Deselect);
                            }
                            this.JustRightClicked = false;
                        }
                        else if (this.JustLeftClicked)
                        {
                            book.Mutate(_ => bar.SetSlot(i, this.Dragging));
                            if (this.Dragging != null)
                            {
                                Game1.playSound(Select);
                            } else if (prep != null)
                            {
                                Game1.playSound(Deselect);
                            }
                            this.Dragging = null;
                            this.JustLeftClicked = false;
                        }
                    }

                    // draw frame & spell
                    drawTextureBox(b, Game1.menuTexture, MenuBoxSource, rect.X - 12, y - 12, HotbarFrameSize, HotbarFrameSize, Color.White, drawShadow: false);

                    if (prep != null)
                    {
                        Spell spell = SpellManager.Get(prep.SpellId);
                        Texture2D icon = spell?.Icons.ElementAtOrDefault(prep.Level);
                        if (icon != null)
                            b.Draw(icon, rect, Color.White);

                        if (hovered)
                            hoverText = spell.GetTooltip(prep.Level);
                    }

                    x += HotbarFrameSize;
                }

                if (!hasFifth)
                    x += HotbarFrameSize;

                barNum++;
            }
        }

        private void DrawDraggedSpell(SpriteBatch b, int mouseX, int mouseY)
        {
            if (this.Dragging == null)
                return;

            Spell spell = SpellManager.Get(this.Dragging.SpellId);
            Texture2D icon = spell?.Icons.ElementAtOrDefault(this.Dragging.Level);
            if (icon == null) return;

            Rectangle rect = new(mouseX - 24, mouseY - 24, HotbarIconSize, HotbarIconSize);
            b.Draw(ModEntry.Assets.SpellMenubg, rect, Color.White);
            b.Draw(icon, rect, Color.White);
        }

        /*********
        ** Logic helpers
        *********/
        private void SelectDefaultSchool()
        {
            SpellBook book = Game1.player.GetSpellBook();
            string firstKnown = School.GetSchoolList().FirstOrDefault(id => book.KnowsSchool(School.GetSchool(id)));
            if (firstKnown != null)
                SelectSchool(firstKnown, book);
        }

        private void SelectSchool(string id, SpellBook book)
        {
            this.SelectedSchool = School.GetSchool(id);
            this.SelectedSpell = this.SelectedSchool?
                .GetAllSpellTiers()
                .SelectMany(t => t ?? new Spell[0])
                .FirstOrDefault(s => s != null && book.KnowsSpell(s, 0));
        }

        private static string WrapText(string text, int maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            SpriteFont font = Game1.dialogueFont;
            float spaceWidth = font.MeasureString(" ").X;
            StringBuilder sb = new();

            foreach (string paragraph in text.Split('\n'))
            {
                float lineWidth = 0f;

                foreach (string word in paragraph.Split(' '))
                {
                    float wordWidth = font.MeasureString(word).X;
                    if (lineWidth + wordWidth > maxWidth)
                    {
                        sb.AppendLine();
                        lineWidth = 0f;
                    }

                    sb.Append(word).Append(' ');
                    lineWidth += wordWidth + spaceWidth;
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}
