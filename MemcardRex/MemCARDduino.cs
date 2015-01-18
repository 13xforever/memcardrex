//MemCARDuino communication class
//Shendo 2013

using System;
using System.IO.Ports;
using System.Threading;

namespace MemCARDuinoCommunication
{
	internal class MemCARDuino
	{
		//Contains a firmware version of a detected device
		private string FirmwareVersion = "0.0";
		//MCino communication port
		private SerialPort OpenedPort;

		public string StartMemCARDuino(string ComPortName)
		{
			//Define a port to open
			OpenedPort = new SerialPort(ComPortName, 38400, Parity.None, 8, StopBits.One);
			OpenedPort.ReadBufferSize = 256;

			//Buffer for storing read data from the MCino
			byte[] ReadData = null;

			//Try to open a selected port (in case of an error return a descriptive string)
			try
			{
				OpenedPort.Open();
			}
			catch (Exception e)
			{
				return e.Message;
			}

			//DTR needs to be toggled to reset Arduino and set it to serial mode
			OpenedPort.DtrEnable = true;
			OpenedPort.DtrEnable = false;
			Thread.Sleep(2000);

			//Check if this is MCino
			SendDataToPort((byte)MCinoCommands.GETID, 100);
			ReadData = ReadDataFromPort();

			if (ReadData[0] != 'M' || ReadData[1] != 'C' || ReadData[2] != 'D' || ReadData[3] != 'I' || ReadData[4] != 'N' || ReadData[5] != 'O')
			{
				return "MemCARDuino was not detected on '" + ComPortName + "' port.";
			}

			//Get the firmware version
			SendDataToPort((byte)MCinoCommands.GETVER, 30);
			ReadData = ReadDataFromPort();

			FirmwareVersion = (ReadData[0] >> 4) + "." + (ReadData[0] & 0xF);

			//Everything went well, MCino is ready to be used
			return null;
		}

		//Cleanly stop working with MCino
		public void StopMemCARDuino() { if (OpenedPort.IsOpen) OpenedPort.Close(); }
		//Get the firmware version of MCino
		public string GetFirmwareVersion() { return FirmwareVersion; }
		//Send MCino command on the opened COM port with a delay
		private void SendDataToPort(byte Command, int Delay)
		{
			//Clear everything in the input buffer
			OpenedPort.DiscardInBuffer();

			//Send Command Byte
			OpenedPort.Write(new[] {Command}, 0, 1);

			//Wait for a required timeframe (for the MCino response)
			if (Delay > 0) Thread.Sleep(Delay);
		}

		//Catch the response from a MCino
		private byte[] ReadDataFromPort()
		{
			//Buffer for reading data
			var InputStream = new byte[256];

			//Read data from MCino
			if (OpenedPort.BytesToRead != 0) OpenedPort.Read(InputStream, 0, 256);

			return InputStream;
		}

		//Read a specified frame of a Memory Card
		public byte[] ReadMemoryCardFrame(ushort FrameNumber)
		{
			var DelayCounter = 0;

			//Buffer for storing read data from MCino
			byte[] ReadData = null;

			//128 byte frame data from a Memory Card
			var ReturnDataBuffer = new byte[128];

			var FrameLsb = (byte)(FrameNumber & 0xFF); //Least significant byte
			var FrameMsb = (byte)(FrameNumber >> 8); //Most significant byte
			var XorData = (byte)(FrameMsb ^ FrameLsb); //XOR variable for consistency checking

			//Read a frame from the Memory Card
			SendDataToPort((byte)MCinoCommands.MCR, 0);
			SendDataToPort(FrameMsb, 0);
			SendDataToPort(FrameLsb, 0);

			//Wait for the buffer to fill
			while (OpenedPort.BytesToRead < 130 && DelayCounter < 18)
			{
				Thread.Sleep(5);
				DelayCounter++;
			}

			ReadData = ReadDataFromPort();

			//Copy recieved data
			Array.Copy(ReadData, 0, ReturnDataBuffer, 0, 128);

			//Calculate XOR checksum
			for (var i = 0; i < 128; i++)
			{
				XorData ^= ReturnDataBuffer[i];
			}

			//Return null if there is a checksum missmatch
			if (XorData != ReadData[128] || ReadData[129] != (byte)MCinoResponses.GOOD) return null;

			//Return read data
			return ReturnDataBuffer;
		}

		//Write a specified frame to a Memory Card
		public bool WriteMemoryCardFrame(ushort FrameNumber, byte[] FrameData)
		{
			var DelayCounter = 0;

			//Buffer for storing read data from MCino
			byte[] ReadData = null;

			var FrameLsb = (byte)(FrameNumber & 0xFF); //Least significant byte
			var FrameMsb = (byte)(FrameNumber >> 8); //Most significant byte
			var XorData = (byte)(FrameMsb ^ FrameLsb); //XOR variable for consistency checking

			//Calculate XOR checksum
			for (var i = 0; i < 128; i++)
			{
				XorData ^= FrameData[i];
			}

			OpenedPort.DiscardInBuffer();

			//Write a frame to the Memory Card
			SendDataToPort((byte)MCinoCommands.MCW, 0);
			SendDataToPort(FrameMsb, 0);
			SendDataToPort(FrameLsb, 0);
			OpenedPort.Write(FrameData, 0, 128);
			SendDataToPort(XorData, 0); //XOR Checksum

			//Wait for the buffer to fill
			while (OpenedPort.BytesToRead < 1 && DelayCounter < 18)
			{
				Thread.Sleep(5);
				DelayCounter++;
			}

			//Fetch MCino's response to the last command
			ReadData = ReadDataFromPort();

			if (ReadData[0x0] == (byte)MCinoResponses.GOOD) return true;

			//Data was not written sucessfully
			return false;
		}

		private enum MCinoCommands
		{
			GETID = 0xA0,
			GETVER = 0xA1,
			MCR = 0xA2,
			MCW = 0xA3
		};

		private enum MCinoResponses
		{
			ERROR = 0xE0,
			GOOD = 0x47,
			BADCHECKSUM = 0x4E,
			BADSECTOR = 0xFF
		};
	}
}