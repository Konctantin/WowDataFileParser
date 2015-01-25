using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MS.Internal.Ink;
using WowDataFileParser.Definitions;

namespace WowDataFileParser
{
    public class RowReader : BitStreamReader
    {
        private StringBuilder content;
        private List<string> subcontent = new List<string>();
        private Dictionary<string, IConvertible> valList = new Dictionary<string, IConvertible>();
        private Dictionary<int, string> stringTable;

        public RowReader(byte[] buffer, Dictionary<int, string> stringTable = null)
            : base (buffer)
        {
            this.stringTable = stringTable;
        }

        public void ReadField(ref StringBuilder content, Field field)
        {
            this.content = content;

            this.ReadType(field);
        }

        public int GetValueByFiedName(string name)
        {
            IConvertible val;
            if (name != null && valList.TryGetValue(name, out val))
                return val.ToInt32(CultureInfo.InvariantCulture);
            return 0;
        }

        private void SetVal(Field field, IConvertible value, bool isString = false)
        {
            if (!string.IsNullOrWhiteSpace(field.Name))
                this.valList[field.Name] = value;

            if (value == null)
                content.Append(", NULL");
            else if (!isString)
                content.Append(", " + value.ToString(CultureInfo.InvariantCulture));
            else
            {
                var val = value.ToString(CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(val))
                    content.Append(", NULL");
                else
                    content.Append(", \"" + val.Replace(@"'", @"\'").Replace("\"", "\\\"") + "\"");
            }

            // for debugging only
            //content.Append(" /* " + field.Name + " */");
        }

        private void ReadType(Field field, bool read = true)
        {
            var count = field.Size;
            if (count == 0)
                count = GetValueByFiedName(field.SizeLink);

            switch (field.Type)
            {
                case DataType.Byte:    SetVal(field, read ? base.ReadByte(count)    : 0 ); break;
                case DataType.SByte:   SetVal(field, read ? base.ReadSByte(count)   : 0 ); break;
                case DataType.Short:   SetVal(field, read ? base.ReadInt16(count)   : 0 ); break;
                case DataType.Ushort:  SetVal(field, read ? base.ReadUInt16(count)  : 0 ); break;
                case DataType.Int:     SetVal(field, read ? base.ReadInt32(count)   : 0 ); break;
                case DataType.Uint:    SetVal(field, read ? base.ReadUInt32(count)  : 0 ); break;
                case DataType.Long:    SetVal(field, read ? base.ReadInt64(count)   : 0 ); break;
                case DataType.Ulong:   SetVal(field, read ? base.ReadUInt64(count)  : 0 ); break;
                case DataType.Float:   SetVal(field, read ? base.ReadFloat()        : 0f); break;
                case DataType.Double:  SetVal(field, read ? base.ReadDouble()       : 0d); break;

                case DataType.Pstring: SetVal(field, read ? base.ReadPString(count) : null, true); break;
                case DataType.String2: SetVal(field, read ? base.ReadString3(count) : null, true); break;
                case DataType.String:
                    {
                        if (stringTable != null) // adb db2
                        {
                            var offset = base.ReadInt32();
                            SetVal(field, stringTable[offset], true);
                        }
                        else if (read)
                        {
                            if (count == 0 && field.SizeLink == null)
                                SetVal(field, base.ReadCString(), true);
                            else
                                SetVal(field, base.ReadString2(count), true);
                        }
                        else
                        {
                            SetVal(field, null);
                        }
                    } break;
                case DataType.List:
                    {
                        var size = 0;
                        if (field.Size > 0)
                        {
                            size = read ? base.ReadSize(field.Size) : 0;
                            SetVal(field, size);
                        }
                        else if (!string.IsNullOrWhiteSpace(field.SizeLink))
                        {
                            size = GetValueByFiedName(field.SizeLink);
                        }
                        else if (field.Maxsize > 0)
                        {
                            size = field.Maxsize;
                        }

                        if (size > field.Maxsize)
                        {
                            throw new Exception(string.Format("Field <{0}>'{1}' size '{2}' is great then maxsize '{3}'!",
                                field.Type, field.Name, size, field.Maxsize));
                        }

                        for (int i = 0; i < field.Maxsize; ++i)
                        {
                            read = i < size;
                            foreach (var subfield in field.Fields)
                            {
                                ReadType(subfield, read);
                            }
                        }
                    } break;
                case DataType.StringList:
                    {
                        var dic = new Dictionary<string, int>();
                        foreach (var subField in field.Fields)
                        {
                            if (string.IsNullOrWhiteSpace(subField.Name))
                                throw new NullReferenceException("StringList: field name is empty!");

                            if (subField.Size == 0)
                                throw new NullReferenceException("StringList: field <" + subField.Name + "> has a size of zero!");

                            if (subField.Type != DataType.String && subField.Type != DataType.String2)
                                throw new NotImplementedException("StringList: field <" + subField.Name + "|" + subField.Type + "> is not supported!");

                            dic[subField.Name] = (int)mReadUInt32(subField.Size);
                        }

                        foreach (var subField in field.Fields)
                        {
                            var len = dic[subField.Name];
                            SetVal(subField, read ? base.ReadString2(len) : null, true);
                        }

                        dic.Clear();
                    } break;
                default:
                    break;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (valList != null)
            {
                valList.Clear();
                valList = null;
            }

            if (stringTable != null)
            {
                stringTable.Clear();
                stringTable = null;
            }
        }
    }
}
