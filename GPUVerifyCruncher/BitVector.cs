//===-----------------------------------------------------------------------==//
//
//                GPUVerify - a Verifier for GPU Kernels
//
// This file is distributed under the Microsoft Public License.  See
// LICENSE.TXT for details.
//
//===----------------------------------------------------------------------===//

namespace GPUVerify
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Boogie;

    public class BitVector
    {
        public static readonly BitVector False = new BitVector(0, 1);
        public static readonly BitVector True = new BitVector(1, 1);

        public string Bits { get; private set; }

        private static readonly Dictionary<int, BitVector> ZeroBVs = new Dictionary<int, BitVector>();
        private static readonly Dictionary<int, BitVector> MaxBVs = new Dictionary<int, BitVector>();

        public BitVector(int val, int width = 32)
        {
            Bits = Convert.ToString(val, 2);
            Pad(width, '0');
        }

        public BitVector(long val, int width = 64)
        {
            Bits = Convert.ToString(val, 2);
            Pad(width, '0');
        }

        public BitVector(string bits)
        {
            Bits = bits;
        }

        public BitVector(BvConst bv)
        {
            // Create bit-string representation
            string str = bv.ToReadableString();
            string bareStr = str.Substring(0, str.IndexOf("bv"));
            if (bareStr.StartsWith("0x"))
            {
                bareStr = bareStr.Replace("0x", string.Empty).Replace(".", string.Empty);
                for (int i = 0; i < bareStr.Length; ++i)
                {
                    Bits += HexToBinary(bareStr[i]);
                }
            }
            else
            {
                int val = Convert.ToInt32(bareStr);
                Bits = Convert.ToString(val, 2);
            }

            Pad(bv.Bits, '0');
        }

        private void Pad(int width, char bit)
        {
            if (Bits.Length < width)
                Bits = Bits.PadLeft(width, bit);
        }

        private static string HexToBinary(char hex)
        {
            switch (hex)
            {
                case '0':
                    return "0000";
                case '1':
                    return "0001";
                case '2':
                    return "0010";
                case '3':
                    return "0011";
                case '4':
                    return "0100";
                case '5':
                    return "0101";
                case '6':
                    return "0110";
                case '7':
                    return "0111";
                case '8':
                    return "1000";
                case '9':
                    return "1001";
                case 'a':
                case 'A':
                    return "1010";
                case 'b':
                case 'B':
                    return "1011";
                case 'c':
                case 'C':
                    return "1100";
                case 'd':
                case 'D':
                    return "1101";
                case 'e':
                case 'E':
                    return "1110";
                case 'f':
                case 'F':
                    return "1111";
                default:
                    Print.ExitMessage("Unhandled hex character " + hex);
                    return string.Empty;
            }
        }

        public int ConvertToInt32()
        {
            try
            {
                return Convert.ToInt32(Bits, 2);
            }
            catch (OverflowException)
            {
                throw;
            }
        }

        public long ConvertToInt64()
        {
            try
            {
                return Convert.ToInt64(Bits, 2);
            }
            catch (OverflowException)
            {
                throw;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            BitVector item = obj as BitVector;
            if ((object)item == null)
                return false;
            return Bits.Equals(item.Bits);
        }

        public override int GetHashCode()
        {
            return Bits.GetHashCode();
        }

        public override string ToString()
        {
            return Bits;
        }

        public static BitVector Zero(int width)
        {
            if (!ZeroBVs.ContainsKey(width))
            {
                if (width <= 32)
                    ZeroBVs[width] = new BitVector(0, width);
                else
                    ZeroBVs[width] = new BitVector((long)0, width);
            }

            return ZeroBVs[width];
        }

        public static BitVector Max(int width)
        {
            if (!MaxBVs.ContainsKey(width))
            {
                if (width <= 32)
                    MaxBVs[width] = new BitVector((int)Math.Pow(2, width) - 1, width);
                else
                    MaxBVs[width] = new BitVector((long)Math.Pow(2, width) - 1, width);
            }

            return MaxBVs[width];
        }

        public static BitVector operator +(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "+ operator : Bit vectors must have equal widths");
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return new BitVector(aData + bData, width);
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return new BitVector(aData + bData, width);
            }
        }

        public static BitVector operator -(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "- operator : Bit vectors must have equal widths");
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return new BitVector(aData - bData, width);
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return new BitVector(aData - bData, width);
            }
        }

        public static BitVector operator *(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "* operator : Bit vectors must have equal widths");
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return new BitVector(aData * bData, width);
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return new BitVector(aData * bData, width);
            }
        }

        public static BitVector operator /(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "/ operator : Bit vectors must have equal widths");
            int width = a.Bits.Length;
            try
            {
                if (width <= 32)
                {
                    int aData = Convert.ToInt32(a.Bits, 2);
                    int bData = Convert.ToInt32(b.Bits, 2);
                    return new BitVector(checked(aData / bData), width);
                }
                else
                {
                    long aData = Convert.ToInt64(a.Bits, 2);
                    long bData = Convert.ToInt64(b.Bits, 2);
                    return new BitVector(checked(aData / bData), width);
                }
            }
            catch (DivideByZeroException)
            {
                throw;
            }
        }

        public static BitVector operator %(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "% operator : Bit vectors must have equal widths");
            int width = a.Bits.Length;
            try
            {
                if (width <= 32)
                {
                    int aData = Convert.ToInt32(a.Bits, 2);
                    int bData = Convert.ToInt32(b.Bits, 2);
                    return new BitVector(checked(aData % bData), width);
                }
                else
                {
                    long aData = Convert.ToInt64(a.Bits, 2);
                    long bData = Convert.ToInt64(b.Bits, 2);
                    return new BitVector(checked(aData % bData), width);
                }
            }
            catch (DivideByZeroException)
            {
                throw;
            }
        }

        public static BitVector operator &(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "& operator : Bit vectors must have equal widths");
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return new BitVector(aData & bData, width);
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return new BitVector(aData & bData, width);
            }
        }

        public static BitVector operator |(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "| operator : Bit vectors must have equal widths");
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return new BitVector(aData | bData, width);
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return new BitVector(aData | bData, width);
            }
        }

        public static BitVector operator ^(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "^ operator : Bit vectors must have equal widths");
            char[] bits = new char[a.Bits.Length];
            for (int i = 0; i < a.Bits.Length; ++i)
            {
                if (a.Bits[i] == '1' && b.Bits[i] == '0')
                    bits[i] = '1';
                else if (a.Bits[i] == '0' && b.Bits[i] == '1')
                    bits[i] = '1';
                else
                    bits[i] = '0';
            }

            return new BitVector(new string(bits));
        }

        public static BitVector operator >>(BitVector a, int shift)
        {
            int width = a.Bits.Length;
            try
            {
                if (width <= 32)
                {
                    int aData = Convert.ToInt32(a.Bits, 2);
                    return new BitVector(checked(aData >> shift), width);
                }
                else
                {
                    long aData = Convert.ToInt64(a.Bits, 2);
                    return new BitVector(checked(aData >> shift), width);
                }
            }
            catch (OverflowException)
            {
                throw;
            }
        }

        public static BitVector LogicalShiftRight(BitVector a, int shift)
        {
            int endIndex = a.Bits.Length - shift;

            if (endIndex > 0)
            {
                string bits = a.Bits.Substring(0, endIndex);
                bits = bits.PadLeft(a.Bits.Length, '0');
                return new BitVector(bits);
            }
            else
            {
                return new BitVector(0, a.Bits.Length);
            }
        }

        public static BitVector operator <<(BitVector a, int shift)
        {
            int width = a.Bits.Length;
            try
            {
                if (width <= 32)
                {
                    int aData = Convert.ToInt32(a.Bits, 2);
                    return new BitVector(checked(aData << shift), width);
                }
                else
                {
                    long aData = Convert.ToInt64(a.Bits, 2);
                    return new BitVector(checked(aData << shift), width);
                }
            }
            catch (OverflowException)
            {
                throw;
            }
        }

        public static bool operator ==(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "== operator : Bit vectors must have equal widths");
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);
            return a.Equals(b);
        }

        public static bool operator !=(BitVector a, BitVector b)
        {
            Print.ConditionalExitMessage(a.Bits.Length == b.Bits.Length, "!= operator : Bit vectors must have equal widths");
            return !(a == b);
        }

        public static bool operator <(BitVector a, BitVector b)
        {
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return aData < bData;
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return aData < bData;
            }
        }

        public static bool operator <=(BitVector a, BitVector b)
        {
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return aData <= bData;
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return aData <= bData;
            }
        }

        public static bool operator >(BitVector a, BitVector b)
        {
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return aData > bData;
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return aData > bData;
            }
        }

        public static bool operator >=(BitVector a, BitVector b)
        {
            int width = a.Bits.Length;
            if (width <= 32)
            {
                int aData = Convert.ToInt32(a.Bits, 2);
                int bData = Convert.ToInt32(b.Bits, 2);
                return aData >= bData;
            }
            else
            {
                long aData = Convert.ToInt64(a.Bits, 2);
                long bData = Convert.ToInt64(b.Bits, 2);
                return aData >= bData;
            }
        }

        public static BitVector Slice(BitVector a, int high, int low)
        {
            Print.ConditionalExitMessage(
                high > low,
                "Slicing " + a + " is not defined because the slice [" + high + ":" + low + "] is not valid");
            int startIndex = a.Bits.Length - high;
            int length = high - low;
            string bits = a.Bits.Substring(startIndex, length);
            return new BitVector(bits);
        }

        public static BitVector Concatenate(BitVector a, BitVector b)
        {
            string bits = a.Bits + b.Bits;
            return new BitVector(bits);
        }

        public static BitVector ZeroExtend(BitVector a, int width)
        {
            string bits = a.Bits;
            bits = bits.PadLeft(width, '0');
            return new BitVector(bits);
        }

        public static BitVector SignExtend(BitVector a, int width)
        {
            string bits = a.Bits;
            char sign = bits[0];
            bits = bits.PadLeft(width, sign);
            return new BitVector(bits);
        }
    }
}
