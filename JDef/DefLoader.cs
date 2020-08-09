using JXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace JDef
{
    /// <summary>
    /// Loads defs from files.
    ///
    /// To load defs:
    /// Load each xml file by reading all text.
    /// Determine parent & abstract state of def by reading root attributes
    /// Store xml text with parent name.
    ///
    /// Once all xml texts are linked with parent name:
    /// Find non-abstract children.
    /// Make parent tree.
    /// Validate parent tree (check C# inheritance, def inheritance etc.)
    /// Create instance of final C# type.
    /// Load sequentially into instance of final type.
    /// </summary>
    public class DefLoader
    {
        private List<PreProcessedDef> rawData = new List<PreProcessedDef>();
        private Dictionary<string, int> nameToRawDataIndex = new Dictionary<string, int>();
        private XmlController controller = new XmlController();

        public void Load(IEnumerable<string> xmlData)
        {
            if (xmlData == null)
                throw new ArgumentNullException(nameof(xmlData));

            foreach(var data in xmlData)
            {
                Load(data);
            }
        }

        public void Load(string xmlData)
        {
            if (string.IsNullOrWhiteSpace(xmlData))
                throw new ArgumentNullException(nameof(xmlData));

            var reader = XmlReader.Create(new StringReader(xmlData));
            reader.MoveToContent();

            string rootName = reader.Name;
            Console.WriteLine("Root name: " + rootName);
            if (string.IsNullOrWhiteSpace(rootName))
                throw new Exception("Root name is null or blank!");

            string parentName = reader.GetAttribute("parent");
            string className = reader.GetAttribute("class");
            bool isAbstract = false;
            if (bool.TryParse(reader.GetAttribute("abstract"), out bool b))
                isAbstract = b;

            Console.WriteLine($"Parent name: {parentName ?? "null"}");
            Console.WriteLine($"Class name: {className ?? "null"}");
            Console.WriteLine($"Abstract: {isAbstract}");

            bool hasExisting = nameToRawDataIndex.ContainsKey(rootName);
            if (hasExisting)
                throw new Exception($"Duplicate def name: '{rootName}'.");

            nameToRawDataIndex.Add(rootName, rawData.Count);
            rawData.Add(new PreProcessedDef()
            {
                Name = rootName,
                ParentName = parentName,
                IsAbstract = isAbstract,
                ClassName = className,
                XmlData = xmlData
            });
        }

        public List<Def> Process()
        {
            /*
             * Steps:
             * 1. Make a list of all non-abstract defs. These will be the final defs.
             * 2. For each final def, validate final class (non-abstract) and create a def inheritance tree.
             * 3. Validate def inheritance tree (no cycles etc.) and C# inheritance tree.
             * 4. Create instance of final type.
             * 5. Sequentially load data into final type from top of inheritance tree to bottom.
             * 6. Done
             */

            // Step 1: Make list of non-abstracts.
            List<int> concreteTypes = new List<int>();
            for(int i = 0; i < rawData.Count; i++)
            {
                var rawDef = rawData[i];
                if (rawDef.IsAbstract)
                    continue;

                concreteTypes.Add(i);
            }

            Console.WriteLine($"There are {concreteTypes.Count} concrete types.");

            // Step 2 and 3: Create inheritance tree and validate.
            List<int> treeIndices = new List<int>();
            foreach(var index in concreteTypes)
            {
                var raw = rawData[index];
                int duplicateEntry = ResolveParents(treeIndices, raw.ClassName);
                if (duplicateEntry != -1)
                {
                    var rawDupe = rawData[duplicateEntry];
                    throw new Exception($"{raw.Name} has looped inheritance: inherits from {rawDupe.Name} twice.");
                }

                Type finalType = ResolveFinalType(treeIndices, raw.Name);

                // Make sure there actually is a type...
                if (finalType == null)
                    throw new Exception($"{raw.Name} does not specify a C# class name, and none of it's {treeIndices.Count - 1} parents do either.");
                
                // Make sure that the final type is a class.
                // Technically the XML system could load structs, but by design defs should always be value types.
                if (!finalType.IsClass)
                    throw new Exception($"{raw.Name} is of C# type '{finalType.FullName}', but this is not a class (it is either interface, struct or enum).");

                // Make sure that the final type is not abstract.
                if (finalType.IsAbstract)
                    throw new Exception($"{raw.Name} is of C# type '{finalType.FullName}', but that class is abstract. Final C# class must not be abstract!");

                // Make sure that the final type inherits from def.
                if (!typeof(Def).IsAssignableFrom(finalType))
                    throw new Exception($"{raw.Name} is of C# type '{finalType.FullName}' which does not inherit directly or indirectly from Def. Must inherit from Def!");

                raw.FinalType = finalType;
                raw.InheritanceTree = treeIndices.ToArray();

                treeIndices.Clear();
            }

            var defs = new List<Def>();
            foreach(var index in concreteTypes)
            {
                var raw = rawData[index];
                var finalType = raw.FinalType;
                if (finalType == null)
                    continue;

                // Step 4: Create instance of final type.
                Def instance = CreateInstance(finalType) as Def;

                // Step 5.1: Load data into class from top to bottom of tree.
                for (int i = 0; i < raw.InheritanceTree.Length; i++)
                {
                    var rawToWrite = rawData[raw.InheritanceTree[i]];
                    string toWrite = rawToWrite.XmlData;

                    controller.Deserialize(toWrite, instance);
                }

                // 5.2: Assign name.
                instance.Name = raw.Name;

                // Step 6: Done!
                defs.Add(instance);
            }

            return defs;
        }

        private int ResolveParents(List<int> indices, string currentName)
        {
            if (!nameToRawDataIndex.TryGetValue(currentName, out int foundIndex))
                throw new Exception($"Failed to find parent def called '{currentName}'");

            // Duplicate entry, means looped inheritance. Return the index of the duplicate entry.
            if (indices.Contains(foundIndex))
                return foundIndex;

            indices.Add(foundIndex);
            var currentRaw = rawData[foundIndex];

            string parent = currentRaw.ParentName;
            if(parent != null)
            {
                return ResolveParents(indices, parent);
            }

            // This means the top of the tree has been reached, return -1 to indicate success.
            // Reverse the list so that the first item is the highest in the tree.
            indices.Reverse();
            return -1;
        }

        private Type ResolveFinalType(List<int> indices, string defName)
        {
            // Finds the top-most C# class that should be used to create an instance of the def.
            // Also validates C# inheritance.
            // Does not validate final class, that is done outside.

            Type current = null;
            foreach(var index in indices)
            {
                var raw = rawData[index];
                if(raw.ClassName != null)
                {
                    var type = ResolveType(raw.ClassName);
                    if (type == null)
                        throw new Exception($"Failed to resolve type '{raw.ClassName}' (from def {raw.Name}) for child def {defName}.");

                    if (current == null)
                    {
                        current = type;
                    }
                    else
                    {
                        if (!current.IsAssignableFrom(type))
                            throw new Exception($"Invalid C# inheritance for def {defName}: {type.Name} is not assignable to {current.Name}.");
                        current = type;
                    }
                }
            }
            return current;
        }

        private object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type, true);
        }

        private Type ResolveType(string name)
        {
            return Type.GetType(name, false, true);
        }
    }
}
