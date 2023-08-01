using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CanProtocol.Utilities
{
    public static class HexDataHelper
    {
        public static byte[] UShortToByte(ushort value)
        {
            byte[] data = new byte[2];
            int k = 0;
            data[k++] = (byte)(value & 0xff);
            data[k++] = (byte)(value >> 8);
            return data;
        }

        public static byte[] ShortToByte(short value)
        {
            byte[] data = new byte[2];
            int k = 0;
            data[k++] = (byte)(value & 0xff);
            data[k++] = (byte)(value >> 8);
            return data;
        }

        public static byte[] IntToByte(int value, bool IsLittleEndian)
        {
            byte[] data = new byte[4];
            int k = 0;
            if (IsLittleEndian)
            {
                data[k++] = (byte)(value & 0xff);
                data[k++] = (byte)(value >> 8);
                data[k++] = (byte)(value >> 16);
                data[k++] = (byte)(value >> 24);

            }
            else//big-Endian
            {
                data[k++] = (byte)(value & 0xff);
                data[k++] = (byte)(value >> 8);
                data[k++] = (byte)(value >> 16);
                data[k++] = (byte)(value >> 24);
            }
            return data;
        }

        public static byte[] UIntToByte(uint value, bool IsLittleEndian)
        {
            byte[] data = new byte[4];
            int k = 0;
            if (IsLittleEndian)
            {
                data[k++] = (byte)(value & 0xff);
                data[k++] = (byte)(value >> 8);
                data[k++] = (byte)(value >> 16);
                data[k++] = (byte)(value >> 24);
            }
            else//big-Endian
            {
                data[k++] = (byte)(value & 0xff);
                data[k++] = (byte)(value >> 8);
                data[k++] = (byte)(value >> 16);
                data[k++] = (byte)(value >> 24);
            }
            return data;
        }

        public static string ByteArrToStrBigend(byte[] bytes, bool add0x = true)
        {
            string data = "";

            foreach (byte d in bytes)
            {
                data += d.ToString("X2");
            }

            return add0x ? ("0x" + data) : data;
        }

        public static string ByteArrToStrSmallend(byte[] bytes, bool add0x = true)
        {
            string data = "";
            int len = bytes.Length;

            for (int i = len - 1; i >= 0; i--)
            {
                data += bytes[i].ToString("X2");

            }
            return add0x ? ("0x" + data) : data;
        }
        public static byte[] HexStringToByte(string str)
        {
            var arr = str.Split(' ');
            byte[] result = new byte[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                int n = Convert.ToInt32(arr[i], 16);
                result[i] = (byte)n;
            }
            return result;
        }

        public static byte[] HexStringToByte2(string str, bool IsLittleEndian = true)
        {
            List<byte> byteList = new List<byte>();
            str = str.Replace("0x", "").Replace("0X", "");

            uint value;
            if (!uint.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                return byteList.ToArray();
            }

            byteList = new List<byte>(UIntToByte(value, IsLittleEndian));
            return byteList.ToArray();
        }
    }
}
