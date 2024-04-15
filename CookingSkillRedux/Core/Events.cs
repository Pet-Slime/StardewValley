using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BirbCore.Attributes;
using Force.DeepCloner;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using SpaceShared.APIs;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using static BirbCore.Attributes.SEvent;
using static BirbCore.Attributes.SMod;

namespace CookingSkill.Core
{
    [SEvent]
    public class Events
    {
        [SEvent.GameLaunchedLate]
        public static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Log.Trace("Cooking: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Cooking_Skill());


            SpaceEvents.OnItemEaten += OnItemEat;
        }

        public static void OnItemEat(object sender, EventArgs e)
        {
            StardewValley.Farmer who = sender as StardewValley.Farmer;
            var player = Game1.getFarmer(who.UniqueMultiplayerID);

            if (player.HasCustomProfession(Cooking_Skill.Cooking5b))
            {
                var food = player.itemToEat;
                Game1.objectData.TryGetValue(food.ItemId, out ObjectData data);

                foreach (Buff foodOrDrinkBuff in food.GetFoodOrDrinkBuffs())
                {
                    if (player.hasBuff(foodOrDrinkBuff.id))
                    {
                        player.buffs.Remove(foodOrDrinkBuff.id);

                        foodOrDrinkBuff.millisecondsDuration = (int)(1.2 * foodOrDrinkBuff.millisecondsDuration);
                        if (player.HasCustomProfession(Cooking_Skill.Cooking10b1))
                        {
                            ApplyAttributeBuff(foodOrDrinkBuff.effects, 1f);
                        }
                        player.buffs.Apply(foodOrDrinkBuff);

                    }
                }

                if (player.HasCustomProfession(Cooking_Skill.Cooking10b2))
                {
                    // Define constants for attribute types and max values
                    const int NumAttributes = 10;
                    const int MaxAttributeValue = 5;
                    const int MaxStaminaMultiplier = 16;
                    const int BuffDurationMultiplier = (6000 * 10);

                    // Generate random attribute and level
                    int attributeBuff = Game1.random.Next(1, NumAttributes + 1);
                    Log.Trace("Cooking: random attibute is: " + attributeBuff.ToString());
                    int attributeLevel = Game1.random.Next(1, MaxAttributeValue + 1);
                    Log.Trace("Cooking: random level is: " + attributeLevel.ToString());

                    // Create a BuffEffects instance
                    BuffEffects randomEffect = new()
                    {
                        FarmingLevel = { 0 },
                        FishingLevel = { 0 },
                        MiningLevel = { 0 },
                        LuckLevel = { 0 },
                        ForagingLevel = { 0 },
                        MaxStamina = { 0 },
                        MagneticRadius = { 0 },
                        Defense = { 0 },
                        Attack = { 0 },
                        Speed = { 0 }
                    };


                    // Apply the random effect based on the randomly generated attribute
                    switch (attributeBuff)
                    {
                        case 1: randomEffect.FarmingLevel.Value = attributeLevel; break;
                        case 2: randomEffect.FishingLevel.Value = attributeLevel; break;
                        case 3: randomEffect.MiningLevel.Value = attributeLevel; break;
                        case 4: randomEffect.LuckLevel.Value = attributeLevel; break;
                        case 5: randomEffect.ForagingLevel.Value = attributeLevel; break;
                        case 6: randomEffect.MaxStamina.Value = attributeLevel * MaxStaminaMultiplier; break;
                        case 7: randomEffect.MagneticRadius.Value = attributeLevel * MaxStaminaMultiplier; break;
                        case 8: randomEffect.Defense.Value = attributeLevel; break;
                        case 9: randomEffect.Attack.Value = attributeLevel; break;
                        case 10: randomEffect.Speed.Value = attributeLevel; break;
                    }

                    // Create and apply the buff
                    Buff buff = new(
                        id: "Cooking:profession:random_buff",
                        displayName: ModEntry.Instance.I18n.Get("moonslime.Cooking.Profession10b2.buff"),
                        description: null,
                        iconTexture: ModEntry.Assets.Random_Buff,
                        iconSheetIndex: 0,
                        duration: BuffDurationMultiplier * Utilities.GetLevel(player),
                        effects: randomEffect
                    );
                    player.buffs.Remove(buff.id);
                    player.applyBuff(buff);
                }

            }
        }

        private static void ApplyAttributeBuff(BuffEffects effects, float value)
        {
            
            NetFloat[] array = [effects.FarmingLevel, effects.FishingLevel, effects.MiningLevel, effects.LuckLevel, effects.ForagingLevel, effects.Speed, effects.Defense, effects.Attack, effects.CombatLevel, effects.Immunity];
            foreach (NetFloat netFloat in array)
            {
                if (netFloat.Value != 0f)
                {
                    netFloat.Value += value;
                }
            }

            NetFloat[] array2 = [effects.MaxStamina, effects.MagneticRadius];
            foreach (NetFloat netFloat in array2)
            {
                if (netFloat.Value != 0f)
                {
                    netFloat.Value = ((value*1.2f)*netFloat.Value);
                }
            }
        }
    }
}
