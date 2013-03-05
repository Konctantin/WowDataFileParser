﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WowDataFileParser
{
    class DBCReader : IWowClientDBReader
    {
        private const uint HeaderSize = 20;
        private const uint DBCFmtSig = 0x43424457;          // WDBC

        public int RecordsCount         { get; private set; }
        public int FieldsCount          { get; private set; }
        public int RecordSize           { get; private set; }
        public int StringTableSize      { get; private set; }
        public uint Build               { get; private set; }
        public string Locale            { get; private set; }

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

        public DBCReader(string fileName)
        {
            using (var reader = new BinaryReader(new FileStream(fileName, FileMode.Open), Encoding.UTF8))
            {
                if (reader.BaseStream.Length < HeaderSize)
                {
                    throw new InvalidDataException(String.Format("File {0} is corrupted!", fileName.GetFileName()));
                }

                if (reader.ReadUInt32() != DBCFmtSig)
                {
                    throw new InvalidDataException(String.Format("File {0} isn't valid DBC file!", fileName.GetFileName()));
                }

                RecordsCount = reader.ReadInt32();
                FieldsCount = reader.ReadInt32();
                RecordSize = reader.ReadInt32();
                StringTableSize = reader.ReadInt32();

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
