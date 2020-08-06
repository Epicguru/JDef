using System;
using System.Xml;

namespace XmlTests
{
    public interface IRootTypeSerializer
    {
        Type TargetType { get; }

        object Deserialize(XmlNode node);

        string Serialize(object o);
    }
}
