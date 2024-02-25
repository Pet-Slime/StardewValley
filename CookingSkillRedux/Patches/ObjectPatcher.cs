using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;
using MoonShared.Patching;

namespace CookingSkill.Patches
{
    internal class ObjectPatcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<SObject>(nameof(SObject._GetOneFrom)),
                prefix: this.GetHarmonyMethod(nameof(ObjectPatcher.After_GetOneFrom))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call after <see cref="SObject._GetOneFrom"/>.</summary>
        public static void After_GetOneFrom(ref SObject __instance, Item source)
        {
            if (source is SObject sourceObj)
                __instance.Edibility = sourceObj.Edibility;
        }
    }
}
