using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WowDataFileParser
{
    internal class BitStreamReader
    {
        private byte[] buffer;
        private uint countBits;
        private int index;
        private byte partialByte;
        private int cbitsInPartialByte;

        internal bool EndOfStream
        {
            get { return 0u == this.countBits; }
        }

        internal int CurrentIndex
        {
            get { return this.index - 1; }
        }

        public int RemainigLength
        {
            get { return this.buffer.Length - this.index; }
        }

        internal BitStreamReader(byte[] buffer)
        {
            this.buffer = buffer;
            this.countBits = (uint)(buffer.Length * 8);
        }

        internal BitStreamReader(byte[] buffer, int startIndex)
        {
            if (startIndex < 0 || startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException("startIndex");

            this.buffer = buffer;
            this.index = startIndex;
            this.countBits = (uint)((buffer.Length - startIndex) * 8);
        }

        internal BitStreamReader(byte[] buffer, uint bufferLengthInBits)
            : this(buffer)
        {
            if ((ulong)bufferLengthInBits > (ulong)((long)(buffer.Length * 8)))
                throw new ArgumentOutOfRangeException("bufferLengthInBits", "InvalidBufferLength");

            this.countBits = bufferLengthInBits;
        }

        internal long ReadUInt64(int countOfBits)
        {
            if (countOfBits > 64 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "CountOfBitsOutOfRange");

            long num = 0L;
            while (countOfBits > 0)
            {
                int num2 = 8;
                if (countOfBits < 8)
                    num2 = countOfBits;

                num <<= num2;
                byte b = this.ReadByte(num2);
                num |= (long)((ulong)b);
                countOfBits -= num2;
            }
            return num;
        }

        internal ushort ReadUInt16(int countOfBits)
        {
            if (countOfBits > 16 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "CountOfBitsOutOfRange");

            ushort num = 0;
            while (countOfBits > 0)
            {
                int num2 = 8;
                if (countOfBits < 8)
                    num2 = countOfBits;

                num = (ushort)(num << num2);
                byte b = this.ReadByte(num2);
                num |= (ushort)b;
                countOfBits -= num2;
            }
            return num;
        }

        internal uint ReadUInt16Reverse(int countOfBits)
        {
            if (countOfBits > 16 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "CountOfBitsOutOfRange");

            ushort num = 0;
            int num2 = 0;
            while (countOfBits > 0)
            {
                int num3 = 8;
                if (countOfBits < 8)
                    num3 = countOfBits;

                ushort num4 = (ushort)this.ReadByte(num3);
                num4 = (ushort)(num4 << num2 * 8);
                num |= num4;
                num2++;
                countOfBits -= num3;
            }
            return (uint)num;
        }

        internal uint ReadUInt32(int countOfBits)
        {
            if (countOfBits > 32 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "CountOfBitsOutOfRange");

            uint num = 0u;
            while (countOfBits > 0)
            {
                int num2 = 8;
                if (countOfBits < 8)
                    num2 = countOfBits;

                num <<= num2;
                byte b = this.ReadByte(num2);
                num |= (uint)b;
                countOfBits -= num2;
            }
            return num;
        }

        internal uint ReadUInt32Reverse(int countOfBits)
        {
            if (countOfBits > 32 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "CountOfBitsOutOfRange");

            uint num = 0u;
            int num2 = 0;
            while (countOfBits > 0)
            {
                int num3 = 8;
                if (countOfBits < 8)
                    num3 = countOfBits;

                uint num4 = (uint)this.ReadByte(num3);
                num4 <<= num2 * 8;
                num |= num4;
                num2++;
                countOfBits -= num3;
            }
            return num;
        }

        internal bool ReadBit()
        {
            byte b = this.ReadByte(1);
            return (b & 1) == 1;
        }

        internal byte ReadByte(int countOfBits)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("EndOfStreamReached");

            if (countOfBits > 8 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "CountOfBitsOutOfRange");

            if ((long)countOfBits > (long)((ulong)this.countBits))
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "CountOfBitsGreatThanRemainingBits");

            this.countBits -= (uint)countOfBits;
            byte b;

            if (this.cbitsInPartialByte >= countOfBits)
            {
                int num = 8 - countOfBits;
                b = (byte)(this.partialByte >> num);
                this.partialByte = (byte)(this.partialByte << countOfBits);
                this.cbitsInPartialByte -= countOfBits;
            }
            else
            {
                byte b2 = this.buffer[this.index];
                this.index++;
                int num2 = 8 - countOfBits;
                b = (byte)(this.partialByte >> num2);
                int num3 = Math.Abs(countOfBits - this.cbitsInPartialByte - 8);
                b |= (byte)(b2 >> num3);
                this.partialByte = (byte)(b2 << countOfBits - this.cbitsInPartialByte);
                this.cbitsInPartialByte = 8 - (countOfBits - this.cbitsInPartialByte);
            }
            return b;
        }

        public void SetPosition(int pos)
        {
            this.index = pos;
        }

        internal string ReadPString(int hsize)
        {
            var size = ReadUInt32(hsize);

            var str = Encoding.UTF8.GetString(buffer, index, (int)size);
            index += (int)size;
            return str;
        }

        internal string ReadString(int hsize)
        {
            var str = Encoding.UTF8.GetString(buffer, index, (int)hsize);
            index += (int)hsize;
            return str;
        }

        internal unsafe byte ReadByte()
        {
            var val = ReadUInt32Reverse(8);
            return *(byte*)&val;
        }

        internal byte[] ReadBytes(int count)
        {
            byte[] arr = new byte[count];
            for (int i = 0; i < count; ++i)
                arr[i] = ReadByte(8);
            return arr;
        }

        internal unsafe short ReadInt16()
        {
            var val = ReadUInt32Reverse(16);
            return *(short*)&val;
        }

        internal unsafe int ReadInt32()
        {
            var val = ReadUInt32Reverse(32);
            return *(int*)&val;
        }

        internal unsafe float ReadFloat()
        {
            var val = ReadUInt32Reverse(32);
            return *(float*)&val;
        }

        public string ReadCString()
        {
            List<byte> list = new List<byte>();
            byte b;
            while ((b = this.ReadByte()) != 0)
                list.Add(b);
            return Encoding.UTF8.GetString(list.ToArray());
        }

        public string ReadPascalString(int len)
        {
            var bytes = new byte[len];
            for (int i = 0; i < len; ++i)
                bytes[i] = this.ReadByte();
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
