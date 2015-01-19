﻿//PS1 Memory Card class
//Shendo 2009-2013

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using MemcardRex.Enums;
using MemcardRex.Utils;

namespace MemcardRex
{
	public class Ps1Card
	{
		public const int MemoryCardSize = 128*1024;
		public const int GmeHeaderSize = 3904;
		private static readonly Encoding AnsiEncoding = Encoding.Default;
		private static readonly Encoding ShiftJisEncodig = Encoding.GetEncoding(932);

		private byte[] rawData = new byte[MemoryCardSize];

		public string CardLocation; //path + filename
		public string CardName;
		public MemoryCardType CardType;
		public bool ChangedFlag; //Flag used to determine if the card has been edited since the last saving
		public byte[] GmeHeader = new byte[GmeHeaderSize]; //Header data for the GME Memory Card
		public byte[,] HeaderData = new byte[15, 128]; //15 slots (128 bytes each)
		public Bitmap[,] IconData = new Bitmap[15, 3];//15 slots, 3 icons per slot, (16*16px icons)
		public int[] IconFrames = new int[15];
		public Color[,] IconPalette = new Color[15, 16]; //15 slots x 16 colors
		public string[] SaveComments = new string[15]; //Save comments (supported by .gme files only), 255 characters allowed
		public byte[,] SaveData = new byte[15, 8192];//15 slots (8192 bytes each)
		public string[] SaveIdentifier = new string[15];
		public string[,] SaveName = new string[15, 2]; //Name of the save in ASCII(0) and UTF-16(1) encoding
		public string[] SaveProductCode = new string[15];
		public MemoryCardSaveRegion[] SaveRegion = new MemoryCardSaveRegion[15];
		public int[] SaveSize = new int[15]; //Size of the save in KBs
		public MemoryCardSaveType[] SaveType = new MemoryCardSaveType[15];

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
			GmeHeader[18] = 0x01; //todo: magic numbers or checksum?
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
				SaveType[slotNumber] = (MemoryCardSaveType)HeaderData[slotNumber, 0];
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

		public void ToggleDeleteSave(int slotNumber)
		{
			var saveSlots = FindSaveLinks(slotNumber);
			foreach (int slot in saveSlots)
			{
				switch (SaveType[slot])
				{
					case MemoryCardSaveType.Initial:
						SaveType[slot] = MemoryCardSaveType.DeletedInitial;
						break;
					case MemoryCardSaveType.MiddleLink:
						SaveType[slot] = MemoryCardSaveType.DeletedMiddleLink;
						break;
					case MemoryCardSaveType.EndLink:
						SaveType[slot] = MemoryCardSaveType.DeletedEndLink;
						break;
					case MemoryCardSaveType.DeletedInitial:
						SaveType[slot] = MemoryCardSaveType.Initial;
						break;
					case MemoryCardSaveType.DeletedMiddleLink:
						SaveType[slot] = MemoryCardSaveType.MiddleLink;
						break;
					case MemoryCardSaveType.DeletedEndLink:
						SaveType[slot] = MemoryCardSaveType.EndLink;
						break;
				}
				HeaderData[slot, 0] = (byte)SaveType[slot];
			}
			CalculateChecksums();
			ChangedFlag = true;
		}

		//Format save
		public void FormatSave(int slotNumber)
		{
			//Get all linked saves
			var saveSlots = FindSaveLinks(slotNumber);

			//Cycle through each slot
			for (var i = 0; i < saveSlots.Length; i++)
			{
				formatSlot(saveSlots[i]);
			}

			//Reload data
			CalculateChecksums();
			LoadStringData();
			LoadSlotTypes();
			LoadRegion();
			CalculateSaveSize();
			LoadPalette();
			LoadIcons();
			LoadIconFrames();

			//Set changedFlag to edited
			ChangedFlag = true;
		}

