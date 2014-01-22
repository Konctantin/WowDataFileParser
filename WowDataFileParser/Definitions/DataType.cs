using System.Xml.Serialization;

namespace WowDataFileParser.Definitions
{
    public enum DataType
    {
        [XmlEnum("")]        None,
        [XmlEnum("bool")]    Bool,
        [XmlEnum("byte")]    Byte,
        [XmlEnum("sbyte")]   Styte,
        [XmlEnum("short")]   Short,
        [XmlEnum("ushort")]  Ushort,
        [XmlEnum("int")]     Int,
        [XmlEnum("uint")]    Uint,
        [XmlEnum("long")]    Long,
        [XmlEnum("ulong")]   Ulong,
        [XmlEnum("float")]   Float,
        [XmlEnum("double")]  Double,
        [XmlEnum("string")]  String,
        [XmlEnum("string2")] String2,
        [XmlEnum("pstring")] Pstring,
        [XmlEnum("list")]    List
    }
}