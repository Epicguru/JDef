using System;
using System.Xml.Serialization;

namespace JDef
{
    public abstract class Def
    {
        [XmlIgnore]
        public string Name;

        internal static void Error(string msg, Exception e = null)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"JDef Error: {msg}");
            if(e != null)
                Console.WriteLine($"Exception:\n{e}");
            Console.ForegroundColor = oldColor;
        }
    }
}
