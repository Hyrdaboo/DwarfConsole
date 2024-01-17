using System;

namespace DwarfConsole
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ConsoleParseAttribute : Attribute {}
}