		//Find and return all save links
		public int[] FindSaveLinks(int initialSlotNumber)
		{
			var tempSlotList = new List<int>();
			var currentSlot = initialSlotNumber;

			//Maximum number of cycles is 15
			for (var i = 0; i < 15; i++)
			{
				//Add current slot to the list
				tempSlotList.Add(currentSlot);

				//Check if next slot pointer overflows
				if (currentSlot > 15) break;

				//Check if current slot is corrupted
				if (!Enum.IsDefined(typeof(MemoryCardSaveType), SaveType[currentSlot])) break;

				//Check if pointer points to the next save
				if (HeaderData[currentSlot, 8] == 0xFF) break;
				currentSlot = HeaderData[currentSlot, 8];
			}

			//Return int array
			return tempSlotList.ToArray();
		}

		//Find and return continuous free slots
		private int[] FindFreeSlots(int slotNumber, int slotCount)
		{
			var tempSlotList = new List<int>();

			//Cycle through available slots
			for (var i = slotNumber; i < (slotNumber + slotCount); i++)
			{
				if (SaveType[i] == 0) tempSlotList.Add(i);
				else break;

				//Exit if next save would be over the limit of 15
				if (slotNumber + slotCount > 15) break;
			}

			//Return int array
			return tempSlotList.ToArray();
		}

		//Return all bytes of the specified save
		public byte[] GetSaveBytes(int slotNumber)
		{
			//Get all linked saves
			var saveSlots = FindSaveLinks(slotNumber);

			//Calculate the number of bytes needed to store the save
			var saveBytes = new byte[8320 + ((saveSlots.Length - 1)*8192)];

			//Copy save header
			for (var i = 0; i < 128; i++)
				saveBytes[i] = HeaderData[saveSlots[0], i];

			//Copy save data
			for (var sNumber = 0; sNumber < saveSlots.Length; sNumber++)
			{
				for (var i = 0; i < 8192; i++)
					saveBytes[128 + (sNumber*8192) + i] = SaveData[saveSlots[sNumber], i];
			}

			//Return save bytes
			return saveBytes;
		}

		//Input given bytes back to the Memory Card
		public bool SetSaveBytes(int slotNumber, byte[] saveBytes, out int reqSlots)
		{
			//Number of slots to set
			var slotCount = (saveBytes.Length - 128)/8192;
			var freeSlots = FindFreeSlots(slotNumber, slotCount);
			var numberOfBytes = slotCount*8192;
			;
			reqSlots = slotCount;

			//Check if there are enough free slots for the operation
			if (freeSlots.Length < slotCount) return false;

			//Place header data
			for (var i = 0; i < 128; i++)
				HeaderData[freeSlots[0], i] = saveBytes[i];

			//Place save size in the header
			HeaderData[freeSlots[0], 4] = (byte)(numberOfBytes & 0xFF);
			HeaderData[freeSlots[0], 5] = (byte)((numberOfBytes & 0xFF00) >> 8);
			HeaderData[freeSlots[0], 6] = (byte)((numberOfBytes & 0xFF0000) >> 16);

			//Place save data(cycle through each save)
			for (var i = 0; i < slotCount; i++)
			{
				//Set all bytes
				for (var byteCount = 0; byteCount < 8192; byteCount++)
				{
					SaveData[freeSlots[i], byteCount] = saveBytes[128 + (i*8192) + byteCount];
				}
			}

			//Recreate header data
			//Set pointer to all slots except the last
			for (var i = 0; i < (freeSlots.Length - 1); i++)
			{
				HeaderData[freeSlots[i], 0] = 0x52;
				HeaderData[freeSlots[i], 8] = (byte)freeSlots[i + 1];
				HeaderData[freeSlots[i], 9] = 0x00;
			}

			//Add final slot pointer to the last slot in the link
			HeaderData[freeSlots[freeSlots.Length - 1], 0] = 0x53;
			HeaderData[freeSlots[freeSlots.Length - 1], 8] = 0xFF;
			HeaderData[freeSlots[freeSlots.Length - 1], 9] = 0xFF;

			//Add initial saveType to the first slot
			HeaderData[freeSlots[0], 0] = 0x51;

			//Reload data
			CalculateChecksums();
			LoadStringData();
			LoadSlotTypes();
			LoadRegion();
			CalculateSaveSize();
			LoadPalette();
			LoadIcons();
			LoadIconFrames();

			//Set changedFlag to edited
			ChangedFlag = true;

			return true;
		}

