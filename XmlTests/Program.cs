using System;
using System.Collections.Generic;
using System.IO;

namespace XmlTests
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello, world!");

            var controller = new XmlController();

            var created = controller.Deserialize<CustomType>(File.ReadAllText(@"D:\Dev\C#\XmlDefs\Example.xml"), null);
            var created2 = controller.Deserialize<List<string>>(File.ReadAllText(@"D:\Dev\C#\XmlDefs\Example2.xml"), null);
            var fromExisting = controller.Deserialize<CustomType>(File.ReadAllText(@"D:\Dev\C#\XmlDefs\Example.xml"), new CustomType(){Float = 5f, Inner = new CustomType(){Number = 123}});

            Console.WriteLine("Float: " + fromExisting.Float);
            Console.WriteLine("Inner.Name: " + fromExisting.Inner.Name);
            Console.WriteLine("Inner.Number: " + fromExisting.Inner.Number);

            foreach(var str in created2)
            {
                Console.WriteLine("  *" + str);
            }

            Console.WriteLine("Result:");
            Console.WriteLine(created.Float);
            Console.WriteLine(created.Obj);
            Console.WriteLine(created.Mode);

            Console.WriteLine("Objects:");
            foreach (var str in created.Objects)
            {
                Console.WriteLine($"  -{str}");
            }
            Console.WriteLine("Bytes:");
            foreach (var b in created.Bytes)
            {
                Console.WriteLine($"  -{b}");
            }
            Console.WriteLine("Nested:");
            foreach (var arr in created.Nested)
            {
                Console.WriteLine($"  -Len {arr.Length}:");
                foreach (var b in arr)
                {
                    Console.WriteLine($"    -{b}");
                }
            }
            Console.WriteLine("Dict:");
            foreach (var pair in created.Dict)
            {
                Console.WriteLine($"  -{pair.Key}: {pair.Value}");
            }
            Console.WriteLine("Subs:");
            foreach (var obj in created.Subs)
            {
                Console.WriteLine($" -{obj.GetType().Name}: {obj.Name}");
            }
            Console.WriteLine("Inner name: " + created.Inner.Name);
            Console.WriteLine("Inner obj: " + created.Inner.Obj);

            Console.ReadKey();
        }
    }
}
