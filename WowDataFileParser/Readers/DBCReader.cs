/* tomrus88 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WowDataFileParser.Readers
{
    public class DbcReader : BaseReader
    {
        private const uint HeaderSize = 20;

        public DbcReader(string fileName)
            : base(fileName)
        {
            this.StringTable = new Dictionary<int, string>();

            if (reader.BaseStream.Length < HeaderSize)
                throw new InvalidDataException(string.Format("File {0} is corrupted!", new FileInfo(fileName).Name));

            if (this.Magic != "WDBC")
                throw new InvalidDataException(string.Format("File {0} isn't valid DBC file!", new FileInfo(fileName).Name));

            RecordsCount    = reader.ReadInt32();
            FieldsCount     = reader.ReadInt32();
            RecordSize      = reader.ReadInt32();
            StringTableSize = reader.ReadInt32();

            Locale = "xxXX";

            base.ReadData();
        }
    }
}
