//PS1 Memory Card class
//Shendo 2009-2013

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using MemcardRex.Enums.MemoryCard;
using MemcardRex.Utils;
using Type = MemcardRex.Enums.MemoryCard.Type;

namespace MemcardRex
{
	public class Ps1Card
	{
		private const int MemoryCardSize = 16*8192;
		private const int GmeHeaderSize = 3904;
		private static readonly Encoding AnsiEncoding = Encoding.Default;
		private static readonly Encoding ShiftJisEncodig = Encoding.GetEncoding(932);

		private byte[] rawData = new byte[MemoryCardSize];

		public string CardLocation; //path + filename
		public string CardName;
		public Type CardType;
		public bool ChangedFlag; //Flag used to determine if the card has been edited since the last saving

		public readonly byte[] GmeHeader = new byte[GmeHeaderSize]; //Header data for the GME Memory Card
		public readonly byte[,] HeaderData = new byte[15, 128]; //15 slots (128 bytes each)
		public readonly Bitmap[,] IconData = new Bitmap[15, 3];//15 slots, 3 icons per slot, (16*16px icons)
		public readonly int[] IconFrames = new int[15];
		public readonly Color[,] IconPalette = new Color[15, 16]; //15 slots x 16 colors
		public readonly string[] SaveComments = new string[15]; //Save comments (supported by .gme files only), 255 characters allowed
		public readonly byte[,] SaveData = new byte[15, 8192];//15 slots (8192 bytes each)
		public readonly string[] SaveIdentifier = new string[15];
		public readonly string[,] SaveName = new string[15, 2]; //Name of the save in ASCII(0) and UTF-16(1) encoding
		public readonly string[] SaveProductCode = new string[15];
		public readonly SaveRegion[] SaveRegion = new SaveRegion[15];
		public readonly int[] SaveSize = new int[15]; //Size of the save in KBs
		public readonly SaveType[] SaveType = new SaveType[15];