		//Set Product code, Identifier and Region in the header of the selected save
		public void SetHeaderData(int slotNumber, string sProdCode, string sIdentifier, ushort sRegion)
		{
			//Temp array used for manipulation
			byte[] tempByteArray;

			//Merge Product code and Identifier
			var headerString = sProdCode + sIdentifier;

			//Convert string from UTF-16 to currently used codepage
			tempByteArray = Encoding.Convert(Encoding.Unicode, Encoding.Default, Encoding.Unicode.GetBytes(headerString));

			//Clear existing data from header
			for (var byteCount = 0; byteCount < 20; byteCount++)
				HeaderData[slotNumber, byteCount + 10] = 0x00;

			//Inject new data to header
			for (var byteCount = 0; byteCount < headerString.Length; byteCount++)
				HeaderData[slotNumber, byteCount + 12] = tempByteArray[byteCount];

			//Add region to header
			HeaderData[slotNumber, 10] = (byte)(sRegion & 0xFF);
			HeaderData[slotNumber, 11] = (byte)(sRegion >> 8);

			//Reload data
			LoadStringData();
			LoadRegion();

			//Calculate XOR
			CalculateChecksums();

			//Set changedFlag to edited
			ChangedFlag = true;
		}

		//Load region of the saves
		private void LoadRegion()
		{
			//Clear existing data
			SaveRegion = new MemoryCardSaveRegion[15];

			//Cycle trough each slot
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				//Store save region
				SaveRegion[slotNumber] = (MemoryCardSaveRegion)((HeaderData[slotNumber, 11] << 8) | HeaderData[slotNumber, 10]);
			}
		}

