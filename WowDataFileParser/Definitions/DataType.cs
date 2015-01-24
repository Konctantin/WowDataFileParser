﻿using System.Xml.Serialization;

namespace WowDataFileParser.Definitions
{
    public enum DataType
    {
        [XmlEnum("")]        None,
        [XmlEnum("byte")]    Byte,
        [XmlEnum("sbyte")]   SByte,
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
        [XmlEnum("list")]    List,
        [XmlEnum("slist")]   StringList,
    }
}