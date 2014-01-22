/* tomrus88 */
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WowDataFileParser.Readers
{
    public class WdbReader : BaseReader
    {
        private const int HeaderSize = 24;
        private uint[] WDBSigs = new uint[] {
            0x574D4F42, // creaturecache.wdb
            0x57474F42, // gameobjectcache.wdb
            0x57494442, // itemcache.wdb
            0x574E4442, // itemnamecache.wdb
            0x57495458, // itemtextcache.wdb
            0x574E5043, // npccache.wdb
            0x57505458, // pagetextcache.wdb
            0x57515354, // questcache.wdb
            0x5752444E  // wowcache.wdb
        };

        public WdbReader(string fileName) 
            : base(fileName)
        {
            if (reader.BaseStream.Length < HeaderSize)
                throw new InvalidDataException(string.Format("File {0} is corrupted!", new FileInfo(fileName).Name));

            if (!WDBSigs.Contains(this.Magic))
                throw new InvalidDataException(string.Format("File {0} isn't valid WDB file!", new FileInfo(fileName).Name));

            Build  = reader.ReadUInt32();
            Locale = Encoding.ASCII.GetString(reader.ReadBytes(4).Reverse().ToArray());

            var unk1    = reader.ReadInt32();
            var unk2    = reader.ReadInt32();
            var version = reader.ReadInt32();

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var entry = reader.ReadInt32();
                var size  = reader.ReadInt32();

                if ((entry == 0 && size == 0) || reader.BaseStream.Position == reader.BaseStream.Length)
                    break;

                var row = new byte[0]
                    .Concat(BitConverter.GetBytes(entry))
                    .Concat(reader.ReadBytes(size))
                    .ToArray();

                m_rows.Add(entry, row);
            }

            RecordsCount = m_rows.Count;
        }
    }
}
