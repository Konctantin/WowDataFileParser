using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WowDataFileParser.Readers
{
    public enum Locale : uint
    {
        enGB = 1,
        koKR = 2,
        frFR = 3,
        deDE = 4,
        zhCN = 5,
        zhTW = 6,
        esES = 7,
        esMX = 8,
        ruRU = 0x100,
        All  = 0xFFF,
    };

    public abstract class BaseReader : IDisposable
    {
        protected BinaryReader reader;
        public List<byte[]> Rows    { get; protected set; }

        public string Magic         { get; protected set; }
        public int RecordsCount     { get; protected set; }
        public int FieldsCount      { get; protected set; }
        public int RecordSize       { get; protected set; }
        public int StringTableSize  { get; protected set; }
        public uint Build           { get; protected set; }
        public string Locale        { get; protected set; }

        public Dictionary<int, string> StringTable { get; protected set; }

        public byte[] this[int row] 
        {
            get { return this.Rows[row]; }
        }

        public BaseReader(string fileName)
        {
            this.Rows   = new List<byte[]>();
            this.reader = new BinaryReader(new FileStream(fileName, FileMode.Open), Encoding.UTF8);

            if (this.reader.BaseStream.Length <= 4)
                throw new InvalidDataException(string.Format("File {0} is corrupted!", new FileInfo(fileName).Name));

            this.Magic = reader.ReadReverseString(4);
        }

        public virtual void ReadData()
        {
            for (int i = 0; i < RecordsCount; i++)
                this.Rows.Add(reader.ReadBytes(RecordSize));

            int stringTableStart = (int)reader.BaseStream.Position;

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                int index = (int)reader.BaseStream.Position - stringTableStart;
                StringTable[index] = reader.ReadCString();
            }
        }

        ~BaseReader()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Close();
                this.reader.Dispose();
            }
            this.reader = null;

            if (this.Rows != null)
                this.Rows.Clear();
            this.Rows = null;

            if (this.StringTable != null)
                this.StringTable.Clear();
            this.StringTable = null;
        }
    }
}
