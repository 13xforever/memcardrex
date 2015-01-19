﻿/*
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

//Main window of the MemcardRex application
//Shendo 2009 - 2014

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using BuildTracker;
using MemcardRex.Enums;
using MemcardRex.Enums.MemoryCard;
using MemcardRex.Properties;
using Type = MemcardRex.Enums.MemoryCard.Type;

namespace MemcardRex
{
	public partial class mainWindow : Form
	{
		//Application related strings
		private const string appName = "MemcardRex";
		private const string appDate = BuildTrack.ApplicationDate;

#if DEBUG
		private const string appVersion = "1.9." + BuildTrack.ApplicationBuild + " (beta)";
#else
        const string appVersion = "1.9";
#endif

		//Location of the application
		private readonly string appPath = Application.StartupPath;

		//API for the Aero glass effect
		private readonly glassSupport windowGlass = new glassSupport();

		//Margins for the Aero glass
		private glassSupport.margins windowMargins;

		//Rectangle for the Aero glass
		private Rectangle windowRectangle;

		//Plugin system (public because plugin dialog has to access it)
		public rexPluginSystem pluginSystem = new rexPluginSystem();

		//Supported plugins for the currently selected save
		private int[] supportedPlugins;

		//Currently clicked plugin (0 - clicked flag, 1 - plugin index)
		private readonly int[] clickedPlugin = {0, 0};

		//Struct holding all program related settings (public because settings dialog has to access it)
		public struct programSettings
		{
			public int backupMemcards; //Backup Memory Card settings
			public string communicationPort; //Communication port for Hardware interfaces
			public int formatType; //Type of formatting for hardware interfaces
			public int glassStatusBar; //Vista glass status bar
			public int iconBackgroundColor; //Various colors based on PS1 BIOS backgrounds
			public int iconInterpolationMode; //Icon iterpolation mode settings
			public int iconPropertiesSize; //Icon size settings in save properties
			public string listFont; //List font
			public int restoreWindowPosition; //Restore window position
			public int showListGrid; //List grid settings
			public int titleEncoding; //Encoding of the save titles (0 - ASCII, 1 - UTF-16)
			public int warningMessage; //Warning message settings
		}

		//All program settings
		private programSettings mainSettings;

		//List of opened Memory Cards
		private readonly List<PsOneCard> PScard = new List<PsOneCard>();

		//Listview of the opened Memory Cards
		private readonly List<ListView> cardList = new List<ListView>();

		//List of icons for the saves
		private readonly List<ImageList> iconList = new List<ImageList>();

		//Temp buffer used to store saves
		private byte[] tempBuffer;
		private string tempBufferName;

		public mainWindow() { InitializeComponent(); }

		//Apply glass effect on the client area
		private void applyGlass()
		{
			//Reset margins to zero
			windowMargins.top = 0;
			windowMargins.bottom = 0;
			windowMargins.left = 0;
			windowMargins.right = 0;

			//Check if the requirements for the Aero glass are met
			if (windowGlass.isGlassSupported() && mainSettings.glassStatusBar == 1)
			{
				//Hide status strip
				mainStatusStrip.Visible = false;

				windowMargins.bottom = 22;
				windowRectangle = new Rectangle(0, ClientSize.Height - windowMargins.bottom, ClientSize.Width, windowMargins.bottom + 5);
				glassSupport.DwmExtendFrameIntoClientArea(Handle, ref windowMargins);

				//Repaint the form
				Refresh();
			}
			else
			{
				//Check if effect of aero needs to be supressed
				if (Environment.OSVersion.Version.Major >= 6)
				{
					windowMargins.bottom = 0;
					windowRectangle = new Rectangle(0, ClientSize.Height - windowMargins.bottom, ClientSize.Width, windowMargins.bottom + 5);
					glassSupport.DwmExtendFrameIntoClientArea(Handle, ref windowMargins);

					//Repaint the form
					Refresh();
				}

				//Show status strip
				mainStatusStrip.Visible = true;
			}
		}

		//Apply program settings
		private void applySettings()
		{
			//Refresh all active lists
			for (var i = 0; i < cardList.Count; i++)
				refreshListView(i, cardList[i].SelectedIndices[0]);

			//Refresh status of Aero glass
			applyGlass();
		}

		//Apply program settings from given values
		public void applyProgramSettings(programSettings progSettings)
		{
			mainSettings = progSettings;

			//Apply given settings
			applySettings();
		}

		//Load program settings
		private void loadProgramSettings()
		{
			var mainWindowLocation = new Point(0, 0);
			var xmlAppSettings = new xmlSettingsEditor();

			//Check if the Settings.xml exists
			if (File.Exists(appPath + "/Settings.xml"))
			{
				//Open XML file for reading, file is auto-closed
				xmlAppSettings.openXmlReader(appPath + "/Settings.xml");

				//Load list font
				mainSettings.listFont = xmlAppSettings.readXmlEntry("ListFont");

				//Load DexDrive COM port
				mainSettings.communicationPort = xmlAppSettings.readXmlEntry("ComPort");

				//Load Title Encoding
				mainSettings.titleEncoding = xmlAppSettings.readXmlEntryInt("TitleEncoding", 0, 1);

				//Load List Grid settings
				mainSettings.showListGrid = xmlAppSettings.readXmlEntryInt("ShowGrid", 0, 1);

				//Load glass option switch
				mainSettings.glassStatusBar = xmlAppSettings.readXmlEntryInt("GlassStatusBar", 0, 1);

				//Load icon interpolation settings
				mainSettings.iconInterpolationMode = xmlAppSettings.readXmlEntryInt("IconInterpolationMode", 0, 1);

				//Load icon size settings
				mainSettings.iconPropertiesSize = xmlAppSettings.readXmlEntryInt("IconSize", 0, 1);

				//Load icon background color
				mainSettings.iconBackgroundColor = xmlAppSettings.readXmlEntryInt("IconBackgroundColor", 0, 4);

				//Load backup Memory Cards value
				mainSettings.backupMemcards = xmlAppSettings.readXmlEntryInt("BackupMemoryCards", 0, 1);

				//Load warning message switch
				mainSettings.warningMessage = xmlAppSettings.readXmlEntryInt("WarningMessage", 0, 1);

				//Load window position switch
				mainSettings.restoreWindowPosition = xmlAppSettings.readXmlEntryInt("RestoreWindowPosition", 0, 1);

				//Load format type
				mainSettings.formatType = xmlAppSettings.readXmlEntryInt("HardwareFormatType", 0, 1);

				//Check if window position should be read
				if (mainSettings.restoreWindowPosition == 1)
				{
					mainWindowLocation.X = xmlAppSettings.readXmlEntryInt("WindowX", -65535, 65535);
					mainWindowLocation.Y = xmlAppSettings.readXmlEntryInt("WindowY", -65535, 65535);

					//Apply read position
					Location = mainWindowLocation;
				}

				//Apply loaded settings
				applySettings();
			}
		}

		//Save program settings
		private void saveProgramSettings()
		{
			var xmlAppSettings = new xmlSettingsEditor();

			//Open XML file for writing
			xmlAppSettings.openXmlWriter(appPath + "/Settings.xml", appName + " " + appVersion + " settings data");

			//Set list font
			xmlAppSettings.writeXmlEntry("ListFont", mainSettings.listFont);

			//Set DexDrive port
			xmlAppSettings.writeXmlEntry("ComPort", mainSettings.communicationPort);

			//Set title encoding
			xmlAppSettings.writeXmlEntry("TitleEncoding", mainSettings.titleEncoding.ToString());

			//Set List Grid settings
			xmlAppSettings.writeXmlEntry("ShowGrid", mainSettings.showListGrid.ToString());

			//Set glass option switch
			xmlAppSettings.writeXmlEntry("GlassStatusBar", mainSettings.glassStatusBar.ToString());

			//Set icon interpolation settings
			xmlAppSettings.writeXmlEntry("IconInterpolationMode", mainSettings.iconInterpolationMode.ToString());

			//Set icon size options
			xmlAppSettings.writeXmlEntry("IconSize", mainSettings.iconPropertiesSize.ToString());

			//Set icon background color
			xmlAppSettings.writeXmlEntry("IconBackgroundColor", mainSettings.iconBackgroundColor.ToString());

			//Set backup Memory Cards value
			xmlAppSettings.writeXmlEntry("BackupMemoryCards", mainSettings.backupMemcards.ToString());

			//Set warning message switch
			xmlAppSettings.writeXmlEntry("WarningMessage", mainSettings.warningMessage.ToString());

			//Set window position switch
			xmlAppSettings.writeXmlEntry("RestoreWindowPosition", mainSettings.restoreWindowPosition.ToString());

			//Set format type
			xmlAppSettings.writeXmlEntry("HardwareFormatType", mainSettings.formatType.ToString());

			//Set window X coordinate
			xmlAppSettings.writeXmlEntry("WindowX", Location.X.ToString());

			//Set window Y coordinate
			xmlAppSettings.writeXmlEntry("WindowY", Location.Y.ToString());

			//Cleanly close opened XML file
			xmlAppSettings.closeXmlWriter();
		}

		//Backup a Memory Card
		private void backupMemcard(string fileName)
		{
			//Check if backuping of memcard is allowed and the filename is valid
			if (mainSettings.backupMemcards == 1 && fileName != null)
			{
				var fInfo = new FileInfo(fileName);

				//Backup only if file is less then 512KB
				if (fInfo.Length < 524288)
				{
					//Copy the file
					try
					{
						//Check if the backup directory exists and create it if it's missing
						if (!Directory.Exists(appPath + "/Backup")) Directory.CreateDirectory(appPath + "/Backup");

						//Copy the file (make a backup of it)
						File.Copy(fileName, appPath + "/Backup/" + fInfo.Name);
					}
					catch
					{
					}
				}
			}
		}

		//Remove the first "Untitled" card if the user opened a valid card
		private void filterNullCard()
		{
			//Check if there are any cards opened
			if (PScard.Count > 0)
			{
				if (PScard.Count == 2 && PScard[0].CardLocation == null && PScard[0].ChangedFlag == false)
				{
					closeCard(0);
				}
			}
		}

		//Open a Memory Card with OpenFileDialog
		private void openCardDialog()
		{
			var openFileDlg = new OpenFileDialog();
			openFileDlg.Title = "Open Memory Card";
			openFileDlg.Filter =
				"All supported|*.mcr;*.gme;*.bin;*.mcd;*.mem;*.vgs;*.mc;*.ddf;*.ps;*.psm;*.mci;*.VMP;*.VM1|ePSXe/PSEmu Pro Memory Card (*.mcr)|*.mcr|DexDrive Memory Card (*.gme)|*.gme|pSX/AdriPSX Memory Card (*.bin)|*.bin|Bleem! Memory Card (*.mcd)|*.mcd|VGS Memory Card (*.mem, *.vgs)|*.mem; *.vgs|PSXGame Edit Memory Card (*.mc)|*.mc|DataDeck Memory Card (*.ddf)|*.ddf|WinPSM Memory Card (*.ps)|*.ps|Smart Link Memory Card (*.psm)|*.psm|MCExplorer (*.mci)|*.mci|PSP virtual Memory Card (*.VMP)|*.VMP|PS3 virtual Memory Card (*.VM1)|*.VM1|All files (*.*)|*.*";
			openFileDlg.Multiselect = true;

			//If user selected a card open it
			if (openFileDlg.ShowDialog() == DialogResult.OK)
			{
				foreach (var fileName in openFileDlg.FileNames)
				{
					openCard(fileName);
				}
			}
		}

		//Open a Memory Card from the given filename
		private void openCard(string fileName)
		{
			//Check if the card already exists
			foreach (var checkCard in PScard)
				if (checkCard.CardLocation == fileName && fileName != null)
				{
					//Card is already opened, display message and exit
					new messageWindow().ShowMessage(this, appName, "'" + Path.GetFileName(fileName) + "' is already opened.", "OK", null, true);
					return;
				}

			try
			{
				var newCard = new PsOneCard();
				if (string.IsNullOrEmpty(fileName))
				{
					newCard.Format();
					newCard.ChangedFlag = false;
				}
				else
				{
					newCard.ImportFrom(fileName);
					backupMemcard(fileName);
				}
				PScard.Add(newCard);
				createTabPage();
			}
			catch (Exception e)
			{
				new messageWindow().ShowMessage(this, appName, e.Message, "OK", null, true);
			}
		}

		//Create a new tab page for the Memory Card
		private void createTabPage()
		{
			//Make new tab page
			var tabPage = new TabPage();

			//Set default color
			tabPage.BackColor = SystemColors.Window;

			//Add a tab corresponding to opened card
			mainTabControl.TabPages.Add(tabPage);

			//Make a new ListView control
			makeListView();

			//Add ListView control to the tab page
			tabPage.Controls.Add(cardList[cardList.Count - 1]);

			//Delete the initial "Untitled" card
			if (PScard[PScard.Count - 1].CardLocation != null) filterNullCard();

			//Switch the active tab to the currently opened card
			mainTabControl.SelectedIndex = PScard.Count - 1;

			//Show the location of the card in the tool strip
			refreshStatusStrip();

			//Enable "Close", "Close All", "Save" and "Save as" menu items
			closeToolStripMenuItem.Enabled = true;
			closeAllToolStripMenuItem.Enabled = true;
			saveToolStripMenuItem.Enabled = true;
			saveButton.Enabled = true;
			saveAsToolStripMenuItem.Enabled = true;
		}

		//Save a Memory Card with SaveFileDialog
		private void saveCardDialog(int listIndex)
		{
			//Check if there are any cards to save
			if (PScard.Count > 0)
			{
				Type type;
				var saveFileDlg = new SaveFileDialog
								{
									Title = "Save Memory Card",
									Filter = "ePSXe/PSEmu Pro Memory Card (*.mcr)|*.mcr|" +
											"DexDrive Memory Card (*.gme)|*.gme|" +
											"pSX/AdriPSX Memory Card (*.bin)|*.bin|" +
											"Bleem! Memory Card (*.mcd)|*.mcd|" +
											"VGS Memory Card (*.mem, *.vgs)|*.mem; *.vgs|" +
											"PSXGame Edit Memory Card (*.mc)|*.mc|" +
											"DataDeck Memory Card (*.ddf)|*.ddf|" +
											"WinPSM Memory Card (*.ps)|*.ps|" +
											"Smart Link Memory Card (*.psm)|*.psm|" +
											"MCExplorer (*.mci)|*.mci|" +
											"PS3 virtual Memory Card (*.VM1)|*.VM1",
								};

				//If user selected a card save to it
				if (saveFileDlg.ShowDialog() == DialogResult.OK)
				{
					//Get save type
					switch (saveFileDlg.FilterIndex)
					{
						case 2:
							type = Type.Gme;
							break;

						case 5:
							type = Type.Vgs;
							break;

						default:
							type = Type.Raw;
							break;
					}
					saveMemoryCard(listIndex, saveFileDlg.FileName, type);
				}
			}
		}

		//Save a Memory Card to a given filename
		private void saveMemoryCard(int listIndex, string fileName, Type type)
		{
			if (PScard[listIndex].ExportTo(fileName, type))
			{
				refreshListView(listIndex, cardList[listIndex].SelectedIndices[0]);
				refreshStatusStrip();
			}
			else
				new messageWindow().ShowMessage(this, appName, "Memory Card could not be saved.", "OK", null, true);
		}

		//Save a selected Memory Card
		private void saveCardFunction(int listIndex)
		{
			//Check if there are any cards to save
			if (PScard.Count > 0)
			{
				//Check if file can be saved or save dialog must be shown (VMP is read only)
				if (PScard[listIndex].CardLocation == null || PScard[listIndex].CardType == Type.Vmp)
					saveCardDialog(listIndex);
				else
					saveMemoryCard(listIndex, PScard[listIndex].CardLocation, PScard[listIndex].CardType);
			}
		}

		//Cleanly close the selected card
		private void closeCard(int listIndex, bool switchToFirst)
		{
			//Check if there are any cards to delete
			if (PScard.Count > 0)
			{
				//Ask for saving before closing
				savePrompt(listIndex);

				PScard.RemoveAt(listIndex);
				cardList.RemoveAt(listIndex);
				iconList.RemoveAt(listIndex);
				mainTabControl.TabPages.RemoveAt(listIndex);

				//Select first tab
				if (PScard.Count > 0 && switchToFirst)
					mainTabControl.SelectedIndex = 0;

				//Refresh plugin list
				refreshPluginBindings();

				//Enable certain list items
				enableSelectiveEditItems();
			}

			//If this was the last card disable "Close", "Close All", "Save" and "Save as" menu items
			if (PScard.Count <= 0)
			{
				closeToolStripMenuItem.Enabled = false;
				closeAllToolStripMenuItem.Enabled = false;
				saveToolStripMenuItem.Enabled = false;
				saveButton.Enabled = false;
				saveAsToolStripMenuItem.Enabled = false;
			}
		}

		//Overload for closeCard function
		private void closeCard(int listIndex) { closeCard(listIndex, true); }

		//Close all opened cards
		private void closeAllCards()
		{
			//Run trough the loop as long as there are cards opened
			while (PScard.Count > 0)
			{
				mainTabControl.SelectedIndex = 0;
				closeCard(0);
			}
		}

		//Edit save comments
		private void editSaveComments()
		{
			//Check if there are any cards to edit comments on
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var saveTitle = PScard[listIndex].SaveName[slotNumber, mainSettings.titleEncoding];
				var saveComment = PScard[listIndex].SaveComments[slotNumber];

				//Check if comments are allowed to be edited
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					default: //Not allowed
						break;

					case SaveType.Initial:
					case SaveType.DeletedInitial:
						var commentsDlg = new commentsWindow();

						//Load values to dialog
						commentsDlg.initializeDialog(saveTitle, saveComment);
						commentsDlg.ShowDialog(this);

						//Update values if OK was pressed
						if (commentsDlg.okPressed)
						{
							//Insert edited comments back in the card
							PScard[listIndex].SaveComments[slotNumber] = commentsDlg.saveComment;
						}
						commentsDlg.Dispose();
						break;
				}
			}
		}

		//Create and show information dialog
		private void showInformation()
		{
			//Check if there are any cards
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var saveRegion = PScard[listIndex].SaveRegion[slotNumber];
				var saveSize = PScard[listIndex].SaveSize[slotNumber];
				var iconFrames = PScard[listIndex].IconFrames[slotNumber];
				var saveProdCode = PScard[listIndex].SaveProductCode[slotNumber];
				var saveIdentifier = PScard[listIndex].SaveIdentifier[slotNumber];
				var saveTitle = PScard[listIndex].SaveName[slotNumber, mainSettings.titleEncoding];
				var saveIcons = new Bitmap[3];

				//Get all 3 bitmaps for selected save
				for (var i = 0; i < 3; i++)
					saveIcons[i] = PScard[listIndex].IconData[slotNumber, i];

				//Check if slot is "legal"
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.Initial:
					case SaveType.DeletedInitial:
						using (var informationDlg = new informationWindow())
						{
							//Load values to dialog
							informationDlg.initializeDialog(saveTitle, saveProdCode, saveIdentifier, saveRegion, saveSize, iconFrames, mainSettings.iconInterpolationMode, mainSettings.iconPropertiesSize, saveIcons, PScard[listIndex].FindSaveLinks(slotNumber),
								mainSettings.iconBackgroundColor);
							informationDlg.ShowDialog(this);
						}
						break;
				}
			}
		}

		//Restore selected save
		private void restoreSave()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;
				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.DeletedInitial:
						PScard[listIndex].ToggleDeleteSave(slotNumber);
						refreshListView(listIndex, slotNumber);
						break;

					case SaveType.Initial:
						new messageWindow().ShowMessage(this, appName, "The selected save is not deleted.", "OK", null, true);
						break;

					case SaveType.MiddleLink:
					case SaveType.EndLink:
					case SaveType.DeletedMiddleLink:
					case SaveType.DeletedEndLink:
						new messageWindow().ShowMessage(this, appName, "The selected slot is linked. Select the initial save slot to proceed.", "OK", null, true);
						break;
				}
			}
		}

		//Delete selected save
		private void deleteSave()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];

				//Check the save type
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.Initial:
						PScard[listIndex].ToggleDeleteSave(slotNumber);
						refreshListView(listIndex, slotNumber);
						break;

					case SaveType.DeletedInitial:
						new messageWindow().ShowMessage(this, appName, "The selected save is already deleted.", "OK", null, true);
						break;

					case SaveType.MiddleLink:
					case SaveType.EndLink:
					case SaveType.DeletedMiddleLink:
					case SaveType.DeletedEndLink:
						new messageWindow().ShowMessage(this, appName, "The selected slot is linked. Select the initial save slot to proceed.", "OK", null, true);
						break;
				}
			}
		}

		//Format selected save
		private void formatSave()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];

				//Check the save type
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					default: //Slot is either initial, deleted initial or corrupted so it can be safetly formatted
						if (new messageWindow().ShowMessage(this, appName, "Formatted slots cannot be restored.\nDo you want to proceed with this operation?", "No", "Yes", true) == "Yes")
						{
							PScard[listIndex].FormatSave(slotNumber);
							refreshListView(listIndex, slotNumber);
						}
						break;

					case SaveType.MiddleLink:
					case SaveType.EndLink:
					case SaveType.DeletedMiddleLink:
					case SaveType.DeletedEndLink:
						new messageWindow().ShowMessage(this, appName, "The selected slot is linked. Select the initial save slot to proceed.", "OK", null, true);
						break;
				}
			}
		}

		//Copy save selected save from Memory Card
		private void copySave()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var saveName = PScard[listIndex].SaveName[slotNumber, 0];

				//Check the save type
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.Initial:
					case SaveType.DeletedInitial:
						tempBuffer = PScard[listIndex].GetSaveBytes(slotNumber);
						tempBufferName = PScard[listIndex].SaveName[slotNumber, 0];

						//Show temp buffer toolbar info
						tBufToolButton.Enabled = true;
						tBufToolButton.Image = PScard[listIndex].IconData[slotNumber, 0];
						tBufToolButton.Text = saveName;

						//Refresh the current list
						refreshListView(listIndex, slotNumber);

						break;

					case SaveType.MiddleLink:
					case SaveType.EndLink:
					case SaveType.DeletedMiddleLink:
					case SaveType.DeletedEndLink:
						new messageWindow().ShowMessage(this, appName, "The selected slot is linked. Select the initial save slot to proceed.", "OK", null, true);
						break;
				}
			}
		}

		//Paste save to Memory Card
		private void pasteSave()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var requiredSlots = 0;

				//Check if temp buffer contains anything
				if (tempBuffer != null)
				{
					//Check if the slot to paste the save on is free
					if (PScard[listIndex].SaveType[slotNumber] == 0)
					{
						if (PScard[listIndex].SetSaveBytes(slotNumber, tempBuffer, out requiredSlots))
						{
							refreshListView(listIndex, slotNumber);
						}
						else
						{
							new messageWindow().ShowMessage(this, appName, "To complete this operation " + requiredSlots + " free slots are required.", "OK", null, true);
						}
					}
					else
					{
						new messageWindow().ShowMessage(this, appName, "The selected slot is not empty.", "OK", null, true);
					}
				}
				else
				{
					new messageWindow().ShowMessage(this, appName, "Temp buffer is empty.", "OK", null, true);
				}
			}
		}

		//Export a save
		private void exportSaveDialog()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];

				//Check the save type
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.Initial:
						SingleSaveFormat singleSaveType = 0;

						//Set output filename
						var outputFilename = getRegionString(PScard[listIndex].SaveRegion[slotNumber]) + PScard[listIndex].SaveProductCode[slotNumber] + PScard[listIndex].SaveIdentifier[slotNumber];

						//Filter illegal characters from the name
						foreach (var illegalChar in Path.GetInvalidPathChars())
						{
							outputFilename = outputFilename.Replace(illegalChar.ToString(), "");
						}

						var saveFileDlg = new SaveFileDialog();
						saveFileDlg.Title = "Export save";
						saveFileDlg.FileName = outputFilename;
						saveFileDlg.Filter = "PSXGameEdit single save (*.mcs)|*.mcs|XP, AR, GS, Caetla single save (*.psx)|*.psx|Memory Juggler (*.ps1)|*.ps1|Smart Link (*.mcb)|*.mcb|Datel (*.mcx;*.pda)|*.mcx;*.pda|RAW single save|B???????????*";

						if (saveFileDlg.ShowDialog() == DialogResult.OK)
						{
							switch (saveFileDlg.FilterIndex)
							{
								case 1: //MCS single save
								case 3: //PS1 (Memory Juggler)
									singleSaveType = SingleSaveFormat.Mcs;
									break;

								case 6: //RAW single save
									singleSaveType = SingleSaveFormat.Raw;

									//Omit the extension if the user left it
									saveFileDlg.FileName = saveFileDlg.FileName.Split('.')[0];
									break;

								default: //Action Replay
									singleSaveType = SingleSaveFormat.ActionReplay;
									break;
							}
							PScard[listIndex].ExportSingleSave(saveFileDlg.FileName, slotNumber, singleSaveType);
						}
						break;
					case SaveType.DeletedInitial:
						new messageWindow().ShowMessage(this, appName, "Deleted saves cannot be exported. Restore a save to proceed.", "OK", null, true);
						break;

					case SaveType.MiddleLink:
					case SaveType.EndLink:
					case SaveType.DeletedMiddleLink:
					case SaveType.DeletedEndLink:
						new messageWindow().ShowMessage(this, appName, "The selected slot is linked. Select the initial save slot to proceed.", "OK", null, true);
						break;
				}
			}
		}

		//Import a save
		private void importSaveDialog()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var requiredSlots = 0;

				//Check if the slot to import the save on is free
				if (PScard[listIndex].SaveType[slotNumber] == 0)
				{
					var openFileDlg = new OpenFileDialog();
					openFileDlg.Title = "Import save";
					openFileDlg.Filter =
						"All supported|*.mcs;*.psx;*.ps1;*.mcb;*.mcx;*.pda;B???????????*;*.psv|PSXGameEdit single save (*.mcs)|*.mcs|XP, AR, GS, Caetla single save (*.psx)|*.psx|Memory Juggler (*.ps1)|*.ps1|Smart Link (*.mcb)|*.mcb|Datel (*.mcx;*.pda)|*.mcx;*.pda|RAW single save|B???????????*|PS3 virtual save (*.psv)|*.psv";

					//If user selected a card save to it
					if (openFileDlg.ShowDialog() == DialogResult.OK)
					{
						if (PScard[listIndex].ImportSingleSave(openFileDlg.FileName, slotNumber, out requiredSlots))
						{
							refreshListView(listIndex, slotNumber);
						}
						else if (requiredSlots > 0)
						{
							new messageWindow().ShowMessage(this, appName, "To complete this operation " + requiredSlots + " free slots are required.", "OK", null, true);
						}
						else
						{
							new messageWindow().ShowMessage(this, appName, "File could not be opened.", "OK", null, true);
						}
					}
				}
				else
				{
					new messageWindow().ShowMessage(this, appName, "The selected slot is not empty.", "OK", null, true);
				}
			}
		}

		public static string getRegionString(SaveRegion region)
		{
			//todo: ugly hack, need tests and simpler conversion
			var tempRegion = new byte[3];
			var ushortRegion = (ushort)region;
			//Convert region to byte array
			tempRegion[0] = (byte)(ushortRegion & 0xFF);
			tempRegion[1] = (byte)(ushortRegion >> 8);

			//Get UTF-16 string
			return Encoding.Default.GetString(tempRegion);
		}

		//Prompt for save
		private void savePrompt(int listIndex)
		{
			//Check if the file has been changed
			if (PScard[listIndex].ChangedFlag)
			{
				if (new messageWindow().ShowMessage(this, appName, "Do you want to save changes to '" + PScard[listIndex].CardName + "'?", "No", "Yes", true, true) == "Yes")
					saveCardFunction(listIndex);
			}
		}

		//Open preferences window
		private void editPreferences()
		{
			var prefDlg = new preferencesWindow();

			prefDlg.hostWindow = this;
			prefDlg.initializeDialog(mainSettings);

			prefDlg.ShowDialog(this);

			prefDlg.Dispose();
		}

		//Open edit icon dialog
		private void editIcon()
		{
			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var iconFrames = PScard[listIndex].IconFrames[slotNumber];
				var saveTitle = PScard[listIndex].SaveName[slotNumber, mainSettings.titleEncoding];
				var iconBytes = PScard[listIndex].GetIconBytes(slotNumber);

				//Check the save type
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.Initial:
					case SaveType.DeletedInitial:
						var iconDlg = new iconWindow();
						iconDlg.initializeDialog(saveTitle, iconFrames, iconBytes);
						iconDlg.ShowDialog(this);

						//Update data if OK has been pressed
						if (iconDlg.okPressed)
						{
							PScard[listIndex].SetIconBytes(slotNumber, iconDlg.iconData);
							refreshListView(listIndex, slotNumber);
						}

						iconDlg.Dispose();
						break;

					case SaveType.MiddleLink:
					case SaveType.EndLink:
					case SaveType.DeletedMiddleLink:
					case SaveType.DeletedEndLink:
						new messageWindow().ShowMessage(this, appName, "The selected slot is linked. Select the initial save slot to proceed.", "OK", null, true);
						break;
				}
			}
		}

		//Create and show plugins dialog
		private void showPluginsWindow()
		{
			var pluginsDlg = new pluginsWindow();

			pluginsDlg.initializeDialog(this, pluginSystem.assembliesMetadata);
			pluginsDlg.ShowDialog(this);
			pluginsDlg.Dispose();
		}

		//Create and show about dialog
		private void showAbout()
		{
			new AboutWindow().initDialog(this, appName, appVersion, appDate, "Copyright © Shendo 2014",
				"Beta testers: Gamesoul Master, Xtreme2damax,\nCarmax91.\n\nThanks to: @ruantec, Cobalt, TheCloudOfSmoke,\nRedawgTS, Hard core Rikki, RainMotorsports,\nZieg, Bobbi, OuTman, Kevstah2004, Kubusleonidas, \nFrédéric Brière, Cor'e, Gemini, DeadlySystem.\n\n" +
				"Special thanks to the following people whose\nMemory Card utilities inspired me to write my own:\nSimon Mallion (PSXMemTool),\nLars Ole Dybdal (PSXGameEdit),\nAldo Vargas (Memory Card Manager),\nNeill Corlett (Dexter),\nPaul Phoneix (ConvertM).");
		}

		//Make a new ListView control
		private void makeListView()
		{
			//Add a new ImageList to hold the card icons
			iconList.Add(new ImageList());
			iconList[iconList.Count - 1].ImageSize = new Size(48, 16);
			iconList[iconList.Count - 1].ColorDepth = ColorDepth.Depth32Bit;
			iconList[iconList.Count - 1].TransparentColor = Color.Magenta;

			cardList.Add(new ListView());
			cardList[cardList.Count - 1].Location = new Point(0, 3);
			cardList[cardList.Count - 1].Size = new Size(492, 286);
			cardList[cardList.Count - 1].SmallImageList = iconList[iconList.Count - 1];
			cardList[cardList.Count - 1].ContextMenuStrip = mainContextMenu;
			cardList[cardList.Count - 1].FullRowSelect = true;
			cardList[cardList.Count - 1].MultiSelect = false;
			cardList[cardList.Count - 1].HeaderStyle = ColumnHeaderStyle.Nonclickable;
			cardList[cardList.Count - 1].HideSelection = false;
			cardList[cardList.Count - 1].Columns.Add("Icon, region and title");
			cardList[cardList.Count - 1].Columns.Add("Product code");
			cardList[cardList.Count - 1].Columns.Add("Identifier");
			cardList[cardList.Count - 1].Columns[0].Width = 315;
			cardList[cardList.Count - 1].Columns[1].Width = 87;
			cardList[cardList.Count - 1].Columns[2].Width = 84;
			cardList[cardList.Count - 1].View = View.Details;
			cardList[cardList.Count - 1].DoubleClick += cardList_DoubleClick;
			cardList[cardList.Count - 1].SelectedIndexChanged += cardList_IndexChanged;

			refreshListView(cardList.Count - 1, 0);
		}

		//Refresh the ListView
		private void refreshListView(int listIndex, int slotNumber)
		{
			//Temporary FontFamily
			FontFamily tempFontFamily = null;

			//Place cardName on the tab
			mainTabControl.TabPages[listIndex].Text = PScard[listIndex].CardName;

			//Remove all icons from the list
			iconList[listIndex].Images.Clear();

			//Remove all items from the list
			cardList[listIndex].Items.Clear();

			//Add linked slot icons to iconList
			iconList[listIndex].Images.Add(Resources.linked);
			iconList[listIndex].Images.Add(Resources.linked_disabled);

			//Add 15 List items along with icons
			for (var i = 0; i < 15; i++)
			{
				//Add save icons to the list
				iconList[listIndex].Images.Add(prepareIcons(listIndex, i));

				switch (PScard[listIndex].SaveType[i])
				{
					default: //Corrupted
						cardList[listIndex].Items.Add("Corrupted slot");
						break;

					case SaveType.Formatted:
						cardList[listIndex].Items.Add("Free slot");
						break;

					case SaveType.Initial:
					case SaveType.DeletedInitial:
						cardList[listIndex].Items.Add(PScard[listIndex].SaveName[i, mainSettings.titleEncoding]);
						cardList[listIndex].Items[i].SubItems.Add(PScard[listIndex].SaveProductCode[i]);
						cardList[listIndex].Items[i].SubItems.Add(PScard[listIndex].SaveIdentifier[i]);
						cardList[listIndex].Items[i].ImageIndex = i + 2; //Skip two linked slot icons
						break;

					case SaveType.MiddleLink:
						cardList[listIndex].Items.Add("Linked slot (middle link)");
						cardList[listIndex].Items[i].ImageIndex = 0;
						break;

					case SaveType.DeletedMiddleLink:
						cardList[listIndex].Items.Add("Linked slot (middle link)");
						cardList[listIndex].Items[i].ImageIndex = 1;
						break;

					case SaveType.EndLink:
						cardList[listIndex].Items.Add("Linked slot (end link)");
						cardList[listIndex].Items[i].ImageIndex = 0;
						break;

					case SaveType.DeletedEndLink:
						cardList[listIndex].Items.Add("Linked slot (end link)");
						cardList[listIndex].Items[i].ImageIndex = 1;
						break;
				}
			}

			//Select the active item in the list
			cardList[listIndex].Items[slotNumber].Selected = true;

			//Set font for the list
			if (mainSettings.listFont != null)
			{
				//Create FontFamily from font name
				tempFontFamily = new FontFamily(mainSettings.listFont);

				//Check if regular style is supported
				if (tempFontFamily.IsStyleAvailable(FontStyle.Regular))
				{
					//Use custom font
					cardList[listIndex].Font = new Font(mainSettings.listFont, 8.25f);
				}
				else
				{
					//Use default font
					mainSettings.listFont = FontFamily.GenericSansSerif.Name;
					cardList[listIndex].Font = new Font(mainSettings.listFont, 8.25f);
				}
			}

			//Set showListGrid option
			if (mainSettings.showListGrid == 0) cardList[listIndex].GridLines = false;
			else cardList[listIndex].GridLines = true;

			refreshPluginBindings();

			//Enable certain list items
			enableSelectiveEditItems();
		}

		//Prepare icons for drawing (add flags and make them transparent if save is deleted)
		private Bitmap prepareIcons(int listIndex, int slotNumber)
		{
			var iconBitmap = new Bitmap(48, 16);
			var iconGraphics = Graphics.FromImage(iconBitmap);

			//Check what background color should be set
			switch (mainSettings.iconBackgroundColor)
			{
				case 1: //Black
					iconGraphics.FillRegion(new SolidBrush(Color.Black), new Region(new Rectangle(0, 0, 16, 16)));
					break;

				case 2: //Gray
					iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x30, 0x30, 0x30)), new Region(new Rectangle(0, 0, 16, 16)));
					break;

				case 3: //Blue
					iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x44, 0x44, 0x98)), new Region(new Rectangle(0, 0, 16, 16)));
					break;
			}

			//Draw icon
			iconGraphics.DrawImage(PScard[listIndex].IconData[slotNumber, 0], 0, 0, 16, 16);

			//Draw flag depending of the region
			switch (PScard[listIndex].SaveRegion[slotNumber])
			{
				default: //Formatted save, Corrupted save, Unknown region
					iconGraphics.DrawImage(Resources.naflag, 17, 0, 30, 16);
					break;

				case SaveRegion.US:
					iconGraphics.DrawImage(Resources.amflag, 17, 0, 30, 16);
					break;

				case SaveRegion.EU:
					iconGraphics.DrawImage(Resources.euflag, 17, 0, 30, 16);
					break;

				case SaveRegion.JP:
					iconGraphics.DrawImage(Resources.jpflag, 17, 0, 30, 16);
					break;
			}

			//Check if save is deleted and color the icon
			if (PScard[listIndex].SaveType[slotNumber] == SaveType.DeletedInitial)
				iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF)), new Region(new Rectangle(0, 0, 16, 16)));

			iconGraphics.Dispose();

			return iconBitmap;
		}

		//Refresh the toolstrip
		private void refreshStatusStrip()
		{
			//Show the location of the active card in the tool strip (if there are any cards)
			if (PScard.Count > 0)
				toolString.Text = PScard[mainTabControl.SelectedIndex].CardLocation;
			else
				toolString.Text = null;

			//If glass is enabled repaint the form
			if (windowGlass.isGlassSupported() && mainSettings.glassStatusBar == 1) Refresh();
		}

		//Save work and close the application
		private void exitApplication()
		{
			//Close every opened card
			closeAllCards();

			//Save settings
			saveProgramSettings();
		}

		//Edit header of the selected save
		private void editSaveHeader()
		{
			//Check if there are any cards to edit
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var saveRegion = PScard[listIndex].SaveRegion[slotNumber];
				var saveProdCode = PScard[listIndex].SaveProductCode[slotNumber];
				var saveIdentifier = PScard[listIndex].SaveIdentifier[slotNumber];
				var saveTitle = PScard[listIndex].SaveName[slotNumber, mainSettings.titleEncoding];

				//Check if slot is allowed to be edited
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.Initial:
					case SaveType.DeletedInitial:
						var headerDlg = new headerWindow();

						//Load values to dialog
						headerDlg.initializeDialog(appName, saveTitle, saveProdCode, saveIdentifier, saveRegion);
						headerDlg.ShowDialog(this);

						//Update values if OK was pressed
						if (headerDlg.okPressed)
						{
							//Insert data to save header of the selected card and slot
							PScard[listIndex].SetHeaderData(slotNumber, headerDlg.prodCode, headerDlg.saveIdentifier, headerDlg.saveRegion);
							refreshListView(listIndex, slotNumber);
						}
						headerDlg.Dispose();
						break;
				}
			}
		}

		//Open a card if it's given by command line
		private bool loadCommandLine()
		{
			if (Environment.GetCommandLineArgs().Length > 1)
			{
				openCard(Environment.GetCommandLineArgs()[1]);
				return true;
			}
			return false;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			//Show name of the application on the mainWindow
			Text = appName + " " + appVersion;

			//Set default settings
			mainSettings.titleEncoding = 0;
			mainSettings.listFont = FontFamily.GenericSansSerif.Name;
			mainSettings.showListGrid = 0;
			mainSettings.iconInterpolationMode = 0;
			mainSettings.iconPropertiesSize = 1;
			mainSettings.backupMemcards = 0;
			mainSettings.warningMessage = 1;
			mainSettings.restoreWindowPosition = 0;
			mainSettings.communicationPort = "COM1";
			mainSettings.formatType = 0;
			mainSettings.glassStatusBar = 1;

			//Load settings from Settings.ini
			loadProgramSettings();

			//Load available plugins
			pluginSystem.fetchPlugins(appPath + "/Plugins");

			//Create an empty card upon startup or load one given by the command line
			if (loadCommandLine() == false) openCard(null);

			//Apply Aero glass effect to the window
			applyGlass();
		}

		private void managePluginsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Show the plugins dialog
			showPluginsWindow();
		}

		//Refresh plugin menu
		private void refreshPluginBindings()
		{
			//Clear the menus
			editWithPluginToolStripMenuItem.DropDownItems.Clear();
			editWithPluginToolStripMenuItem.Enabled = false;

			editWithPluginToolStripMenuItem1.DropDownItems.Clear();
			editWithPluginToolStripMenuItem1.Enabled = false;

			//Check if there are any cards
			if (cardList.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if any item on the list is selected
				if (cardList[listIndex].SelectedItems.Count > 0)
				{
					var slotNumber = cardList[listIndex].SelectedIndices[0];

					//Check the save type
					switch (PScard[listIndex].SaveType[slotNumber])
					{
						case SaveType.Initial:
						case SaveType.DeletedInitial:

							//Get the supported plugins
							supportedPlugins = pluginSystem.getSupportedPlugins(PScard[listIndex].SaveProductCode[slotNumber]);

							//Check if there are any plugins that support the product code
							if (supportedPlugins != null)
							{
								//Enable plugin menu
								editWithPluginToolStripMenuItem.Enabled = true;
								editWithPluginToolStripMenuItem1.Enabled = true;

								foreach (var currentAssembly in supportedPlugins)
								{
									//Add item to the plugin menu
									editWithPluginToolStripMenuItem.DropDownItems.Add(pluginSystem.assembliesMetadata[currentAssembly].pluginName);
									editWithPluginToolStripMenuItem1.DropDownItems.Add(pluginSystem.assembliesMetadata[currentAssembly].pluginName);
								}
							}
							break;
					}
				}
			}
		}

		//Edit a selected save with a selected plugin
		private void editWithPlugin(int pluginIndex)
		{
			//Check if there are any cards to edit
			if (PScard.Count > 0)
			{
				//Show backup warning message
				if (mainSettings.warningMessage == 1)
				{
					if (new messageWindow().ShowMessage(this, appName, "Save editing may potentialy corrupt the save.\nDo you want to proceed with this operation?", "No", "Yes", true) == "No") return;
				}

				var listIndex = mainTabControl.SelectedIndex;
				var slotNumber = cardList[listIndex].SelectedIndices[0];
				var reqSlots = 0;
				var editedSaveBytes = pluginSystem.editSaveData(supportedPlugins[pluginIndex], PScard[listIndex].GetSaveBytes(slotNumber), PScard[listIndex].SaveProductCode[slotNumber]);

				if (editedSaveBytes != null)
				{
					//Delete save so the edited one can be placed in.
					PScard[listIndex].FormatSave(slotNumber);

					PScard[listIndex].SetSaveBytes(slotNumber, editedSaveBytes, out reqSlots);

					//Refresh the list with new data
					refreshListView(listIndex, slotNumber);

					//Set the edited flag of the card
					PScard[listIndex].ChangedFlag = true;
				}
			}
		}

		private void readMeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Check if Readme.txt exists
			if (File.Exists(appPath + "/Readme.txt")) Process.Start(appPath + "/Readme.txt");
			else new messageWindow().ShowMessage(this, appName, "'ReadMe.txt' was not found.", "OK", null, true);
		}


		//Disable all items related to save editing
		private void disableEditItems()
		{
			//Edit menu
			editSaveHeaderToolStripMenuItem.Enabled = false;
			editSaveCommentToolStripMenuItem.Enabled = false;
			compareWithTempBufferToolStripMenuItem.Enabled = false;
			editIconToolStripMenuItem.Enabled = false;
			deleteSaveToolStripMenuItem.Enabled = false;
			restoreSaveToolStripMenuItem.Enabled = false;
			removeSaveformatSlotsToolStripMenuItem.Enabled = false;
			copySaveToTempraryBufferToolStripMenuItem.Enabled = false;
			pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = false;
			importSaveToolStripMenuItem.Enabled = false;
			exportSaveToolStripMenuItem.Enabled = false;

			//Edit toolbar
			editHeaderButton.Enabled = false;
			commentsButton.Enabled = false;
			editIconButton.Enabled = false;
			importButton.Enabled = false;
			exportButton.Enabled = false;

			//Edit popup
			editSaveHeaderToolStripMenuItem1.Enabled = false;
			editSaveCommentsToolStripMenuItem.Enabled = false;
			compareWithTempBufferToolStripMenuItem1.Enabled = false;
			editIconToolStripMenuItem1.Enabled = false;
			deleteSaveToolStripMenuItem1.Enabled = false;
			restoreSaveToolStripMenuItem1.Enabled = false;
			removeSaveformatSlotsToolStripMenuItem1.Enabled = false;
			copySaveToTempBufferToolStripMenuItem.Enabled = false;
			paseToolStripMenuItem.Enabled = false;
			importSaveToolStripMenuItem1.Enabled = false;
			exportSaveToolStripMenuItem1.Enabled = false;
			saveInformationToolStripMenuItem.Enabled = false;
		}

		//Enable all items related to save editing
		private void enableEditItems()
		{
			//Edit menu
			editSaveHeaderToolStripMenuItem.Enabled = true;
			editSaveCommentToolStripMenuItem.Enabled = true;
			editIconToolStripMenuItem.Enabled = true;
			deleteSaveToolStripMenuItem.Enabled = true;
			restoreSaveToolStripMenuItem.Enabled = true;
			removeSaveformatSlotsToolStripMenuItem.Enabled = true;
			copySaveToTempraryBufferToolStripMenuItem.Enabled = true;
			pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = true;
			importSaveToolStripMenuItem.Enabled = true;
			exportSaveToolStripMenuItem.Enabled = true;

			//Edit toolbar
			editHeaderButton.Enabled = true;
			commentsButton.Enabled = true;
			editIconButton.Enabled = true;
			importButton.Enabled = true;
			exportButton.Enabled = true;

			//Edit popup
			editSaveHeaderToolStripMenuItem1.Enabled = true;
			editSaveCommentsToolStripMenuItem.Enabled = true;
			editIconToolStripMenuItem1.Enabled = true;
			deleteSaveToolStripMenuItem1.Enabled = true;
			restoreSaveToolStripMenuItem1.Enabled = true;
			removeSaveformatSlotsToolStripMenuItem1.Enabled = true;
			copySaveToTempBufferToolStripMenuItem.Enabled = true;
			paseToolStripMenuItem.Enabled = true;
			importSaveToolStripMenuItem1.Enabled = true;
			exportSaveToolStripMenuItem1.Enabled = true;
			saveInformationToolStripMenuItem.Enabled = true;

			//Temp buffer related
			if (tempBuffer != null)
			{
				compareWithTempBufferToolStripMenuItem.Enabled = true;
				compareWithTempBufferToolStripMenuItem1.Enabled = true;
			}
			else
			{
				compareWithTempBufferToolStripMenuItem.Enabled = false;
				compareWithTempBufferToolStripMenuItem1.Enabled = false;
			}
		}

		//Enable only supported edit operations
		private void enableSelectiveEditItems()
		{
			//Check if there are any cards
			if (cardList.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if any item on the list is selected
				if (cardList[listIndex].SelectedItems.Count > 0)
				{
					var slotNumber = cardList[listIndex].SelectedIndices[0];
					switch (PScard[listIndex].SaveType[slotNumber])
					{
						case SaveType.Formatted:
							disableEditItems();
							pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = true;
							paseToolStripMenuItem.Enabled = true;
							importSaveToolStripMenuItem.Enabled = true;
							importSaveToolStripMenuItem1.Enabled = true;
							importButton.Enabled = true;
							break;

						case SaveType.Initial:
							enableEditItems();
							restoreSaveToolStripMenuItem.Enabled = false;
							restoreSaveToolStripMenuItem1.Enabled = false;
							pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = false;
							paseToolStripMenuItem.Enabled = false;
							importSaveToolStripMenuItem.Enabled = false;
							importSaveToolStripMenuItem1.Enabled = false;
							importButton.Enabled = false;
							break;

						case SaveType.MiddleLink:
						case SaveType.EndLink:
							disableEditItems();
							break;

						case SaveType.DeletedInitial:
							enableEditItems();
							deleteSaveToolStripMenuItem.Enabled = false;
							deleteSaveToolStripMenuItem1.Enabled = false;
							pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = false;
							paseToolStripMenuItem.Enabled = false;
							importSaveToolStripMenuItem.Enabled = false;
							importSaveToolStripMenuItem1.Enabled = false;
							importButton.Enabled = false;
							exportSaveToolStripMenuItem.Enabled = false;
							exportSaveToolStripMenuItem1.Enabled = false;
							exportButton.Enabled = false;
							break;

						case SaveType.DeletedMiddleLink:
						case SaveType.DeletedEndLink:
							disableEditItems();
							break;

						default:
							disableEditItems();
							removeSaveformatSlotsToolStripMenuItem.Enabled = true;
							removeSaveformatSlotsToolStripMenuItem1.Enabled = true;
							break;
					}
				}
				else
				{
					//No save is selected, disable all items
					disableEditItems();
				}
			}
			else
			{
				//There is no card, disable all items
				disableEditItems();
			}
		}

		//Compare currently selected save with the temp buffer
		private void compareSaveWithTemp()
		{
			//Save data to work with
			byte[] fetchedData = null;
			string fetchedDataTitle = null;

			//Check if temp buffer contains anything
			if (tempBuffer == null)
			{
				new messageWindow().ShowMessage(this, appName, "Temp buffer is empty. Save can't be compared.", "OK", null, true);
				return;
			}

			//Check if there are any cards available
			if (PScard.Count > 0)
			{
				var listIndex = mainTabControl.SelectedIndex;

				//Check if a save is selected
				if (cardList[listIndex].SelectedIndices.Count == 0) return;

				var slotNumber = cardList[listIndex].SelectedIndices[0];

				//Check the save type
				switch (PScard[listIndex].SaveType[slotNumber])
				{
					case SaveType.Initial:
					case SaveType.DeletedInitial:

						//Get data to work with
						fetchedData = PScard[listIndex].GetSaveBytes(slotNumber);
						fetchedDataTitle = PScard[listIndex].SaveName[slotNumber, mainSettings.titleEncoding];

						//Check if selected saves have the same size
						if (fetchedData.Length != tempBuffer.Length)
						{
							new messageWindow().ShowMessage(this, appName, "Save file size mismatch. Saves can't be compared.", "OK", null, true);
							return;
						}

						//Show compare window
						new compareWindow().initializeDialog(this, appName, fetchedData, fetchedDataTitle, tempBuffer, tempBufferName + " (temp buffer)");

						break;

					case SaveType.MiddleLink:
					case SaveType.EndLink:
					case SaveType.DeletedMiddleLink:
					case SaveType.DeletedEndLink:
						new messageWindow().ShowMessage(this, appName, "The selected slot is linked. Select the initial save slot to proceed.", "OK", null, true);
						break;
				}
			}
		}

		//Read a Memory Card from the physical device
		private void cardReaderRead(byte[] readData)
		{
			//Check if the Memory Card was sucessfully read
			if (readData != null)
			{
				//Create a new card
				PScard.Add(new PsOneCard());

				//Fill the card with the new data
				PScard[PScard.Count - 1].LoadMemoryCard(readData);

				//Temporary set a bogus file location (to fool filterNullCard function)
				PScard[PScard.Count - 1].CardLocation = "\0";

				//Create a tab page for the new card
				createTabPage();

				//Restore null location since DexDrive Memory Card is not a file present on the Hard Disk
				PScard[PScard.Count - 1].CardLocation = null;
			}
		}


		private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			//Show the location of the active card in the tool strip
			refreshStatusStrip();

			//Load available plugins for the selected save
			refreshPluginBindings();

			//Enable certain list items
			enableSelectiveEditItems();
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Show about dialog
			showAbout();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Show browse dialog and open a selected Memory Card
			openCardDialog();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Close the application
			//Application.Exit();
			Close();
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Create a new card by giving a null path
			openCard(null);
		}

		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Close the selected card
			closeCard(mainTabControl.SelectedIndex);
		}

		private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Close all opened cards
			closeAllCards();
		}

		private void editSaveHeaderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Edit header of the selected save
			editSaveHeader();
		}

		private void editSaveHeaderToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Edit header of the selected save
			editSaveHeader();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Save a Memory Card as...
			saveCardDialog(mainTabControl.SelectedIndex);
		}

		private void newButton_Click(object sender, EventArgs e)
		{
			//Create a new card by giving a null path
			openCard(null);
		}

		private void openButton_Click(object sender, EventArgs e)
		{
			//Show browse dialog and open a selected Memory Card
			openCardDialog();
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			//Save a Memory Card
			saveCardFunction(mainTabControl.SelectedIndex);
		}

		private void editSaveCommentToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Edit save comment of the selected slot
			editSaveComments();
		}

		private void editSaveCommentsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Edit save comment of the selected slot
			editSaveComments();
		}

		private void commentsButton_Click(object sender, EventArgs e)
		{
			//Edit save comment of the selected slot
			editSaveComments();
		}

		private void cardList_DoubleClick(object sender, EventArgs e)
		{
			//Show information about the selected save
			showInformation();
		}

		private void cardList_IndexChanged(object sender, EventArgs e)
		{
			//Load appropriate plugins for the selected save
			refreshPluginBindings();

			//Enable certain list items
			enableSelectiveEditItems();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Save Memory Card
			saveCardFunction(mainTabControl.SelectedIndex);
		}

		private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Open preferences dialog
			editPreferences();
		}

		private void mainWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			//Cleanly close the application
			exitApplication();
		}

		private void deleteSaveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Delete selected save
			deleteSave();
		}

		private void deleteSaveToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Delete selected save
			deleteSave();
		}

		private void restoreSaveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Restore deleted save
			restoreSave();
		}

		private void restoreSaveToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Restore deleted save
			restoreSave();
		}

		private void editHeaderButton_Click(object sender, EventArgs e)
		{
			//Edit header of the selected save
			editSaveHeader();
		}

		private void editIconButton_Click(object sender, EventArgs e)
		{
			//Edit save icon
			editIcon();
		}

		private void removeSaveformatSlotsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Format selected save
			formatSave();
		}

		private void removeSaveformatSlotsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Format selected save
			formatSave();
		}

		private void editIconToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Edit icon of the selected save
			editIcon();
		}

		private void editIconToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Edit icon of the selected save
			editIcon();
		}

		private void copySaveToTempraryBufferToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Store selected save to temp buffer
			copySave();
		}

		private void copySaveToTempBufferToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Store selected save to temp buffer
			copySave();
		}

		private void exportSaveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Export a save
			exportSaveDialog();
		}

		private void exportSaveToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Export a save
			exportSaveDialog();
		}

		private void exportButton_Click(object sender, EventArgs e)
		{
			//Export a save
			exportSaveDialog();
		}

		private void importSaveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Import a save
			importSaveDialog();
		}

		private void importSaveToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Import a save
			importSaveDialog();
		}

		private void importButton_Click(object sender, EventArgs e)
		{
			//Import a save
			importSaveDialog();
		}

		private void pasteSaveFromTemporaryBufferToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Paste a save from temp buffer to selected slot
			pasteSave();
		}

		private void paseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Paste a save from temp buffer to selected slot
			pasteSave();
		}

		private void tBufToolButton_Click(object sender, EventArgs e)
		{
			//Paste a save from temp buffer to selected slot
			pasteSave();
		}

		private void saveInformationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Show information about a selected save
			showInformation();
		}

		private void mainTabControl_DragEnter(object sender, DragEventArgs e)
		{
			//Check if dragged data are files
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.All;
		}

		private void editWithPluginToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			//Set the click item
			clickedPlugin[1] = e.ClickedItem.Owner.Items.IndexOf(e.ClickedItem);

			//Set the clicked flag
			clickedPlugin[0] = 1;
		}

		private void editWithPluginToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
		{
			//Load clicked plugin if the menu was clicked
			if (clickedPlugin[0] == 1) editWithPlugin(clickedPlugin[1]);

			//Set the clicked flag on false
			clickedPlugin[0] = 0;
		}

		private void mainTabControl_MouseDown(object sender, MouseEventArgs e)
		{
			//Check if the middle mouse button is pressed
			if (e.Button == MouseButtons.Middle)
			{
				Rectangle tabRectangle;

				//Cycle through all available tabs
				for (var i = 0; i < mainTabControl.TabCount; i++)
				{
					tabRectangle = mainTabControl.GetTabRect(i);

					//Close the middle clicked tab
					if (tabRectangle.Contains(e.X, e.Y)) closeCard(i, false);
				}
			}
		}

		private void mainTabControl_DragDrop(object sender, DragEventArgs e)
		{
			var droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

			//Cycle through every dropped file
			foreach (var fileName in droppedFiles)
			{
				openCard(fileName);
			}
		}

		private void mainWindow_Paint(object sender, PaintEventArgs e)
		{
			if (windowGlass.isGlassSupported() && mainSettings.glassStatusBar == 1)
			{
				Brush blackBrush = new SolidBrush(Color.Black);
				var windowGraphics = e.Graphics;

				//Create a black rectangle for Aero glass
				windowGraphics.FillRectangle(blackBrush, windowRectangle);

				//Show path of the currently active card
				windowGlass.DrawTextOnGlass(Handle, toolString.Text, new Font("Segoe UI", 9f, FontStyle.Regular), windowRectangle, 10);
			}
		}

		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);

			//DWM composition is changed and glass support should be revaluated
			if (message.Msg == glassSupport.WM_DWMCOMPOSITIONCHANGED)
			{
				applyGlass();
			}

			//Move the window if user clicked on the glass area
			if (message.Msg == glassSupport.WM_NCHITTEST && message.Result.ToInt32() == glassSupport.HTCLIENT)
			{
				//Check if DWM composition is enabled
				if (windowGlass.isGlassSupported() && mainSettings.glassStatusBar == 1)
				{
					var mouseX = (message.LParam.ToInt32() & 0xFFFF);
					var mouseY = (message.LParam.ToInt32() >> 16);
					var windowPoint = PointToClient(new Point(mouseX, mouseY));

					//Check if the clicked area is on glass
					if (windowRectangle.Contains(windowPoint)) message.Result = new IntPtr(glassSupport.HTCAPTION);
				}
			}
		}

		private void dexDriveMenuRead_Click(object sender, EventArgs e)
		{
			//Read a Memory Card from DexDrive
			var tempByteArray = new cardReaderWindow().readMemoryCardDexDrive(this, appName, mainSettings.communicationPort);

			cardReaderRead(tempByteArray);
		}

		private void dexDriveMenuWrite_Click(object sender, EventArgs e)
		{
			//Write a Memory Card to DexDrive
			var listIndex = mainTabControl.SelectedIndex;

			//Check if there are any cards to write
			if (PScard.Count > 0)
			{
				//Open a DexDrive communication window
				new cardReaderWindow().writeMemoryCardDexDrive(this, appName, mainSettings.communicationPort, PScard[listIndex].GetRawMemoryCard(), 1024);
			}
		}

		private void compareWithTempBufferToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Compare selected save to a temp buffer save
			compareSaveWithTemp();
		}

		private void compareWithTempBufferToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			//Compare selected save to a temp buffer save
			compareSaveWithTemp();
		}

		private void memCARDuinoMenuRead_Click(object sender, EventArgs e)
		{
			//Read a Memory Card from MemCARDuino
			var tempByteArray = new cardReaderWindow().readMemoryCardCARDuino(this, appName, mainSettings.communicationPort);

			cardReaderRead(tempByteArray);
		}

		private void memCARDuinoMenuWrite_Click(object sender, EventArgs e)
		{
			//Write a Memory Card to MemCARDuino
			var listIndex = mainTabControl.SelectedIndex;

			//Check if there are any cards to write
			if (PScard.Count > 0)
			{
				//Open a DexDrive communication window
				new cardReaderWindow().writeMemoryCardCARDuino(this, appName, mainSettings.communicationPort, PScard[listIndex].GetRawMemoryCard(), 1024);
			}
		}

		private void pS1CardLinkMenuRead_Click(object sender, EventArgs e)
		{
			//Read a Memory Card from PS1CardLink
			var tempByteArray = new cardReaderWindow().readMemoryCardPS1CLnk(this, appName, mainSettings.communicationPort);

			cardReaderRead(tempByteArray);
		}

		//Write a Memory Card to PS1CardLink
		private void pS1CardLinkMenuWrite_Click(object sender, EventArgs e)
		{
			var listIndex = mainTabControl.SelectedIndex;

			//Check if there are any cards to write
			if (PScard.Count > 0)
			{
				//Open a DexDrive communication window
				new cardReaderWindow().writeMemoryCardPS1CLnk(this, appName, mainSettings.communicationPort, PScard[listIndex].GetRawMemoryCard(), 1024);
			}
		}

		private void dexDriveMenuFormat_Click(object sender, EventArgs e)
		{
			//Format a Memory Card on DexDrive
			formatHardwareCard(0);
		}

		private void memCARDuinoMenuFormat_Click(object sender, EventArgs e)
		{
			//Format a Memory Card on MemCARDuino
			formatHardwareCard(1);
		}

		private void pS1CardLinkMenuFormat_Click(object sender, EventArgs e)
		{
			//Format a Memory Card on PS1CardLink
			formatHardwareCard(2);
		}

		//Format a Memory Card on the hardware interface (0 - DexDrive, 1 - MemCARDuino, 2 - PS1CardLink)
		private void formatHardwareCard(int hardDevice)
		{
			//Show warning message
			if (new messageWindow().ShowMessage(this, appName, "Formatting will delete all saves on the Memory Card.\nDo you want to proceed with this operation?", "No", "Yes", true) == "No") return;

			var frameNumber = 1024;
			var blankCard = new PsOneCard();

			//Check if quick format option is turned on
			if (mainSettings.formatType == 0) frameNumber = 64;

			//Create a new card by giving a null path
			blankCard.Format();

			//Check what device to use
			switch (hardDevice)
			{
				case 0: //DexDrive
					new cardReaderWindow().writeMemoryCardDexDrive(this, appName, mainSettings.communicationPort, blankCard.GetRawMemoryCard(), frameNumber);
					break;

				case 1: //MemCARDuino
					new cardReaderWindow().writeMemoryCardCARDuino(this, appName, mainSettings.communicationPort, blankCard.GetRawMemoryCard(), frameNumber);
					break;

				case 2: //PS1CardLink
					new cardReaderWindow().writeMemoryCardPS1CLnk(this, appName, mainSettings.communicationPort, blankCard.GetRawMemoryCard(), frameNumber);
					break;
			}
		}
	}
}