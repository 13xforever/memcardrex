using System.Drawing;

namespace MemcardRex.Utils
{
	static internal class Palette
	{
		public static Color GetColorFromRgba5551(byte[,] palette, int slot, int offset)
		{
			var red = (palette[slot, offset] & 0x1F) << 3;
			var green = ((palette[slot, offset] & 0xE0) >> 2) | ((palette[slot, offset + 1] & 0x03) << 6);
			var blue = ((palette[slot, offset + 1] & 0x7C) << 1);
			var alpha = (palette[slot, offset + 1] & 0x80);
			if ((red | green | blue | alpha) == 0)
				return Color.Transparent;
			return Color.FromArgb(red, green, blue);
		}
	}
}