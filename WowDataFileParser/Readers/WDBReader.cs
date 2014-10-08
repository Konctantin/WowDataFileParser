/* tomrus88 */
using System;
using System.IO;
using System.Linq;

namespace WowDataFileParser.Readers
{
    public class WdbReader : BaseReader
    {
        private const int HeaderSize = 24;
        private string[] WDBSigs = new [] {
            "WMOB", // creaturecache.wdb
            "WGOB", // gameobjectcache.wdb
            "WIDB", // itemcache.wdb
            "WNDB", // itemnamecache.wdb
            "WITX", // itemtextcache.wdb
            "WNPC", // npccache.wdb
            "WPTX", // pagetextcache.wdb
            "WQST", // questcache.wdb
            "WPTN", // petitioncache.wdb
            "WRDN"  // wowcache.wdb
        };

        public WdbReader(string fileName) 
            : base(fileName)
        {
            if (reader.BaseStream.Length < HeaderSize)
                throw new InvalidDataException(string.Format("File {0} is corrupted!", new FileInfo(fileName).Name));

            if (!WDBSigs.Contains(this.Magic))
                throw new InvalidDataException(string.Format("File {0} isn't valid WDB file!", new FileInfo(fileName).Name));

            Build  = reader.ReadUInt32();
            Locale = reader.ReadReverseString(4);

            var unk1    = reader.ReadInt32();
            var unk2    = reader.ReadInt32();
            var version = reader.ReadInt32();

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var entry = reader.ReadInt32();
                var size  = reader.ReadInt32();

                if ((entry <= 0 || size <= 0) || reader.BaseStream.Position == reader.BaseStream.Length)
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
