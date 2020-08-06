using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using XmlTests.Serializers;
using XmlTests.Utils;

namespace XmlTests
{
    public class XmlController
    {
        private Dictionary<Type, IRootTypeSerializer> rootSerializers = new Dictionary<Type, IRootTypeSerializer>();

        public XmlController()
        {
            // Add default serializers.
            AddRootTypeSerializer(new ByteParser());
            AddRootTypeSerializer(new SByteParser());
            AddRootTypeSerializer(new ShortParser());
            AddRootTypeSerializer(new UShortParser());
            AddRootTypeSerializer(new IntParser());
            AddRootTypeSerializer(new UIntParser());
            AddRootTypeSerializer(new LongParser());
            AddRootTypeSerializer(new ULongParser());
            AddRootTypeSerializer(new FloatParser());
            AddRootTypeSerializer(new DoubleParser());
            AddRootTypeSerializer(new DecimalParser());
            AddRootTypeSerializer(new StringParser());
            AddRootTypeSerializer(new BoolParser());
            AddRootTypeSerializer(new CharParser());
        }

        public void AddRootTypeSerializer(IRootTypeSerializer serializer)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            var targetType = serializer.TargetType;

            if (rootSerializers.ContainsKey(targetType))
                throw new ArgumentException($"There is already a root serializer for the type {targetType.FullName}", nameof(serializer));

            rootSerializers.Add(targetType, serializer);
        }

        public IRootTypeSerializer GetRootTypeSerializer<T>()
        {
            return GetRootTypeSerializer(typeof(T));
        }

        public IRootTypeSerializer GetRootTypeSerializer(Type type)
        {
            if (type == null)
                return null;

            return rootSerializers.TryGetValue(type, out var value) ? value : null;
        }

        public bool HasRootTypeSerializer<T>()
        {
            return HasRootTypeSerializer(typeof(T));
        }

        public bool HasRootTypeSerializer(Type type)
        {
            return GetRootTypeSerializer(type) != null;
        }

        public void RemoveRootTypeSerializer<T>()
        {
            RemoveRootTypeSerializer(typeof(T));
        }

        public void RemoveRootTypeSerializer(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!rootSerializers.ContainsKey(type))
                throw new Exception($"There is no registered root serializer for type {type.FullName}, cannot remove.");

