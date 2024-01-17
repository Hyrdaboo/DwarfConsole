using System;

namespace DwarfConsole
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    internal class ConsoleCommandAttribute : Attribute
    {
        public readonly string name;
        public readonly string description;

        public ConsoleCommandAttribute(string name, string description = "")
        {
            this.name = name;
            this.description = description;
        }
    }
}
