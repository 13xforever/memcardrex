//Hardware card reading device information window
//Shendo 2012 - 2013

using System;
using System.ComponentModel;
using System.Windows.Forms;
using DexDriveCommunication;
using MemCARDuinoCommunication;
using PS1CardLinkCommunication;

namespace MemcardRex
{
	public partial class cardReaderWindow : Form
	{
		//MemCARDuino Memory Card reading device
		private readonly MemCARDuino CARDuino = new MemCARDuino();
		//DexDrive Memory Card reading device
		private readonly DexDrive dexDevice = new DexDrive();
		//PS1CardLink Memory Card reading device
		private readonly PS1CardLink PS1CLnk = new PS1CardLink();
		//Complete Memory Card data
		private byte[] completeMemoryCard = new byte[131072];
		//Currently active device (0 - DexDrive, 1 - MemCARDuino, 2 - PS1CardLink)
		private int currentDeviceIdentifier;
		//Maximum number of frames for writing (usually 1024 but 16 for quick format)
		private int maxWritingFrames;
		//Reading status flag
		private bool sucessfullRead;
		public cardReaderWindow() { InitializeComponent(); }
		//Read a Memory Card from DexDrive (return null if there was an error)
		public byte[] readMemoryCardDexDrive(Form hostWindow, string applicationName, string comPort)
		{
			//Initialize DexDrive
			var errorString = dexDevice.StartDexDrive(comPort);

			//Check if there were any errors
			if (errorString != null)
			{
				//Display an error message and cleanly close DexDrive communication
				new messageWindow().ShowMessage(hostWindow, applicationName, errorString, "OK", null, true);
				dexDevice.StopDexDrive();
				return null;
			}

			//Set scale for progress bar
			mainProgressBar.Maximum = 1024;

			//Set current device to DexDrive
			currentDeviceIdentifier = 0;

			//Set window title and information
			Text = "DexDrive communication";
			infoLabel.Text = "Reading data from DexDrive (ver. " + dexDevice.GetFirmwareVersion() + ")...";

			//Start reading data
			backgroundReader.RunWorkerAsync();

			ShowDialog(hostWindow);

			//Stop working with DexDrive
			dexDevice.StopDexDrive();

			//Check the final status (return data if all is ok, otherwise return null)
			if (sucessfullRead) return completeMemoryCard;
			return null;
		}

		//Read a Memory Card from MemCARDuino
		public byte[] readMemoryCardCARDuino(Form hostWindow, string applicationName, string comPort)
		{
			//Initialize MemCARDuino
			var errorString = CARDuino.StartMemCARDuino(comPort);

			//Check if there were any errors
			if (errorString != null)
			{
				//Display an error message and cleanly close MemCARDuino communication
				new messageWindow().ShowMessage(hostWindow, applicationName, errorString, "OK", null, true);
				CARDuino.StopMemCARDuino();
				return null;
			}

			//Set scale for progress bar
			mainProgressBar.Maximum = 1024;

			//Set current device to MemCARDuino
			currentDeviceIdentifier = 1;

			//Set window title and information
			Text = "MemCARDuino communication";
			infoLabel.Text = "Reading data from MemCARDuino (ver. " + CARDuino.GetFirmwareVersion() + ")...";

			//Start reading data
			backgroundReader.RunWorkerAsync();

			ShowDialog(hostWindow);

			//Stop working with MemCARDuino
			CARDuino.StopMemCARDuino();

			//Check the final status (return data if all is ok, otherwise return null)
			if (sucessfullRead) return completeMemoryCard;
			return null;
		}

