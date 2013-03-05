using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace WowDataFileParser
{
    class ADBReader : IWowClientDBReader
    {
        private const int HeaderSize = 48;
        private const uint ADBFmtSig = 0x32484357;          // WCH2

        public int RecordsCount     { get; private set; }
        public int FieldsCount      { get; private set; }
        public int RecordSize       { get; private set; }
        public int StringTableSize  { get; private set; }
        public uint Build           { get; private set; }
        public string Locale        { get; private set; }

        public Dictionary<int, string> StringTable { get; private set; }
        
        private byte[][] m_rows;

        public byte[] GetRowAsByteArray(int row)
        {
            return m_rows[row];
        }

        public BinaryReader this[int row]
        {
            get { return new BinaryReader(new MemoryStream(m_rows[row]), Encoding.UTF8); }
        }

        public ADBReader(string fileName)
        {
            using (var reader = new BinaryReader(new FileStream(fileName, FileMode.Open), Encoding.UTF8))
            {
                if (reader.BaseStream.Length < HeaderSize)
                {
                    throw new InvalidDataException(String.Format("File {0} is corrupted!", fileName.GetFileName()));
                }

                var signature = reader.ReadUInt32();

                if (signature != ADBFmtSig)
                {
                    throw new InvalidDataException(String.Format("File {0} isn't valid DBC file!", fileName.GetFileName()));
                }

                RecordsCount    = reader.ReadInt32();
                FieldsCount     = reader.ReadInt32(); // not fields count in WCH2
                RecordSize      = reader.ReadInt32();
                StringTableSize = reader.ReadInt32();

                // WCH2 specific fields
                uint tableHash  = reader.ReadUInt32(); // new field in WCH2
                Build           = reader.ReadUInt32(); // new field in WCH2

                int unk1        = reader.ReadInt32(); // Unix time in WCH2
                int unk2        = reader.ReadInt32(); // new field in WCH2
                int unk3        = reader.ReadInt32(); // new field in WCH2 (index table?)
                Locale          = ((Locale)reader.ReadInt32()).ToString();
                //int locale      = reader.ReadInt32(); // new field in WCH2
                int unk5        = reader.ReadInt32(); // new field in WCH2

                if (unk3 != 0)
                {
                    reader.ReadBytes(unk3 * 4 - HeaderSize);     // an index for rows
                    reader.ReadBytes(unk3 * 2 - HeaderSize * 2); // a memory allocation bank
                }

                m_rows = new byte[RecordsCount][];

                for (int i = 0; i < RecordsCount; i++)
                    m_rows[i] = reader.ReadBytes(RecordSize);

                int stringTableStart = (int)reader.BaseStream.Position;

                StringTable = new Dictionary<int, string>();

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    int index = (int)reader.BaseStream.Position - stringTableStart;
                    StringTable[index] = reader.ReadCString();
                }
            }
        }
    }
}
