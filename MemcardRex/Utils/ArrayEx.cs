using System;

namespace MemcardRex.Utils
{
	public static class ArrayEx
	{
		public static void Clear(this Array array)
		{
			if (array == null) return;
			Array.Clear(array, 0, array.Length);
		}
	}
}
