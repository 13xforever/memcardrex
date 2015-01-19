namespace MemcardRex.Enums
{
	public enum MemoryCardType : byte
	{
		None = 0,
		Raw = 1,
		Gme = 2,  //DexDrive GME Memory Card
		Vgs = 3,
		Vmp = 4,  //PSP virtual Memory Card
	}

	public enum MemoryCardSaveRegion : ushort
	{
		US = 0x4142, // BA
		EU = 0x4542, // BE
		JP = 0x4942, // BI
	}

	public enum MemoryCardSaveType: byte
	{
		Formatted = 0xA0,
		Initial = 0x51,
		MiddleLink = 0x52,
		EndLink = 0x53,
		DeletedInitial = 0xA1,
		DeletedMiddleLink = 0xA2,
		DeletedEndLink = 0xA3,
	}
}