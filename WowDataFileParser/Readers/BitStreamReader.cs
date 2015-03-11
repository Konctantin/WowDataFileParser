using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MS.Internal.Ink
{
    // import from MS.Internal.Ink.BitStreamReader
    public class BitStreamReader : IDisposable
    {
        public byte[] Buffer { get; private set; }
        public int Index { get; private set; }

        private uint countBits;
        private byte partialByte;
        private int cbitsInPartialByte;

        public bool EndOfStream
        {
            get { return 0u == this.countBits; }
        }

        public int Remains
        {
            get { return Buffer.Length - Index; }
        }

        public BitStreamReader(byte[] buffer)
        {
            this.Buffer = buffer;
            this.countBits = (uint)(buffer.Length * 8);
        }

        public BitStreamReader(byte[] buffer, int startIndex)
        {
            if (startIndex < 0 || startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException("startIndex");

            this.Buffer = buffer;
            this.Index = startIndex;
            this.countBits = (uint)((buffer.Length - startIndex) * 8);
        }

        public BitStreamReader(byte[] buffer, uint bufferLengthInBits)
            : this(buffer)
        {
            if ((ulong)bufferLengthInBits > (ulong)((long)(buffer.Length * 8)))
                throw new ArgumentOutOfRangeException("bufferLengthInBits", "Invalid buffer length");

            this.countBits = bufferLengthInBits;
        }

        protected long mReadUInt64(int countOfBits)
        {
            if (countOfBits > 64 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "Count of bits out of range");

            long num = 0L;
            while (countOfBits > 0)
            {
                int num2 = 8;
                if (countOfBits < 8)
                    num2 = countOfBits;

                num <<= num2;
                byte b = this.mReadByte(num2);
                num |= (long)((ulong)b);
                countOfBits -= num2;
            }
            return num;
        }

        protected ushort mReadUInt16(int countOfBits)
        {
            if (countOfBits > 16 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "Count of bits out of range");

            ushort num = 0;
            while (countOfBits > 0)
            {
                int num2 = 8;
                if (countOfBits < 8)
                    num2 = countOfBits;

                num = (ushort)(num << num2);
                byte b = this.mReadByte(num2);
                num |= (ushort)b;
                countOfBits -= num2;
            }
            return num;
        }

        protected uint mReadUInt16Reverse(int countOfBits)
        {
            if (countOfBits > 16 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "Count of bits out of range");

            ushort num = 0;
            int num2 = 0;
            while (countOfBits > 0)
            {
                int num3 = 8;
                if (countOfBits < 8)
                    num3 = countOfBits;

                ushort num4 = (ushort)this.mReadByte(num3);
                num4 = (ushort)(num4 << num2 * 8);
                num |= num4;
                num2++;
                countOfBits -= num3;
            }
            return (uint)num;
        }

        protected uint mReadUInt32(int countOfBits)
        {
            if (countOfBits > 32 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "Count of bits out of range");

            uint num = 0u;
            while (countOfBits > 0)
            {
                int num2 = 8;
                if (countOfBits < 8)
                    num2 = countOfBits;

                num <<= num2;
                byte b = this.mReadByte(num2);
                num |= (uint)b;
                countOfBits -= num2;
            }
            return num;
        }

        protected uint mReadUInt32Reverse(int countOfBits)
        {
            if (countOfBits > 32 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "Count of bits out of range");

            uint num = 0u;
            int num2 = 0;
            while (countOfBits > 0)
            {
                int num3 = 8;
                if (countOfBits < 8)
                    num3 = countOfBits;

                uint num4 = (uint)this.mReadByte(num3);
                num4 <<= num2 * 8;
                num |= num4;
                num2++;
                countOfBits -= num3;
            }
            return num;
        }

        public byte ReadBit()
        {
            return this.mReadByte(1);
        }

        protected byte mReadByte(int countOfBits)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (countOfBits > 8 || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "Count of bits out of range");

            if ((long)countOfBits > (long)((ulong)this.countBits))
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, "Count of bits great than remaining bits " + this.countBits);

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
                byte b2 = this.Buffer[this.Index];
                this.Index++;
                int num2 = 8 - countOfBits;
                b = (byte)(this.partialByte >> num2);
                int num3 = Math.Abs(countOfBits - this.cbitsInPartialByte - 8);
                b |= (byte)(b2 >> num3);
                this.partialByte = (byte)(b2 << countOfBits - this.cbitsInPartialByte);
                this.cbitsInPartialByte = 8 - (countOfBits - this.cbitsInPartialByte);
            }
            return b;
        }

        #region Extensions

        public byte ReadByte(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 1)
                    throw new ArgumentOutOfRangeException("count", 1, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = Buffer[Index];
                ++Index;
                return val;
            }
            else
                return mReadByte(count);
        }

        public sbyte ReadSByte(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 1)
                    throw new ArgumentOutOfRangeException("count", 1, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = (sbyte)Buffer[Index];
                ++Index;
                return val;
            }
            else
                return (sbyte)mReadByte(count);
        }

        public short ReadInt16(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 2)
                    throw new ArgumentOutOfRangeException("count", 2, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = BitConverter.ToInt16(Buffer, Index);
                Index += 2;
                return val;
            }
            else
                return unchecked((short)mReadUInt16(count));
        }

        public ushort ReadUInt16(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 2)
                    throw new ArgumentOutOfRangeException("count", 2, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = BitConverter.ToUInt16(Buffer, Index);
                Index += 2;
                return val;
            }
            else
                return mReadUInt16(count);
        }

        public int ReadInt32(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 4)
                    throw new ArgumentOutOfRangeException("count", 4, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = BitConverter.ToInt32(Buffer, Index);
                Index += 4;
                return val;
            }
            else
                return unchecked((int)mReadUInt32(count));
        }

        public uint ReadUInt32(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 4)
                    throw new ArgumentOutOfRangeException("count", 4, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = BitConverter.ToUInt32(Buffer, Index);
                Index += 4;
                return val;
            }
            else
                return mReadUInt32(count);
        }

        public long ReadInt64(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 8)
                    throw new ArgumentOutOfRangeException("count", 8, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = BitConverter.ToInt64(Buffer, Index);
                Index += 8;
                return val;
            }
            else
                return mReadUInt64(count);
        }

        public ulong ReadUInt64(int count = 0)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count == 0)
            {
                if (Buffer.Length - Index < 8)
                    throw new ArgumentOutOfRangeException("count", 8, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

                var val = BitConverter.ToUInt64(Buffer, Index);
                Index += 8;
                return val;
            }
            else
                return unchecked((ulong)mReadUInt64(count));
        }

        public float ReadFloat()
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (Buffer.Length - Index < 4)
                throw new ArgumentOutOfRangeException("count", 4, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

            var val = BitConverter.ToSingle(Buffer, Index);
            Index += 4;
            return val;
        }

        public double ReadDouble()
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (Buffer.Length - Index < 8)
                throw new ArgumentOutOfRangeException("count", 8, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

            var val = BitConverter.ToDouble(Buffer, Index);
            Index += 8;
            return val;
        }

        public int ReadSize(int count)
        {
            if (count < 8)
                return ReadByte(count);
            else if (count == 8)
                return ReadByte();
            else if (count > 8 && count < 16)
                return ReadInt16(count);
            else if (count == 16)
                return ReadInt16();
            else if (count > 16 && count < 32)
                return ReadInt32(count);
            else if (count == 32)
                return ReadInt32();
            return 0;
        }

        public string ReadString(int len)
        {
            if (len == 0)
                return ReadCString();
            else
                return ReadString2(len);
        }

        public string ReadString3(int count)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (count <= 1)
                return string.Empty;

            if (Buffer.Length - Index < count)
                throw new ArgumentOutOfRangeException("count", count, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

            var str = Encoding.UTF8.GetString(Buffer, Index, count);
            Index += (int)count;
            return (str ?? "").TrimEnd('\0');
        }

        public string ReadString2(int count)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (Buffer.Length - Index < count)
                throw new ArgumentOutOfRangeException("count", count, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

            if (count <= 1)
            {
                if (count == 1)
                    ++Index;
                return string.Empty;
            }

            var str = Encoding.UTF8.GetString(Buffer, Index, count);
            Index += (int)count;
            return (str ?? "").TrimEnd('\0');
        }

        public string ReadCString()
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            int start = Index;
            while (Buffer[Index++] != 0);
            var str = Encoding.UTF8.GetString(Buffer, start, Index - start);
            return (str ?? "").TrimEnd('\0');
        }

        public string ReadPString(int count)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (Buffer.Length - Index < count)
                throw new ArgumentOutOfRangeException("count", count, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

            var len = ReadSize(count);
            var str = Encoding.UTF8.GetString(Buffer, Index, len);
            Index += len;
            return (str ?? "").TrimEnd('\0');
        }

        public string ReadReverseString(int count)
        {
            if (this.EndOfStream)
                throw new EndOfStreamException("End of stream reached");

            if (Buffer.Length - Index < count)
                throw new ArgumentOutOfRangeException("count", count, "Count of bytes great than remaining bytes " + (Buffer.Length - Index));

            var buff = new byte[count];
            Array.Copy(Buffer, Index, buff, 0, count);
            Index += count;
            return Encoding.ASCII.GetString(buff.Reverse().ToArray());
        }

        #endregion

        public virtual void Dispose()
        {
            this.Buffer = null;
            this.Index  = 0;
            this.countBits   = 0;
            this.partialByte = 0;
            this.cbitsInPartialByte = 0;
        }
    }
}