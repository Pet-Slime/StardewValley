using StardewValley;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MoonShared.APIs
{
    public interface ISpaceCore
    {
        string[] GetCustomSkills();
        int GetLevelForCustomSkill(Farmer farmer, string skill);
        int GetExperienceForCustomSkill(Farmer farmer, string skill);
        List<Tuple<string, int, int>> GetExperienceAndLevelsForCustomSkill(Farmer farmer);
        void AddExperienceForCustomSkill(Farmer farmer, string skill, int amt);
        int GetProfessionId(string skill, string profession);

        /// Must have [XmlType("Mods_SOMETHINGHERE")] attribute (required to start with "Mods_")
        void RegisterSerializerType(Type type);

        void RegisterCustomProperty(Type declaringType, string name, Type propType, MethodInfo getter, MethodInfo setter);

        public event EventHandler<Action<string, Action>> AdvancedInteractionStarted;
    }
}
