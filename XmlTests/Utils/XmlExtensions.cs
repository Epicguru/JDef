using System;
using System.Xml;

namespace XmlTests.Utils
{
    public static class XmlExtensions
    {
        private static void ReportAttributeError(XmlNode node, string attrName, string value, string expectedType)
        {
            Console.WriteLine($"Attribute error: Node {node.Name} has attribute {attrName}=\"{value}\", but it should be {expectedType}.");
        }

        public static string TryGetAttribute(this XmlNode node, string attributeName)
        {
            if (node == null)
                return null;
            if (attributeName == null)
                return null;

            return node.Attributes?.GetNamedItem(attributeName)?.Value;
        }

        public static bool? TryParseAttributeBool(this XmlNode node, string attributeName)
        {
            string raw = node.TryGetAttribute(attributeName);
            if (raw == null)
                return null;

            if (bool.TryParse(raw, out var value))
                return value;

            ReportAttributeError(node, attributeName, raw, "true or false");
            return null;
        }

        public static int? TryParseAttributeInt(this XmlNode node, string attributeName)
        {
            string raw = node.TryGetAttribute(attributeName);
            if (raw == null)
                return null;

            if (int.TryParse(raw, out var value))
                return value;

            ReportAttributeError(node, attributeName, raw, "an integer");
            return null;
        }

        public static T? TryParseAttributeEnum<T>(this XmlNode node, string attributeName) where T : struct
        {
            string raw = node.TryGetAttribute(attributeName);
            if (raw == null)
                return null;

            Type enumType = typeof(T);

            try
            {
                object obj = Enum.Parse(enumType, raw, true);
                return (T)obj;
            }
            catch
            {
                // Try to parse enum from long integer.
                if(long.TryParse(raw, out long longInt))
                {
                    if (Enum.IsDefined(enumType, longInt))
                        return (T)Enum.ToObject(enumType, (object)longInt);
                }
            }


            ReportAttributeError(node, attributeName, raw, "an enum name or index");
            return null;
        }

        public static bool IsNullable(this Type type)
        {
            if (!type.IsValueType)
                return true; // Structs, primitives...
            if (Nullable.GetUnderlyingType(type) != null)
                return true; // Nullable<T>

            return false; // value-type
        }
    }
}
