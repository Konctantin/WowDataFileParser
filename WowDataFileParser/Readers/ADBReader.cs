/* tomrus88 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WowDataFileParser.Readers
{
    public class AdbReader : BaseReader
    {
        private const int HeaderSize = 48;

        public AdbReader(string fileName)
            : base(fileName)
        {
            this.StringTable = new Dictionary<int, string>();

            if (reader.BaseStream.Length < HeaderSize)
                throw new InvalidDataException(string.Format("File {0} is corrupted!", new FileInfo(fileName).Name));

            if (Magic != "2HCW")
                throw new InvalidDataException(string.Format("File {0} isn't valid ADB file!", new FileInfo(fileName).Name));

            RecordsCount    = reader.ReadInt32();
            FieldsCount     = reader.ReadInt32();                       // not fields count in WCH2
            RecordSize      = reader.ReadInt32();
            StringTableSize = reader.ReadInt32();

            // WCH2 specific fields
            uint tableHash  = reader.ReadUInt32();                      // new field in WCH2
            Build           = reader.ReadUInt32();                      // new field in WCH2

            int unk1        = reader.ReadInt32();                       // Unix time in WCH2
            int unk2        = reader.ReadInt32();                       // new field in WCH2
            int unk3        = reader.ReadInt32();                       // new field in WCH2 (index table?)
            Locale          = ((Locale)reader.ReadInt32()).ToString();
            int unk5        = reader.ReadInt32();                       // new field in WCH2

            if (unk3 != 0)
            {
                reader.ReadBytes(unk3 * 4 - HeaderSize);                // an index for rows
                reader.ReadBytes(unk3 * 2 - HeaderSize * 2);            // a memory allocation bank
            }

            base.ReadData();
        }
    }
}