            rootSerializers.Remove(type);
        }

        public T Deserialize<T>(string xml, T toFill) where T : class
        {
            IList list = new List<string>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            T returnValue = (T)CreateAndPopulate(toFill, doc.FirstChild, typeof(T));

            watch.Stop();
            Console.WriteLine($"Took {watch.Elapsed.TotalMilliseconds:F2} ms");

            return returnValue;
            object CreateAndPopulate(object existing, XmlNode rootNode, Type type)
            {
                string customClassName = rootNode.TryGetAttribute("class");
                if(customClassName != null)
                {
                    customClassName = customClassName.Trim();
                    var newType = Type.GetType(customClassName, false, true);
                    if(newType == null)
                    {
                        Console.WriteLine($"[ERROR] Node {rootNode.Name}: Could not find custom class '{customClassName}' for node {rootNode.Name}. Node will be ignored.");
                        return null;
                    }
                    if (!type.IsAssignableFrom(newType))
                    {
                        string problem = type.IsInterface ? "does not implement interface" : "is not a subclass of";
                        Console.WriteLine($"[ERROR] Node {rootNode.Name}: {newType.FullName} {problem} {type.FullName}. Node will be ignored.");
                        return null;
                    }

                    type = newType;
                }
                if(type.IsAbstract || type.IsInterface)
                {
                    string problem = type.IsInterface ? "an interface" : "an abstract class";
                    Console.WriteLine($"[ERROR] Node {rootNode.Name}: {type.FullName} is {problem} and so cannot be instantiated. Please use the class='typeName' attribute to specify a concrete class. Node will be ignored.");
                    return null;
                }

                var loader = GetRootTypeSerializer(type);
                bool isBasic = loader != null;
                bool isArrayType = IsArrayType(type);
                bool isListType = IsListType(type);
                bool isDictType = IsDictionaryType(type);

                if (isArrayType)
                {
                    // Create the array.
                    int arrayLength = 0;
                    for(int i = 0; i < rootNode.ChildNodes.Count; i++)
                    {
                        var node = rootNode.ChildNodes.Item(i);
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            arrayLength++; 
                        }
                    }

                    Array created = CreateInstance(type, arrayLength) as Array;
                    Type arrayType = type.GetElementType();
                    for (int i = 0; i < created.Length; i++)
                    {
                        var node = rootNode.ChildNodes.Item(i);
                        object atPosition = CreateAndPopulate(null, node, arrayType);

                        created.SetValue(atPosition, i);
                    }

                    string attr = rootNode.TryGetAttribute("mode");
                    if(attr != null)
                    {
                        Console.WriteLine($"[ERROR] Node {rootNode.Name}: Arrays cannot use merge modes. To use merge modes, change it to a List<{arrayType.Name}>.");
                    }

                    return created;
                }

                if (isListType)
                {
                    IList old = existing as IList;

                    Type listType = type.GetGenericArguments()[0];
                    IList created = (IList)CreateInstance(typeof(List<>).MakeGenericType(listType));
                    for (int i = 0; i < rootNode.ChildNodes.Count; i++)
                    {
                        var node = rootNode.ChildNodes.Item(i);
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            object atPosition = CreateAndPopulate(null, node, listType);

                            created.Add(atPosition);
                        }
                    }

                    if (old == null)
                        return created;

                    ListMergeMode mode = rootNode.TryParseAttributeEnum<ListMergeMode>("mode") ?? ListMergeMode.Merge;
                    if((old.IsFixedSize || old.IsReadOnly) && mode != ListMergeMode.Replace)
                    {
                        Console.WriteLine($"[ERROR] Node {rootNode.Name}: This List<{listType.Name}> uses merge mode {mode}, but the list instance is read-only. New list will replace old values.");
                        mode = ListMergeMode.Replace;
                    }

                    ListMergeUtils.Combine(old, created, mode);

                    return old;
                }

                if (isDictType)
                {
                    Type[] dictParams = type.GetGenericArguments();
                    Type keyType = dictParams[0];
                    Type valueType = dictParams[1];

                    IDictionary created = (IDictionary) CreateInstance(typeof(Dictionary<,>).MakeGenericType(dictParams));
                    bool? attrCompact = rootNode.TryParseAttributeBool("compact");
                    bool useCompact = keyType == typeof(string) && (attrCompact == null || attrCompact.Value);
                    if(keyType != typeof(string) && attrCompact != null && attrCompact.Value)
                    {
                        Console.WriteLine($"[ERROR] Node {rootNode.Name} has compact='true', but the dictionary has keys of type {keyType.Name}. They must be Strings to use compact mode.");
                    }
                    bool allowNullValues = valueType.IsNullable();
                    if (useCompact)
                    {
                        // The key is an attribute, the value is the node value.
                        for (int i = 0; i < rootNode.ChildNodes.Count; i++)
                        {
                            var node = rootNode.ChildNodes.Item(i);
                            if (node.NodeType != XmlNodeType.Element)
                                continue;

                            string key = node.TryGetAttribute("key");
                            if (key == null)
                            {
                                Console.WriteLine($"[ERROR] Dictionary {rootNode.Name} has element at index {i} that does not have a key attribute, such as key='hello'. Compact mode is enabled. Use compact='false' to disable compact mode on this dictionary.");
                                continue;
                            }
                            if (created.Contains(key))
                            {
                                Console.WriteLine($"[ERROR] Duplicate key in dictionary {rootNode.Name}: '{key}'");
                                continue;
                            }
                            object value = CreateAndPopulate(null, node, valueType);

                            if(value == null && !allowNullValues)
                            {
                                Console.WriteLine($"[ERROR] Dictionary {rootNode.Name} has a null value. This is not valid because the value type is {valueType.Name}.");
                                continue;
                            }

                            created.Add(key, value);
                        }
                    }
                    else
                    {
                        // The key is a node with the name K, a value is a node with the name V.
                        // K and V's Should be in sequence and should be in pairs.
                        // The key is an attribute, the value is the node value.
                        bool expectKey = true;
                        int index = -1;
                        object lastKey = null;
                        for (int i = 0; i < rootNode.ChildNodes.Count; i++)
                        {
                            var node = rootNode.ChildNodes.Item(i);
                            if (node.NodeType != XmlNodeType.Element)
                                continue;

                            index++;
                            string nodeName = node.Name.Trim().ToLower();
                            bool isKey = nodeName == "k" || nodeName == "key";
                            bool isValue = nodeName == "v" || nodeName == "value";

                            if(!isKey && !isValue)
                            {
                                Console.WriteLine($"[ERROR] Dictionary {rootNode.Name} has an item at index {index} called '{node.Name}'. Dictionary items, when not in compact mode, should only be named either K or V. Value will be assumed to be a {(expectKey ? "key" : "value")}.");
                                if (expectKey)
                                    isKey = true;
                                else
                                    isValue = true;
                            }

                            if(isKey && !expectKey)
                            {
                                Console.WriteLine($"[ERROR] Dictionary {rootNode.Name} has two keys in a row! Item order must be key, value, key, value etc.");
                                continue;
                            }
                            if (!isKey && expectKey)
                            {
                                Console.WriteLine($"[ERROR] Dictionary {rootNode.Name} has two values in a row! Item order must be key, value, key, value etc.");
                                continue;
                            }

                            if (isKey)
                            {
                                lastKey = CreateAndPopulate(null, node, keyType);
                                expectKey = false;
                            }
                            else
                            {
                                expectKey = true;
                                if (created.Contains(lastKey))
                                {
                                    Console.WriteLine($"[ERROR] Duplicate key in dictionary {rootNode.Name}: '{lastKey}'");
                                    lastKey = null;
                                    continue;
                                }
                                object value = CreateAndPopulate(null, node, valueType);
                                created.Add(lastKey, value);
                                lastKey = null;
                            }
                        }
                    }
                    return created;
                }

                if (isBasic)
                {
                    var fromLoader = loader.Deserialize(rootNode.FirstChild);
                    return fromLoader;
                }
                else
                {
                    // Create new object (class or struct)
                    var created = existing ?? CreateInstance(type);

                    // Get all child nodes in this node.
                    var children = rootNode.ChildNodes;
                    for (int i = 0; i < children.Count; i++)
                    {
                        var node = children.Item(i);

                        //Console.WriteLine($"[{node.NodeType}] {node.Name}: {node.Value}");
                        if (node.NodeType != XmlNodeType.Element)
                            continue;

                        string fieldName = node.Name;
                        var field = GetField(fieldName, type);
                        if (!field.IsValid)
                        {
                            //Console.WriteLine($"Error: Failed to find field '{type.FullName}.{fieldName}'");
                            continue;
                        }

                        Type childType = field.FieldType;

                        var childObj = CreateAndPopulate(ReadField(field, created), node, childType);

                        WriteField(field, created, childObj);
                    }
                    return created;
                }
            }
        }

        private bool IsArrayType(Type t)
        {
            return t.IsArray;
        }

        private bool IsListType(Type t)
        {
            return typeof(IList).IsAssignableFrom(t);
        }

        private bool IsDictionaryType(Type t)
        {
            return typeof(IDictionary).IsAssignableFrom(t);
        }

        private bool IsEnumType(Type t)
        {
            return t.IsEnum;
        }

        private object CreateInstance(Type type, int arrayLength = 0)
        {
            if (type.IsArray)
            {
                return Array.CreateInstance(type.GetElementType(), arrayLength <= 0 ? throw new ArgumentException("Array length must be greater than zero.", nameof(arrayLength), null) : arrayLength);
            }

            return Activator.CreateInstance(type, true);
        }

        private FieldWrapper GetField(string fieldName, Type type)
        {
            if (type == null || fieldName == null)
                return default;

            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
                return new FieldWrapper(field);
            var prop = type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
                return new FieldWrapper(prop);

            return default;
        }

        private void WriteField(FieldWrapper wrapper, object obj, object value)
        {
            if (!wrapper.IsValid)
                throw new Exception("Field wrapper is invalid.");

            if (wrapper.IsField)
            {
                wrapper.Field.SetValue(obj, value);
            }
            else
            {
                wrapper.Property.SetValue(obj, value);
            }
        }

        private object ReadField(FieldWrapper wrapper, object obj)
        {
            if (!wrapper.IsValid)
                throw new Exception("Field wrapper is invalid.");

            if (wrapper.IsField)
            {
                return wrapper.Field.GetValue(obj);
            }
            else
            {
                if(wrapper.Property.CanRead)
                    return wrapper.Property.GetValue(obj);
                return null;
            }
        }
    }
}