		//Load palette
		private void LoadPalette()
		{
			var redChannel = 0;
			var greenChannel = 0;
			var blueChannel = 0;
			var colorCounter = 0;
			var blackFlag = 0;

			//Clear existing data
			IconPalette = new Color[15, 16];

			//Cycle through each slot on the Memory Card
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				//Reset color counter
				colorCounter = 0;

				//Fetch two bytes at a time
				for (var byteCount = 0; byteCount < 32; byteCount += 2)
				{
					redChannel = (SaveData[slotNumber, byteCount + 96] & 0x1F) << 3;
					greenChannel = ((SaveData[slotNumber, byteCount + 97] & 0x3) << 6) | ((SaveData[slotNumber, byteCount + 96] & 0xE0) >> 2);
					blueChannel = ((SaveData[slotNumber, byteCount + 97] & 0x7C) << 1);
					blackFlag = (SaveData[slotNumber, byteCount + 97] & 0x80);

					//Get the color value
					if ((redChannel | greenChannel | blueChannel | blackFlag) == 0) IconPalette[slotNumber, colorCounter] = Color.Transparent;
					else IconPalette[slotNumber, colorCounter] = Color.FromArgb(redChannel, greenChannel, blueChannel);
					colorCounter++;
				}
			}
		}

		//Load the icons
		private void LoadIcons()
		{
			var byteCount = 0;

			//Clear existing data
			IconData = new Bitmap[15, 3];

			//Cycle through each slot
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				//Each save has 3 icons (some are data but those will not be shown)
				for (var iconNumber = 0; iconNumber < 3; iconNumber++)
				{
					IconData[slotNumber, iconNumber] = new Bitmap(16, 16);
					byteCount = 128 + (128*iconNumber);

					for (var y = 0; y < 16; y++)
					{
						for (var x = 0; x < 16; x += 2)
						{
							IconData[slotNumber, iconNumber].SetPixel(x, y, IconPalette[slotNumber, SaveData[slotNumber, byteCount] & 0xF]);
							IconData[slotNumber, iconNumber].SetPixel(x + 1, y, IconPalette[slotNumber, SaveData[slotNumber, byteCount] >> 4]);
							byteCount++;
						}
					}
				}
			}
		}

		//Get icon data as bytes
		public byte[] GetIconBytes(int slotNumber)
		{
			var iconBytes = new byte[416];

			//Copy bytes from the given slot
			for (var i = 0; i < 416; i++)
				iconBytes[i] = SaveData[slotNumber, i + 96];

			return iconBytes;
		}

		//Set icon data to saveData
		public void SetIconBytes(int slotNumber, byte[] iconBytes)
		{
			//Set bytes from the given slot
			for (var i = 0; i < 416; i++)
				SaveData[slotNumber, i + 96] = iconBytes[i];

			//Reload data
			LoadPalette();
			LoadIcons();

			//Set changedFlag to edited
			ChangedFlag = true;
		}

		//Load icon frames
		private void LoadIconFrames()
		{
			//Clear existing data
			IconFrames = new int[15];

			//Cycle through each slot
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
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

					default: //No frames (save data is probably clean)
						break;
				}
			}
		}

		private void LoadGmeComments()
		{
			//Clear existing data
			SaveComments = new string[15];

			//Load comments from gmeHeader to saveComments
			byte[] tempByteArray;
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				tempByteArray = new byte[256];

				for (var byteCount = 0; byteCount < 256; byteCount++)
					tempByteArray[byteCount] = GmeHeader[byteCount + 64 + (256*slotNumber)];

				//Set save comment for each slot
				SaveComments[slotNumber] = Encoding.Default.GetString(tempByteArray);
			}
		}

		private void CalculateChecksums()
		{
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
			{
				byte checkSum = 0;
				for (var byteCount = 0; byteCount < 126; byteCount++)
					checkSum ^= HeaderData[slotNumber, byteCount];
				HeaderData[slotNumber, 127] = checkSum;
			}
		}

		//Format a specified slot (Data MUST be reloaded after the use of this function)
		private void formatSlot(int slotNumber)
		{
			//Clear headerData
			for (var byteCount = 0; byteCount < 128; byteCount++)
				HeaderData[slotNumber, byteCount] = 0x00;

			//Clear saveData
			for (var byteCount = 0; byteCount < 8192; byteCount++)
				SaveData[slotNumber, byteCount] = 0x00;

			//Clear GME comment for selected slot
			SaveComments[slotNumber] = new string('\0', 256);

			//Place default values in headerData
			HeaderData[slotNumber, 0] = 0xA0;
			HeaderData[slotNumber, 8] = 0xFF;
			HeaderData[slotNumber, 9] = 0xFF;
		}

		public void Format()
		{
			//Format each slot in Memory Card
			for (var slotNumber = 0; slotNumber < 15; slotNumber++)
				formatSlot(slotNumber);

			//Reload data
			CalculateChecksums();
			LoadStringData();
			LoadSlotTypes();
			LoadRegion();
			CalculateSaveSize();
			LoadPalette();
			LoadIcons();
			LoadIconFrames();

			//Set changedFlag to edited
			ChangedFlag = true;
		}

		//Save single save to the given filename
		public bool saveSingleSave(string fileName, int slotNumber, int singleSaveType)
		{
			BinaryWriter binWriter;
			var outputData = GetSaveBytes(slotNumber);

			//Check if the file is allowed to be opened for writing
			try
			{
				binWriter = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
			}
			catch (Exception)
			{
				return false;
			}

			//Check what kind of file to output according to singleSaveType
			switch (singleSaveType)
			{
				default: //Action Replay single save
					var arHeader = new byte[54];
					byte[] arName = null;

					//Copy header data to arHeader
					for (var byteCount = 0; byteCount < 22; byteCount++)
						arHeader[byteCount] = HeaderData[slotNumber, byteCount + 10];

					//Convert save name to bytes
					arName = Encoding.Default.GetBytes(SaveName[slotNumber, 0]);

					//Copy save name to arHeader
					for (var byteCount = 0; byteCount < arName.Length; byteCount++)
						arHeader[byteCount + 21] = arName[byteCount];

					binWriter.Write(arHeader);
					binWriter.Write(outputData, 128, outputData.Length - 128);
					break;

				case 2: //MCS single save
					binWriter.Write(outputData);
					break;

				case 3: //RAW single save
					binWriter.Write(outputData, 128, outputData.Length - 128);
					break;
			}

			//File is sucesfully saved, close the stream
			binWriter.Close();

			return true;
		}

		//Import single save to the Memory Card
		public bool openSingleSave(string fileName, int slotNumber, out int requiredSlots)
		{
			requiredSlots = 0;
			string tempString = null;
			byte[] inputData = null;
			byte[] finalData = null;
			BinaryReader binReader = null;

			//Check if the file is allowed to be opened
			try
			{
				binReader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			}
			catch (Exception)
			{
				return false;
			}

			//Put data into temp array
			inputData = binReader.ReadBytes(123008);

			//File is sucesfully read, close the stream
			binReader.Close();

			//Check the format of the save and if it's supported load it (filter illegal characters from types)
			if (inputData.Length > 3) tempString = Encoding.ASCII.GetString(inputData, 0, 2).Trim((char)0x0);

			switch (tempString)
			{
				default: //Action Replay single save

					//Check if this is really an AR save (check for SC magic)
					if (!(inputData[0x36] == 0x53 && inputData[0x37] == 0x43)) return false;

					finalData = new byte[inputData.Length + 74];

					//Recreate save header
					finalData[0] = 0x51; //Q

					for (var i = 0; i < 20; i++)
						finalData[i + 10] = inputData[i];

					//Copy save data
					for (var i = 0; i < inputData.Length - 54; i++)
						finalData[i + 128] = inputData[i + 54];
					break;

				case "Q": //MCS single save
					finalData = inputData;
					break;

				case "SC": //RAW single save
					finalData = new byte[inputData.Length + 128];
					var singleSaveHeader = Encoding.Default.GetBytes(Path.GetFileName(fileName));

					//Recreate save header
					finalData[0] = 0x51; //Q

					for (var i = 0; i < 20 && i < singleSaveHeader.Length; i++)
						finalData[i + 10] = singleSaveHeader[i];

					//Copy save data
					for (var i = 0; i < inputData.Length; i++)
						finalData[i + 128] = inputData[i];
					break;

				case "V": //PSV single save (PS3 virtual save)
					//Check if this is a PS1 type save
					if (inputData[60] != 1) return false;

					finalData = new byte[inputData.Length - 4];

					//Recreate save header
					finalData[0] = 0x51; //Q

					for (var i = 0; i < 20; i++)
						finalData[i + 10] = inputData[i + 100];

					//Copy save data
					for (var i = 0; i < inputData.Length - 132; i++)
						finalData[i + 128] = inputData[i + 132];
					break;
			}

			//Import the save to Memory Card
			if (SetSaveBytes(slotNumber, finalData, out requiredSlots)) return true;
			return false;
		}

		public bool SaveTo(string filePath, MemoryCardType memoryCardType)
		{
			try
			{
				using (var file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
				{
					SaveTo(file, memoryCardType);
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

		public void SaveTo(Stream outputStream, MemoryCardType memoryCardType)
		{
			using (var binWriter = new BinaryWriter(outputStream))
			{
				CreateRawCardInternal();
				switch (memoryCardType)
				{
					case MemoryCardType.Gme:
						CreateGmeHeader();
						binWriter.Write(GmeHeader);
						binWriter.Write(rawData);
						break;

					case MemoryCardType.Vgs:
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
						CardType = MemoryCardType.Raw;
						break;

					case "123-456-STD":
						startOffset = 3904;
						CardType = MemoryCardType.Gme;
						for (var i = 0; i < GmeHeaderSize; i++)
							GmeHeader[i] = tempData[i];
						break;

					case "VgsM":
						startOffset = 64;
						CardType = MemoryCardType.Vgs;
						break;

					case "PMV": 
						startOffset = 128;
						CardType = MemoryCardType.Vmp;
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