		//Read a Memory Card from PS1CardLink
		public byte[] readMemoryCardPS1CLnk(Form hostWindow, string applicationName, string comPort)
		{
			//Initialize PS1CardLink
			var errorString = PS1CLnk.StartPS1CardLink(comPort);

			//Check if there were any errors
			if (errorString != null)
			{
				//Display an error message and cleanly close PS1CardLink communication
				new messageWindow().ShowMessage(hostWindow, applicationName, errorString, "OK", null, true);
				PS1CLnk.StopPS1CardLink();
				return null;
			}

			//Set scale for progress bar
			mainProgressBar.Maximum = 1024;

			//Set current device to PS1CardLink
			currentDeviceIdentifier = 2;

			//Set window title and information
			Text = "PS1CardLink communication";
			infoLabel.Text = "Reading data from PS1CardLink (ver. " + PS1CLnk.GetSoftwareVersion() + ")...";

			//Start reading data
			backgroundReader.RunWorkerAsync();

			ShowDialog(hostWindow);

			//Stop working with PS1CardLink
			PS1CLnk.StopPS1CardLink();

			//Check the final status (return data if all is ok, otherwise return null)
			if (sucessfullRead) return completeMemoryCard;
			return null;
		}

		//Write a Memory Card to DexDrive
		public void writeMemoryCardDexDrive(Form hostWindow, string applicationName, string comPort, byte[] memoryCardData, int frameNumber)
		{
			//Initialize DexDrive
			var errorString = dexDevice.StartDexDrive(comPort);

			//Check if there were any errors
			if (errorString != null)
			{
				//Display an error message and cleanly close DexDrive communication
				new messageWindow().ShowMessage(hostWindow, applicationName, errorString, "OK", null, true);
				dexDevice.StopDexDrive();
				return;
			}

			//Set maximum number of frames to write
			maxWritingFrames = frameNumber;

			//Set scale for progress bar
			mainProgressBar.Maximum = frameNumber;

			//Set current device to DexDrive
			currentDeviceIdentifier = 0;

			//Set window title and information
			Text = "DexDrive communication";
			infoLabel.Text = "Writing data to DexDrive (ver. " + dexDevice.GetFirmwareVersion() + ")...";

			//Set reference to the Memory Card data
			completeMemoryCard = memoryCardData;

			//Start writing data
			backgroundWriter.RunWorkerAsync();

			ShowDialog(hostWindow);

			//Stop working with DexDrive
			dexDevice.StopDexDrive();
		}

		//Write a Memory Card to MemCARDuino
		public void writeMemoryCardCARDuino(Form hostWindow, string applicationName, string comPort, byte[] memoryCardData, int frameNumber)
		{
			//Initialize MemCARDuino
			var errorString = CARDuino.StartMemCARDuino(comPort);

			//Check if there were any errors
			if (errorString != null)
			{
				//Display an error message and cleanly close MemCARDuino communication
				new messageWindow().ShowMessage(hostWindow, applicationName, errorString, "OK", null, true);
				CARDuino.StopMemCARDuino();
				return;
			}

			//Set maximum number of frames to write
			maxWritingFrames = frameNumber;

			//Set scale for progress bar
			mainProgressBar.Maximum = frameNumber;

			//Set current device to MemCARDuino
			currentDeviceIdentifier = 1;

			//Set window title and information
			Text = "MemCARDuino communication";
			infoLabel.Text = "Writing data to MemCARDuino (ver. " + CARDuino.GetFirmwareVersion() + ")...";

			//Set reference to the Memory Card data
			completeMemoryCard = memoryCardData;

			//Start writing data
			backgroundWriter.RunWorkerAsync();

			ShowDialog(hostWindow);

			//Stop working with MemCARDuino
			CARDuino.StopMemCARDuino();
		}

