/* tomrus88 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WowDataFileParser.Readers
{
    public class Db2Reader : BaseReader
    {
        private const int HeaderSize = 48;
        private const uint DB2FmtSig = 0x32424457;          // WDB2

        public Db2Reader(string fileName)
            : base(fileName)
        {
            this.StringTable = new Dictionary<int, string>();

            if (reader.BaseStream.Length < HeaderSize)
                throw new InvalidDataException(string.Format("File {0} is corrupted!", new FileInfo(fileName).Name));

            if (this.Magic != DB2FmtSig)
                throw new InvalidDataException(string.Format("File {0} isn't valid DBC file!", new FileInfo(fileName).Name));

            RecordsCount        = reader.ReadInt32();
            FieldsCount         = reader.ReadInt32();
            RecordSize          = reader.ReadInt32();
            StringTableSize     = reader.ReadInt32();

            // WDB2 specific fields
            uint tableHash      = reader.ReadUInt32();   // new field in WDB2
            this.Build          = reader.ReadUInt32();   // new field in WDB2
            uint unk1           = reader.ReadUInt32();   // new field in WDB2

            if (this.Build > 12880) // new extended header
            {
                int MinId       = reader.ReadInt32();    // new field in WDB2
                int MaxId       = reader.ReadInt32();    // new field in WDB2
                this.Locale     = ((Locale)reader.ReadUInt32()).ToString();
#warning small hack
                if (this.Locale == "")
                    Locale = "xxXX";

                int unk5        = reader.ReadInt32();    // new field in WDB2

                if (MaxId != 0)
                {
                    var diff = MaxId - MinId + 1;   // blizzard is weird people...
                    reader.ReadBytes(diff * 4);     // an index for rows
                    reader.ReadBytes(diff * 2);     // a memory allocation bank
                }
            }
            else
                Locale = "xxXX";

            base.ReadData();
        }
    }
}
