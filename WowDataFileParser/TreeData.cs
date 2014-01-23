using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WowDataFileParser
{
    public class TreeData : List<object>
    {
        public TreeData()
            : base()
        {
        }

        public TreeData Alloc()
        {
            var sub = new TreeData();
            Add(sub);
            return sub;
        }

        public void ParseValue(ref StringBuilder sb, object element)
        {
            if (element == null)
                sb.Append("NULL, ");
            else if (element is TreeData)
                foreach (var sub in (element as TreeData))
                    ParseValue(ref sb, sub);
            else
                sb.AppendFormat("{0}, ", GetEscapedSqlValue(element));
        }

        public string ToSqlString(string table, string locale)
        {
            var content = new StringBuilder();
            content.AppendFormat("REPLACE INTO `{0}` VALUES (\'{1}\', ", table, locale);

            foreach (var element in this)
                ParseValue(ref content, element);

            return content.
                Remove(content.Length - 2, 2)
                .Append(");")
                .ToString();
        }

        public string GetEscapedSqlValue(object Value)
        {
            if (Value == null)
                return "NULL";
            else if (Value is string)
            {
                var str = (Value as string);
                if (string.IsNullOrWhiteSpace(str) || str == "\0")
                    return "NULL";
                return "\'" + str.Replace(@"'", @"\'").Replace("\"", "\\\"") + "\'";
            }
            else if (Value is bool)     return ((bool)   Value ? "1" : "0");
            else if (Value is byte)     return ((byte)   Value).ToString();
            else if (Value is sbyte)    return ((sbyte)  Value).ToString();
            else if (Value is short)    return ((short)  Value).ToString();
            else if (Value is ushort)   return ((ushort) Value).ToString();
            else if (Value is int)      return ((int)    Value).ToString();
            else if (Value is uint)     return ((uint)   Value).ToString();
            else if (Value is long)     return ((long)   Value).ToString();
            else if (Value is ulong)    return ((ulong)  Value).ToString();
            else if (Value is float)    return (((float) Value).ToString(CultureInfo.InvariantCulture));
            else if (Value is double)   return (((double)Value).ToString(CultureInfo.InvariantCulture));
            else
                throw new NotSupportedException("Not suported type " + Value.GetType());
        }
    }
}
