using System;
using Microsoft.SPOT;

namespace LEDStripeControl
{
	static class BitConverter
	{
		public static string ToString(byte[] value, int index = 0)
		{
			return ToString(value, index, value.Length - index);
		}

		public static string ToString(byte[] value, int index, int length)
		{
			char[] c = new char[length * 3];
			byte b;

			for (int y = 0, x = 0; y < length; ++y, ++x)
			{
				b = (byte)(value[index + y] >> 4);
				c[x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
				b = (byte)(value[index + y] & 0xF);
				c[++x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
			}
			return new string(c, 0, c.Length - 1).ToLower();
		}
	}
}
