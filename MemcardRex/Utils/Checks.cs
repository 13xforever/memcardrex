using System;

namespace MemcardRex.Utils
{
	public static class Checks
	{
		public static void CheckSaveSlotNumber(int slotNumber)
		{
			if (slotNumber < 0 || slotNumber > 15)
				throw new ArgumentOutOfRangeException("slotNumber");
		}
	}
}