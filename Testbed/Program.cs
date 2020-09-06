using System;
using JDef;

namespace Testbed
{
    class Program
    {
        static void Main(string[] _)
        {
            const string DIR = @"C:\Users\James.000\Documents\Dev\C#\XmlTests\Content";

            DefDatabase database = new DefDatabase();
            database.LoadFromDir(DIR);
            database.Process();

            Console.ReadKey();
        }
    }
}
