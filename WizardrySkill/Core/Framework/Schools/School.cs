using System;
using System.Collections.Generic;
using System.Linq;
using WizardrySkill.Core;
using Microsoft.Xna.Framework.Graphics;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework.Schools
{
    public class School
    {
        /*********
        ** Fields
        *********/
        private static Dictionary<string, School> Schools;
        private readonly Lazy<Texture2D> IconImpl;


        /*********
        ** Accessors
        *********/
        public string Id { get; }

        /// <summary>The display name for the school.</summary>
        public string DisplayName => ModEntry.Instance.I18N.Get($"school.{this.Id}.name");

        public Texture2D Icon => this.IconImpl.Value;


        /*********
        ** Public methods
        *********/
        public virtual Spell[] GetSpellsTier1() { return Array.Empty<Spell>(); }
        public virtual Spell[] GetSpellsTier2() { return Array.Empty<Spell>(); }
        public virtual Spell[] GetSpellsTier3() { return Array.Empty<Spell>(); }

        /// <summary>Get all spell tiers.</summary>
        public IEnumerable<Spell[]> GetAllSpellTiers()
        {
            return new[] { GetSpellsTier1(), GetSpellsTier2(), GetSpellsTier3() }
                .Where(p => p?.Length > 0);
        }

        public static void RegisterSchool(School school)
        {
            if (Schools == null)
                Init();

            Schools.Add(school.Id, school);
        }

        public static School GetSchool(string id)
        {
            if (Schools == null)
                Init();

            return Schools[id];
        }

        public static ICollection<string> GetSchoolList()
        {
            if (Schools == null)
                Init();

            return Schools.Keys;
        }


        /*********
        ** Protected methods
        *********/
        protected School(string id)
        {
            this.Id = id;
            this.IconImpl = new(() => Content.LoadTexture($"magic/{id}/school-icon.png"));
        }

        private static void Init()
        {
            Schools = new Dictionary<string, School>();
            RegisterSchool(new ArcaneSchool());
            RegisterSchool(new ElementalSchool());
            RegisterSchool(new NatureSchool());
            RegisterSchool(new LifeSchool());
            RegisterSchool(new EldritchSchool());
            RegisterSchool(new ToilSchool());

        }
    }
}