		private void ParseRawCardInternal()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				for (var currentByte = 0; currentByte < 128; currentByte++)
					HeaderData[slotNumber, currentByte] = rawData[128 + (slotNumber*128) + currentByte];
				for (var currentByte = 0; currentByte < 8192; currentByte++)
					SaveData[slotNumber, currentByte] = rawData[8192 + (slotNumber*8192) + currentByte];
			}
		}

		private void CreateRawCardInternal()
		{
			rawData.Clear();
			// signature
			rawData[0] = 0x4D;   // M
			rawData[1] = 0x43;   // C
			rawData[127] = 0x0E; // pre-calculated checksum
			rawData[8064] = 0x4D;
			rawData[8065] = 0x43;
			rawData[8191] = 0x0E;
			// data
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				for (var currentByte = 0; currentByte < 128; currentByte++)
					rawData[128 + (slotNumber*128) + currentByte] = HeaderData[slotNumber, currentByte];
				for (var currentByte = 0; currentByte < 8192; currentByte++)
					rawData[8192 + (slotNumber*8192) + currentByte] = SaveData[slotNumber, currentByte];
			}
			// create authentic data (just for completeness)
			for (var i = 0; i < 20; i++)
			{
				// reserved slot typed
				rawData[2048 + (i*128)] = 0xFF;
				rawData[2048 + (i*128) + 1] = 0xFF;
				rawData[2048 + (i*128) + 2] = 0xFF;
				rawData[2048 + (i*128) + 3] = 0xFF;
				// next slot pointer doesn't point to anything
				rawData[2048 + (i*128) + 8] = 0xFF;
				rawData[2048 + (i*128) + 9] = 0xFF;
			}
		}

		private void CreateGmeHeader()
		{
			GmeHeader.Clear();
			var signature = Encoding.ASCII.GetBytes("123-456-STD");
			Buffer.BlockCopy(signature, 0, GmeHeader, 0, signature.Length);
			GmeHeader[18] = 0x01;
			GmeHeader[20] = 0x01;
			GmeHeader[21] = 0x4D; //M
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				GmeHeader[22 + slotNumber] = HeaderData[slotNumber, 0];
				GmeHeader[38 + slotNumber] = HeaderData[slotNumber, 8];
				//Convert string from UTF-16 to currently used codepage
				var tempByteArray = Encoding.Convert(Encoding.Unicode, AnsiEncoding, Encoding.Unicode.GetBytes(SaveComments[slotNumber]));
				//Inject comments to GME header
				for (var byteCount = 0; byteCount < tempByteArray.Length; byteCount++)
					GmeHeader[byteCount + 64 + (256*slotNumber)] = tempByteArray[byteCount];
			}
		}

		private byte[] GetVgsHeader()
		{
			var vgsHeader = new byte[64];
			var signature = Encoding.ASCII.GetBytes("VgsM");
			Buffer.BlockCopy(signature, 0, vgsHeader, 0, signature.Length);
			vgsHeader[4] = 0x1;
			vgsHeader[8] = 0x1;
			vgsHeader[12] = 0x1;
			vgsHeader[17] = 0x2;
			return vgsHeader;
		}

		private void LoadSlotTypes()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				SaveType[slotNumber] = (SaveType)HeaderData[slotNumber, 0];
		}

		private static string GetString(byte[,] header, int slotNumber, int offset, int dataLength, Encoding encoding, byte[] buffer)
		{
			for (var idx = 0; idx < dataLength; idx++)
				buffer[idx] = header[slotNumber, offset + idx];
			return encoding.GetString(buffer, 0, dataLength);
		}

		private void LoadStringData() //Load Save name, Product code and Identifier from the header data
		{
			SaveProductCode.Clear();
			SaveIdentifier.Clear();
			SaveName.Clear();
			var buffer = new byte[64];
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				SaveProductCode[slotNumber] = GetString(HeaderData, slotNumber, 12, 10, AnsiEncoding, buffer);
				SaveIdentifier[slotNumber] = GetString(HeaderData, slotNumber, 22, 8, AnsiEncoding, buffer);
				SaveName[slotNumber, 1] = GetString(SaveData, slotNumber, 4, 64, ShiftJisEncodig, buffer);
				SaveName[slotNumber, 0] = CharConverter.SjisToAscii(buffer); //Convert save name from Shift-JIS to UTF-16 as ASCII equivalent
				if (string.IsNullOrEmpty(SaveName[slotNumber, 0]))
					SaveName[slotNumber, 0] = AnsiEncoding.GetString(buffer, 0, 32);
			}
		}

		private void CalculateSaveSize()
		{
			SaveSize.Clear();
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				SaveSize[slotNumber] = (HeaderData[slotNumber, 4] | (HeaderData[slotNumber, 5] << 8) | (HeaderData[slotNumber, 6] << 16))/1024;
		}

		private void ParseEverything()
		{
			CalculateChecksums();
			LoadStringData();
			LoadSlotTypes();
			LoadRegion();
			CalculateSaveSize();
			LoadPalette();
			LoadIcons();
			LoadIconFrames();
		}

		public void ToggleDeleteSave(int slotNumber)
		{
			var saveSlots = FindSaveLinks(slotNumber);
			foreach (int slot in saveSlots)
			{
				switch (SaveType[slot])
				{
					case Enums.MemoryCard.SaveType.Initial:
						SaveType[slot] = Enums.MemoryCard.SaveType.DeletedInitial;
						break;
					case Enums.MemoryCard.SaveType.MiddleLink:
						SaveType[slot] = Enums.MemoryCard.SaveType.DeletedMiddleLink;
						break;
					case Enums.MemoryCard.SaveType.EndLink:
						SaveType[slot] = Enums.MemoryCard.SaveType.DeletedEndLink;
						break;
					case Enums.MemoryCard.SaveType.DeletedInitial:
						SaveType[slot] = Enums.MemoryCard.SaveType.Initial;
						break;
					case Enums.MemoryCard.SaveType.DeletedMiddleLink:
						SaveType[slot] = Enums.MemoryCard.SaveType.MiddleLink;
						break;
					case Enums.MemoryCard.SaveType.DeletedEndLink:
						SaveType[slot] = Enums.MemoryCard.SaveType.EndLink;
						break;
				}
				HeaderData[slot, 0] = (byte)SaveType[slot];
			}
			ChangedFlag = true;
			ParseEverything();
		}

		public void FormatSave(int slotNumber)
		{
			Checks.CheckSaveSlotNumber(slotNumber);

			var saveSlots = FindSaveLinks((byte)slotNumber);
			foreach (var slot in saveSlots)
				FormatSlot(slot);
			ChangedFlag = true;
			ParseEverything();
		}

		public List<int> FindSaveLinks(int initialSlotNumber)
		{
			Checks.CheckSaveSlotNumber(initialSlotNumber);

			var result = new List<int>();
			var slotIdx = initialSlotNumber;
			for (var i = 0; i < 15; i++)
			{
				if (slotIdx > 15) break;

				result.Add(slotIdx);
				//Check if current slot is corrupted
				if (!Enum.IsDefined(typeof(SaveType), SaveType[slotIdx])) break;

				slotIdx = HeaderData[slotIdx, 8];
				if (slotIdx == 0xFF) break;
			}
			return result;
		}

		private List<int> FindContinuousFreeSlots(int slotNumber, int slotCount)
		{
			Checks.CheckSaveSlotNumber(slotNumber);

			var result = new List<int>();
			for (var i = slotNumber; i < 15 && i < (slotNumber + slotCount); i++)
			{
				if (SaveType[i] == Enums.MemoryCard.SaveType.Formatted)
					result.Add(i);
				else
					break;
			}
			return result;
		}

		public byte[] GetSaveBytes(int slotNumber)
		{
			var saveSlots = FindSaveLinks(slotNumber);
			var saveBytes = new byte[128 + saveSlots.Count*8192];
			for (var i = 0; i < 128; i++)
				saveBytes[i] = HeaderData[saveSlots[0], i];
			for (var i = 0; i < saveSlots.Count; i++)
				for (var j = 0; j < 8192; j++)
					saveBytes[128 + i*8192 + j] = SaveData[saveSlots[i], j];
			return saveBytes;
		}

		public bool SetSaveBytes(int slotNumber, byte[] saveBytes, out int requiredSlots)
		{
			requiredSlots = (saveBytes.Length + 8192 - 128) / 8192;
			var freeSlots = FindContinuousFreeSlots(slotNumber, requiredSlots);
			var numberOfBytes = requiredSlots * 8192;
			if (freeSlots.Count < requiredSlots) return false;

			for (var i = 0; i < 128; i++)
				HeaderData[freeSlots[0], i] = saveBytes[i];
			HeaderData[freeSlots[0], 4] = (byte)(numberOfBytes & 0xFF);
			HeaderData[freeSlots[0], 5] = (byte)((numberOfBytes >> 8) & 0xFF);
			HeaderData[freeSlots[0], 6] = (byte)((numberOfBytes >> 16) & 0xFF);
			for (var i = 0; i < requiredSlots; i++)
				for (var j = 0; j < 8192; j++)
					SaveData[freeSlots[i], j] = saveBytes[128 + (i*8192) + j];

			// set links
			HeaderData[freeSlots[0], 0] = (byte)Enums.MemoryCard.SaveType.Initial;
			var lastFreeSlotIdx = freeSlots.Count - 1;
			for (var i = 0; i < lastFreeSlotIdx; i++)
			{
				HeaderData[freeSlots[i], 0] = (byte)Enums.MemoryCard.SaveType.MiddleLink;
				HeaderData[freeSlots[i], 8] = (byte)freeSlots[i + 1];
				HeaderData[freeSlots[i], 9] = 0x00;
			}
			HeaderData[freeSlots[lastFreeSlotIdx], 0] = (byte)Enums.MemoryCard.SaveType.EndLink;
			HeaderData[freeSlots[lastFreeSlotIdx], 8] = 0xFF;
			HeaderData[freeSlots[lastFreeSlotIdx], 9] = 0xFF;

			ChangedFlag = true;
			ParseEverything();
			return true;
		}

		public void SetHeaderData(int slotNumber, string productCode, string identifier, SaveRegion region)
		{
			Checks.CheckSaveSlotNumber(slotNumber);

			//todo: checks (10 + 8) and append with filler?
			var headerString = productCode + identifier;
			var headerStringBytes = Encoding.Convert(Encoding.Unicode, AnsiEncoding, Encoding.Unicode.GetBytes(headerString));
			for (var i = 0; i < 20; i++)
				HeaderData[slotNumber, 10 + i] = 0x00;
			for (var i = 0; i < headerString.Length; i++)
				HeaderData[slotNumber, 12 + i] = headerStringBytes[i];
			HeaderData[slotNumber, 10] = (byte)((ushort)region & 0xFF);
			HeaderData[slotNumber, 11] = (byte)(((ushort)region >> 8) & 0xFF);
			ChangedFlag = true;
			ParseEverything();
		}

		private void LoadRegion()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				SaveRegion[slotNumber] = (SaveRegion)((HeaderData[slotNumber, 11] << 8) | HeaderData[slotNumber, 10]);
		}

		private void LoadPalette()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				for (var colorIdx = 0; colorIdx < 16; colorIdx++)
					IconPalette[slotNumber, colorIdx] = Palette.GetColorFromRgba5551(SaveData, slotNumber, 96 + colorIdx*2);
		}

		private void LoadIcons()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				for (var iconNumber = 0; iconNumber < 3; iconNumber++) //Each save has 3 icons (some are data but those will not be shown)
				{
					IconData[slotNumber, iconNumber] = new Bitmap(16, 16);
					var idx = 128 + (128*iconNumber);
					for (var y = 0; y < 16; y++)
						for (var x = 0; x < 16; x += 2)
						{
							IconData[slotNumber, iconNumber].SetPixel(x, y, IconPalette[slotNumber, SaveData[slotNumber, idx] & 0x0F]);
							IconData[slotNumber, iconNumber].SetPixel(x + 1, y, IconPalette[slotNumber, SaveData[slotNumber, idx] >> 4]);
							idx++;
						}
				}
		}

		public byte[] GetIconBytes(int slotNumber)
		{
			var iconBytes = new byte[32 + 3*128];
			for (var i = 0; i < iconBytes.Length; i++)
				iconBytes[i] = SaveData[slotNumber, 96 + i];
			return iconBytes;
		}

		public void SetIconBytes(int slotNumber, byte[] iconBytes)
		{
			for (var i = 0; i < 416; i++) // 32 + 3*128
				SaveData[slotNumber, 96 + i] = iconBytes[i];
			ChangedFlag = true;
			ParseEverything();
		}

		private void LoadIconFrames()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				switch (SaveData[slotNumber, 2])
				{
					case 0x11: //1 frame
						IconFrames[slotNumber] = 1;
						break;

					case 0x12: //2 frames
						IconFrames[slotNumber] = 2;
						break;

					case 0x13: //3 frames
						IconFrames[slotNumber] = 3;
						break;

					//No frames (save data is probably clean)
				}
		}

		private void LoadGmeComments()
		{
			var buffer = new byte[256];
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				Buffer.BlockCopy(GmeHeader, 64 + (256 * slotNumber), buffer, 0, 256);
				SaveComments[slotNumber] = AnsiEncoding.GetString(buffer);
			}
		}

		private void CalculateChecksums()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				byte checkSum = 0;
				for (var i = 0; i < 127; i++)
					checkSum ^= HeaderData[slotNumber, i];
				HeaderData[slotNumber, 127] = checkSum;
			}
		}

		private void FormatSlot(int slotNumber)
		{
			Checks.CheckSaveSlotNumber(slotNumber);

			SaveComments[slotNumber] = new string('\0', 256);
			for (var byteCount = 0; byteCount < 8192; byteCount++)
				SaveData[slotNumber, byteCount] = 0x00;
			for (var byteCount = 0; byteCount < 128; byteCount++)
				HeaderData[slotNumber, byteCount] = 0x00;
			HeaderData[slotNumber, 0] = 0xA0;
			HeaderData[slotNumber, 8] = 0xFF;
			HeaderData[slotNumber, 9] = 0xFF;
		}

		public void Format()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				FormatSlot(slotNumber);
			ChangedFlag = true;
			ParseEverything();
		}

		public bool ExportSingleSave(string fileName, int slotNumber, SingleSaveFormat singleSaveType)
		{
			try
			{
				using (var stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
				using (var writer = new BinaryWriter(stream))
				{
					var outputData = GetSaveBytes(slotNumber);
					switch (singleSaveType)
					{
						case SingleSaveFormat.Mcs:
							writer.Write(outputData);
							break;

						case SingleSaveFormat.Raw:
							writer.Write(outputData, 128, outputData.Length - 128);
							break;

						default:
							var arHeader = new byte[54];
							for (var byteCount = 0; byteCount < 22; byteCount++)
								arHeader[byteCount] = HeaderData[slotNumber, 10 + byteCount];
							var arName = AnsiEncoding.GetBytes(SaveName[slotNumber, 0]);
							Buffer.BlockCopy(arName, 0, arHeader, 21, arName.Length);
							writer.Write(arHeader);
							writer.Write(outputData, 128, outputData.Length - 128);
							writer.Flush();
							break;
					}
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool ImportSingleSave(string fileName, int slotNumber, out int requiredSlots)
		{
			requiredSlots = 0;
			try
			{
				byte[] inputData;
				using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var binReader = new BinaryReader(stream))
					inputData = binReader.ReadBytes(123008);
				var signature = inputData.Length > 1 ? Encoding.ASCII.GetString(inputData, 0, 2).Trim('\0') : null;
				byte[] cardData;
				switch (signature)
				{
					case "Q": //MCS single save
						cardData = inputData;
						break;

					case "SC": //RAW single save
						cardData = new byte[inputData.Length + 128];
						var singleSaveHeader = AnsiEncoding.GetBytes(Path.GetFileName(fileName));
						Buffer.BlockCopy(singleSaveHeader, 0, cardData, 10, Math.Min(20, singleSaveHeader.Length));
						Buffer.BlockCopy(inputData, 0, cardData, 128, inputData.Length);
						break;

					case "V": //PSV single save (PS3 virtual save)
						if (inputData[60] != 1) return false; // not a PS1 type save

						cardData = new byte[inputData.Length - 4];
						Buffer.BlockCopy(inputData, 100, cardData, 10, 20);
						Buffer.BlockCopy(inputData, 132, cardData, 128, inputData.Length-132);
						break;

					default: //Action Replay single save
						if (!(inputData[0x36] == 0x53 && inputData[0x37] == 0x43)) return false; // not an AR save (check for SC magic)

						cardData = new byte[inputData.Length + 74];
						Buffer.BlockCopy(inputData, 0, cardData, 10, 20);
						Buffer.BlockCopy(inputData, 54, cardData, 128, inputData.Length-54);
						break;
				}
				cardData[0] = 0x51; //Q
				return SetSaveBytes(slotNumber, cardData, out requiredSlots);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool SaveTo(string filePath, Type type)
		{
			try
			{
				using (var file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
				{
					SaveTo(file, type);
					CardLocation = filePath;
					CardName = Path.GetFileNameWithoutExtension(filePath);
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		public void SaveTo(Stream outputStream, Type type)
		{
			using (var binWriter = new BinaryWriter(outputStream))
			{
				CreateRawCardInternal();
				switch (type)
				{
					case Type.Gme:
						CreateGmeHeader();
						binWriter.Write(GmeHeader);
						binWriter.Write(rawData);
						break;

					case Type.Vgs:
						binWriter.Write(GetVgsHeader());
						binWriter.Write(rawData);
						break;

					default:
						binWriter.Write(rawData);
						break;
				}
				ChangedFlag = false;
				binWriter.Flush();
			}
		}

		public byte[] GetRawMemoryCard()
		{
			CreateRawCardInternal();
			return rawData;
		}

		public void LoadMemoryCard(byte[] memCardData)
		{
			rawData = memCardData;
			ParseRawCardInternal();
			CardName = "Untitled";
			CalculateChecksums();
			LoadStringData();
			LoadGmeComments();
			LoadSlotTypes();
			LoadRegion();
			CalculateSaveSize();
			LoadPalette();
			LoadIcons();
			LoadIconFrames();

			//Since the stream is of the unknown origin Memory Card is treated as edited
			ChangedFlag = true;
		}

		//Open Memory Card from the given filename (return error message if operation is not sucessfull)
		public string openMemoryCard(string fileName)
		{
			//Check if the Memory Card should be opened or created
			if (fileName != null)
			{
				var tempData = new byte[134976];
				string tempString = null;
				var startOffset = 0;
				BinaryReader binReader = null;

				//Check if the file is allowed to be opened
				try
				{
					binReader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
				}
				catch (Exception errorException)
				{
					//Return the error description
					return errorException.Message;
				}

				//Put data into temp array
				binReader.BaseStream.Read(tempData, 0, GmeHeaderSize + MemoryCardSize);

				//File is sucesfully read, close the stream
				binReader.Close();

				//Store the location of the Memory Card
				CardLocation = fileName;

				//Store the filename of the Memory Card
				CardName = Path.GetFileNameWithoutExtension(fileName);

				//Check the format of the card and if it's supported load it (filter illegal characters from types)
				tempString = Encoding.ASCII.GetString(tempData, 0, 11).Trim((char)0x00, (char)0x01, (char)0x3F);
				switch (tempString)
				{
					case "MC":
						startOffset = 0;
						CardType = Type.Raw;
						break;

					case "123-456-STD":
						startOffset = 3904;
						CardType = Type.Gme;
						for (var i = 0; i < GmeHeaderSize; i++)
							GmeHeader[i] = tempData[i];
						break;

					case "VgsM":
						startOffset = 64;
						CardType = Type.Vgs;
						break;

					case "PMV": 
						startOffset = 128;
						CardType = Type.Vmp;
						break;

					default: //File type is not supported
						return "'" + CardName + "' is not a supported Memory Card format.";
				}

				//Copy data to rawData array with offset from input data
				Buffer.BlockCopy(tempData, startOffset, rawData, 0, MemoryCardSize);

				//Load Memory Card data from raw card
				ParseRawCardInternal();
			}
			// Memory Card should be created
			else
			{
				CardName = "Untitled";
				Format();

				//Set changedFlag to false since this is created card
				ChangedFlag = false;
			}

			//Calculate XOR checksum (in case if any of the saveHeaders have corrputed XOR)
			CalculateChecksums();

			//Convert various Memory Card data to strings
			LoadStringData();

			//Load GME comments (if card is any other type comments will be null)
			LoadGmeComments();

			//Load slot descriptions (types)
			LoadSlotTypes();

			//Load region data
			LoadRegion();

			//Load size data
			CalculateSaveSize();

			//Load icon palette data as Color values
			LoadPalette();

			//Load icon data to bitmaps
			LoadIcons();

			//Load number of frames
			LoadIconFrames();

			//Everything went well, no error messages
			return null;
		}
	}
}