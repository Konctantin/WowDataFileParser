using System.Collections.Generic;
using System.IO;

namespace WowDataFileParser
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

    interface IWowClientDBReader
    {
        int RecordsCount            { get; }
        int FieldsCount             { get; }
        int RecordSize              { get; }
        int StringTableSize         { get; }
        uint Build                  { get; }
        string Locale               { get; }
        Dictionary<int, string> StringTable { get; }
        BinaryReader this[int row]  { get; }
        byte[] GetRowAsByteArray(int row);
    }
}
