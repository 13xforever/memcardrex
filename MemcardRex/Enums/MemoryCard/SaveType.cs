namespace MemcardRex.Enums.MemoryCard
{
	public enum SaveType: byte
	{
		Formatted = 0xA0,
		Initial = 0x51,
		MiddleLink = 0x52,
		EndLink = 0x53,
		DeletedInitial = 0xA1,
		DeletedMiddleLink = 0xA2,
		DeletedEndLink = 0xA3,
	}

	public enum SingleSaveFormat
	{
		Default = 0,
		ActionReplay = 1,
		Mcs = 2,
		Raw = 3,
	}
}