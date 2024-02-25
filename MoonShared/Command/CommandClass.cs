using System;
using System.Collections.Generic;
using System.Text;

namespace MoonShared.Command
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandClass : Attribute
    {
        public string Name = "";
        public string Prefix = "ms_";
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandMethod : Attribute
    {
        public string Docs = "";

        public CommandMethod(string docs)
        {
            this.Docs = docs;
        }
    }
}
