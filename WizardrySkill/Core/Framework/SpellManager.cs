using System;
using System.Collections.Generic;
using System.Linq;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells;
using Log = BirbCore.Attributes.Log;

namespace WizardrySkill.Core.Framework
{
    /// <summary>Manages the available spells.</summary>
    internal static class SpellManager
    {
        /*********
        ** Fields
        *********/
        private static readonly Dictionary<string, Spell> Spells = new();


        /*********
        ** Public methods
        *********/
        public static Spell Get(string id)
        {
            return !string.IsNullOrEmpty(id) && Spells.TryGetValue(id, out Spell spell)
                ? spell
                : null;
        }

        public static List<string> GetAll()
        {
            return Spells.Keys.ToList();
        }

        internal static void Init(Func<long> getNewId)
        {
            Register(new AnalyzeSpell());
            Register(new ProjectileSpell(SchoolId.Arcane, "magicmissle", 5, 7, 15, "debuffSpell", 16, rotationVelocy: 0f, seeking: true, wavey: false, piercesLeft: 999, ignoreTerrain: true));
            Register(new EnchantSpell(false));
            Register(new EnchantSpell(true));
            Register(new RewindSpell());

            Register(new ClearDebrisSpell());
            Register(new TillSpell());
            Register(new WaterSpell());
            Register(new CollectionSpell());
            Register(new HarvestSpell());

            Register(new LanternSpell(getNewId));
            Register(new TendrilsSpell());
            Register(new MagnetSpell());
            Register(new ShockwaveSpell());
            Register(new PhotosynthesisSpell());

            Register(new HealSpell());
            Register(new CleanseSpell());
            Register(new HasteSpell());
            Register(new BuffSpell());
            Register(new EvacSpell());

            Register(new ProjectileSpell(SchoolId.Elemental, "frostbolt", 7, 5, 10, "flameSpell", 9, tail: 5, debuff: "frozen", wavey: false));
            Register(new ProjectileSpell(SchoolId.Elemental, "fireball", 7, 10, 20, "flameSpell", 10, tail: 10, explosion: true));
            Register(new DescendSpell());
            Register(new KilnSpell());
            Register(new TeleportSpell());

            Register(new MeteorSpell());
            Register(new CharmSpell());
            Register(new BloodManaSpell());
            Register(new LuckStealSpell());
            Register(new SpiritSpell());
        }


        /*********
        ** Private methods
        *********/
        public static void Register(Spell spell)
        {
            Spells.Add(spell.ParentSchool.Id + ":" + spell.Id, spell);
            spell.LoadIcon();
        }
    }
}
