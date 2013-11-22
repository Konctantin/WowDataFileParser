using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kamilla.IO;

namespace WDReader.Reader
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
        ruRU = 256,
    };

    public abstract class BaseReader : IDisposable
    {
        protected StreamHandler reader;
        protected Dictionary<int, byte[]> m_rows;

        public uint Magic           { get; protected set; }
        public int RecordsCount     { get; protected set; }
        public int FieldsCount      { get; protected set; }
        public int RecordSize       { get; protected set; }
        public int StringTableSize  { get; protected set; }
        public uint Build           { get; protected set; }
        public string Locale        { get; protected set; }

        public Dictionary<int, string> StringTable { get; protected set; }

        public StreamHandler this[int row] 
        {
            get { return new StreamHandler(new MemoryStream(m_rows.ElementAt(row).Value), Encoding.UTF8); } 
        }

        public BaseReader(string fileName)
        {
            this.m_rows = new Dictionary<int, byte[]>();
            this.reader = new StreamHandler(new FileStream(fileName, FileMode.Open), Encoding.UTF8);

            if (this.reader.BaseStream.Length <= 4)
                throw new InvalidDataException(string.Format("File {0} is corrupted!", new FileInfo(fileName).Name));

            this.Magic = reader.ReadUInt32();
        }

        public virtual void ReadData()
        {
            for (int i = 0; i < RecordsCount; i++)
                m_rows[i] = reader.ReadBytes(RecordSize);

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
            if (reader != null)
            {
                reader.Close();
                reader.Dispose();                
            }
            reader = null;

            if (m_rows != null)
                m_rows.Clear();
            m_rows = null;

            if (StringTable != null)
                StringTable.Clear();
            StringTable = null;
        }

        public byte[] GetRowAsByteArray(int row)
        {
            if (row < 0)
                return new byte[0];

            return m_rows.ElementAt(row).Value;
        }
    }
}
