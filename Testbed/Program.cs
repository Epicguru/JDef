using System;
using JDef;

namespace Testbed
{
    class Program
    {
        static void Main(string[] args)
        {
            const string DIR = @"C:\Users\James.000\Documents\Dev\C#\XmlTests";

            DefDatabase database = new DefDatabase();
            database.LoadFromDir(DIR);

            Console.ReadKey();
        }
    }
}
