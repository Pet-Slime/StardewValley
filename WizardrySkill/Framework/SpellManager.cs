using System;
using System.Collections.Generic;
using System.Linq;
using WizardrySkill.Framework.Schools;
using WizardrySkill.Framework.Spells;
using Log = BirbCore.Attributes.Log;

namespace WizardrySkill.Framework
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
            return !string.IsNullOrEmpty(id) && SpellManager.Spells.TryGetValue(id, out Spell spell)
                ? spell
                : null;
        }

        public static List<string> GetAll()
        {
            return SpellManager.Spells.Keys.ToList<string>();
        }

        internal static void Init(Func<long> getNewId)
        {
            SpellManager.Register(new AnalyzeSpell());
            SpellManager.Register(new ProjectileSpell(SchoolId.Arcane, "magicmissle", 5, 7, 15, "debuffSpell", 16, rotationVelocy: 0f, seeking: true, wavey: false, piercesLeft: 999, ignoreTerrain: true));
            SpellManager.Register(new EnchantSpell(false));
            SpellManager.Register(new EnchantSpell(true));
            SpellManager.Register(new RewindSpell());

            SpellManager.Register(new ClearDebrisSpell());
            SpellManager.Register(new TillSpell());
            SpellManager.Register(new WaterSpell());
            SpellManager.Register(new CollectionSpell());
            SpellManager.Register(new HarvestSpell());

            SpellManager.Register(new LanternSpell(getNewId));
            SpellManager.Register(new TendrilsSpell());
            SpellManager.Register(new MagnetSpell());
            SpellManager.Register(new ShockwaveSpell());
            SpellManager.Register(new PhotosynthesisSpell());

            SpellManager.Register(new HealSpell());
            SpellManager.Register(new CleanseSpell());
            SpellManager.Register(new HasteSpell());
            SpellManager.Register(new BuffSpell());
            SpellManager.Register(new EvacSpell());

            SpellManager.Register(new ProjectileSpell(SchoolId.Elemental, "frostbolt", 7, 5, 10, "flameSpell", 9, tail: 5, debuff: "frozen", wavey: false));
            SpellManager.Register(new ProjectileSpell(SchoolId.Elemental, "fireball", 7, 10, 20, "flameSpell", 10, tail: 10, explosion: true));
            SpellManager.Register(new DescendSpell());
            SpellManager.Register(new KilnSpell());
            SpellManager.Register(new TeleportSpell());

            SpellManager.Register(new MeteorSpell());
            SpellManager.Register(new CharmSpell());
            SpellManager.Register(new BloodManaSpell());
            SpellManager.Register(new LuckStealSpell());
            SpellManager.Register(new SpiritSpell());
        }


        /*********
        ** Private methods
        *********/
        public static void Register(Spell spell)
        {
            SpellManager.Spells.Add(spell.ParentSchool.Id + ":" + spell.Id, spell);
            spell.LoadIcon();
        }
    }
}
