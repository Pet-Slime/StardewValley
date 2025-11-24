using System;
using SpaceCore;
using StardewValley;
using StardewValley.GameData.Machines;
using WizardrySkill.Core.Framework;
using Object = StardewValley.Object;

namespace WizardrySkill.Objects
{
    /// <summary>
    /// Represents a "Mana Sink" machine that consumes mana-related items to grant Extra Spell Points to the player.
    /// Prevents infinite loops by returning a junk item for every mana-containing input.
    /// </summary>
    public class ManaSink : Object
    {
        // Tag constants for detecting mana-related items
        private const string ManaFill = "moonslime.ManaBarApi.ManaFill";       // Flat mana amount
        private const string ManaRestore = "moonslime.ManaBarApi.ManaRestore"; // Percentage of max mana

        public ManaSink()
        {
        }

        /// <summary>
        /// Processes an input item through the Mana Sink.
        /// Adds mana toward Extra Spell Points and prevents item reuse via junk item return.
        /// </summary>
        public static Item GetOutput(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = 720; // Fixed processing time (12 hours in-game)

            // Safety checks to prevent crashes
            if (player == null || inputItem == null)
                return inputItem;

            if (inputItem is not StardewValley.Object)
                return inputItem;

            // Clone the input item for processing
            Object @object = (Object)inputItem.getOne();

            int mana = 0;

            // Loop through all context tags on the item
            // Only the last mana-related tag will be used, consistent with other systems
            foreach (string tag in @object.GetContextTags())
            {
                bool isFill = tag.StartsWith(ManaFill);           // Flat mana
                bool isRestore = !isFill && tag.StartsWith(ManaRestore); // Percentage mana
                if (!isFill && !isRestore) continue;

                // Parse the value after the '/' in the tag
                int sep = tag.IndexOf('/');
                if (sep <= 0 || sep >= tag.Length - 1) continue;

                ReadOnlySpan<char> valueSpan = tag.AsSpan(sep + 1);

                // Calculate mana from the tag
                if (isFill && int.TryParse(valueSpan, out int manaValue))
                {
                    int qualityAdjustment = @object.Quality; // Accounts for item quality
                    mana = (int)Math.Floor(manaValue * (1 + qualityAdjustment * 0.4));
                }
                else if (isRestore && float.TryParse(valueSpan, out float manaPercent))
                {
                    mana = ((int)(player.GetMaxMana() * (manaPercent / 100)));
                }
            }

            // Only process if mana is non-zero && the player has the Adept Profession
            if (mana != 0 && player.HasCustomProfession(WizardrySkill.Objects.Wizard_Skill.Magic10a1))
            {
                // Read current accumulated mana and ESP from player modData
                int currentSinkValue = int.TryParse(player.modData.GetValueOrDefault(MagicConstants.CurrentManaDump), out int val) ? val : 0;
                int extraSpellPoints = int.TryParse(player.modData.GetValueOrDefault(MagicConstants.ExtraSpellPoints), out int esp) ? esp : 0;

                // Add mana to the accumulated total
                currentSinkValue += mana;

                // Calculate threshold for the next Extra Spell Point
                int valueToBeat = GetThreshold(extraSpellPoints);

                // Grant an Extra Spell Point if the threshold is reached
                if (currentSinkValue >= valueToBeat)
                {
                    //Add an extra spell point and keep track of it
                    extraSpellPoints += 1;
                    player.modData[MagicConstants.ExtraSpellPoints] = extraSpellPoints.ToString();
                    player.GetSpellBook().UseSpellPoints(-1);

                    // Reset accumulated mana toward next ESP
                    currentSinkValue = 0;

                }

                // Save updated accumulated mana
                player.modData[MagicConstants.CurrentManaDump] = currentSinkValue.ToString();

                // Return a junk item to prevent the player from looping the same input endlessly
                var junkItem = ItemRegistry.Create<Object>("(O)168", 1, 0);
                return junkItem;
            }

            // Return the original item if no mana was processed
            return @object;
        }

        /// <summary>
        /// Calculates the cumulative mana threshold for the next Extra Spell Point.
        /// Uses a halved cumulative sum for scaling.
        /// 
        /// First 20 thresholds for reference:
        /// ESP: Threshold
        /// 1 : 50,
        /// 2 : 100,
        /// 3 : 200,
        /// 4 : 350,
        /// 5 : 550,
        /// 6 : 800,
        /// 7 : 1100,
        /// 8 : 1450,
        /// 9 : 1850,
        /// 10: 2300,
        /// 11: 2800,
        /// 12: 3350,
        /// 13: 3950,
        /// 14: 4600,
        /// 15: 5300,
        /// 16: 6050,
        /// 17: 6850,
        /// 18: 7700,
        /// 19: 8600,
        /// 20: 9550,
        /// </summary>
        private static int GetThreshold(int extraSpellPoints)
        {
            if (extraSpellPoints <= 0) return 100;

            int threshold = 100;
            for (int i = 1; i < extraSpellPoints; i++)
            {
                threshold += 100 * i;
            }

            threshold = threshold >> 1; 
            return threshold;
        }
    }
}
