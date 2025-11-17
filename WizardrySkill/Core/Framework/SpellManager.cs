using System;
using System.Collections.Generic;
using System.Linq;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells;

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
            Register(new EnchantSpell(false));
            Register(new EnchantSpell(true));
            Register(new SpellStonesSpell());
            Register(new RewindSpell());

            Register(new ClearDebrisSpell());
            Register(new TillSpell());
            Register(new WaterSpell());
            Register(new CollectionSpell());
            Register(new KilnSpell());


            Register(new LanternSpell(getNewId));
            Register(new CrabRainSpell());
            Register(new FishFrenzySpell());
            Register(new TendrilsSpell());
            Register(new PhotosynthesisSpell());

            Register(new HealSpell());
            Register(new HealAreaSpell());
            Register(new CleanseSpell());
            Register(new MagnetSpell());
            Register(new BuffSpell());

            Register(new ProjectileSpell(SchoolId.Elemental, "magicmissle", 4, ModEntry.Config.Magic_arrow_base, ModEntry.Config.Magic_arrow_scale, "magic_arrow_hit", 16, rotationVelocy: 0f, seeking: true, wavey: false, piercesLeft: 999, ignoreTerrain: true));
            Register(new ProjectileSpell(SchoolId.Elemental, "frostbolt", 6, ModEntry.Config.Frost_bolt_base, ModEntry.Config.Frost_bolt_scale, "coldSpell", 9, tail: 5, debuff: "frozen", wavey: false));
            Register(new ProjectileSpell(SchoolId.Elemental, "fireball", 6, ModEntry.Config.Fire_ball_base, ModEntry.Config.Fire_ball_scale, "flameSpell", 10, tail: 10, explosion: true));
            Register(new ShockwaveSpell());
            Register(new MeteorSpell());

            Register(new EvacSpell());
            Register(new HasteSpell());
            Register(new DescendSpell());
            Register(new TeleportSpell());
            if (ModEntry.Config.VoidSchool)
            {
                Register(new BlinkSpell());
            }

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
            spell.LoadLevel();
        }
    }
}