		//Write a Memory Card to PS1CardLink
		public void writeMemoryCardPS1CLnk(Form hostWindow, string applicationName, string comPort, byte[] memoryCardData, int frameNumber)
		{
			//Initialize PS1CardLink
			var errorString = PS1CLnk.StartPS1CardLink(comPort);

			//Check if there were any errors
			if (errorString != null)
			{
				//Display an error message and cleanly close PS1CardLink communication
				new messageWindow().ShowMessage(hostWindow, applicationName, errorString, "OK", null, true);
				PS1CLnk.StopPS1CardLink();
				return;
			}

			//Set maximum number of frames to write
			maxWritingFrames = frameNumber;

			//Set scale for progress bar
			mainProgressBar.Maximum = frameNumber;

			//Set current device to PS1CardLink
			currentDeviceIdentifier = 2;

			//Set window title and information
			Text = "PS1CardLink communication";
			infoLabel.Text = "Writing data to PS1CardLink (ver. " + PS1CLnk.GetSoftwareVersion() + ")...";

			//Set reference to the Memory Card data
			completeMemoryCard = memoryCardData;

			//Start writing data
			backgroundWriter.RunWorkerAsync();

			ShowDialog(hostWindow);

			//Stop working with PS1CardLink
			PS1CLnk.StopPS1CardLink();
		}

		private void OKbutton_Click(object sender, EventArgs e)
		{
			//Cancel reading job
			if (backgroundReader.IsBusy) backgroundReader.CancelAsync();

			//Cancel writing job
			if (backgroundWriter.IsBusy) backgroundWriter.CancelAsync();
		}

		private void backgroundReader_DoWork(object sender, DoWorkEventArgs e)
		{
			byte[] tempDataBuffer = null;
			ushort i = 0;

			//Read all 1024 frames of the Memory Card
			while (i < 1024)
			{
				//Check if the "Abort" button has been pressed
				if (backgroundReader.CancellationPending) return;

				//Get 128 byte frame data from DexDrive
				if (currentDeviceIdentifier == 0) tempDataBuffer = dexDevice.ReadMemoryCardFrame(i);

				//Get 128 byte frame data from MemCARDuino
				if (currentDeviceIdentifier == 1) tempDataBuffer = CARDuino.ReadMemoryCardFrame(i);

				//Get 128 byte frame data from PS1CardLink
				if (currentDeviceIdentifier == 2) tempDataBuffer = PS1CLnk.ReadMemoryCardFrame(i);

				//Check if there was a checksum mismatch
				if (tempDataBuffer != null)
				{
					Array.Copy(tempDataBuffer, 0, completeMemoryCard, i*128, 128);
					backgroundReader.ReportProgress(i);
					i++;
				}
			}

			//All data has been read, report success
			sucessfullRead = true;
		}

		private void backgroundReader_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			//Report the read progress to the progress bar
			mainProgressBar.Value = e.ProgressPercentage;
		}

		private void backgroundReader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//Reading was completed or canceled, close the form
			Close();
		}

		private void backgroundWriter_DoWork(object sender, DoWorkEventArgs e)
		{
			var tempDataBuffer = new byte[128];
			ushort i = 0;
			var lastStatus = false;

			//Write all required frames of the Memory Card
			while (i < maxWritingFrames)
			{
				//Check if the "Abort" button has been pressed
				if (backgroundWriter.CancellationPending) return;

				//Get 128 byte frame data
				Array.Copy(completeMemoryCard, i*128, tempDataBuffer, 0, 128);

				//Reset write status
				lastStatus = false;

				//Write data to DexDrive
				if (currentDeviceIdentifier == 0) lastStatus = dexDevice.WriteMemoryCardFrame(i, tempDataBuffer);

				//Write data to MemCARDuino
				if (currentDeviceIdentifier == 1) lastStatus = CARDuino.WriteMemoryCardFrame(i, tempDataBuffer);

				//Write data to PS1CardLink
				if (currentDeviceIdentifier == 2) lastStatus = PS1CLnk.WriteMemoryCardFrame(i, tempDataBuffer);

				//Check if there was a frame or checksum mismatch
				if (lastStatus)
				{
					backgroundWriter.ReportProgress(i);
					i++;
				}
			}

			//All data has been written, report success
			sucessfullRead = true;
		}

		private void backgroundWriter_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			//Report the write progress to the progress bar
			mainProgressBar.Value = e.ProgressPercentage;
		}

		private void backgroundWriter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//Writing was completed or canceled, close the form
			Close();
		}
	}
}