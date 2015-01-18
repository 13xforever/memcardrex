using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace MemcardRex
{
	public partial class preferencesWindow : Form
	{
		//Window that hosted this dialog
		public mainWindow hostWindow;
		//Saved DexDrive COM port (in case it's a removable USB adapter)
		private string SavedComPort;
		public preferencesWindow() { InitializeComponent(); }
		//Load default values
		public void initializeDialog(mainWindow.programSettings progSettings)
		{
			encodingCombo.SelectedIndex = progSettings.titleEncoding;
			interpolationCombo.SelectedIndex = progSettings.iconInterpolationMode;
			iconSizeCombo.SelectedIndex = progSettings.iconPropertiesSize;
			backgroundCombo.SelectedIndex = progSettings.iconBackgroundColor;
			formatCombo.SelectedIndex = progSettings.formatType;
			SavedComPort = progSettings.communicationPort;
			if (progSettings.showListGrid == 1) gridCheckbox.Checked = true;
			else gridCheckbox.Checked = false;
			if (progSettings.backupMemcards == 1) backupCheckbox.Checked = true;
			else backupCheckbox.Checked = false;
			if (progSettings.glassStatusBar == 1) glassCheckbox.Checked = true;
			else glassCheckbox.Checked = false;
			if (progSettings.warningMessage == 1) backupWarningCheckBox.Checked = true;
			else backupWarningCheckBox.Checked = false;
			if (progSettings.restoreWindowPosition == 1) restorePositionCheckbox.Checked = true;
			else restorePositionCheckbox.Checked = false;

			//Load all COM ports found on the system
			foreach (var port in SerialPort.GetPortNames())
			{
				dexDriveCombo.Items.Add(port);
			}

			//If there are no ports disable combobox
			if (dexDriveCombo.Items.Count < 1) dexDriveCombo.Enabled = false;

			//Select a com port (if it exists)
			dexDriveCombo.SelectedItem = progSettings.communicationPort;

			//Load all fonts installed on system
			foreach (var font in FontFamily.Families)
			{
				//Add the font on the list
				fontCombo.Items.Add(font.Name);
			}

			//Find font used in the save list
			fontCombo.SelectedItem = progSettings.listFont;
		}

		//Apply configured settings
		private void applySettings()
		{
			var progSettings = new mainWindow.programSettings();

			progSettings.titleEncoding = encodingCombo.SelectedIndex;
			progSettings.iconInterpolationMode = interpolationCombo.SelectedIndex;
			progSettings.iconPropertiesSize = iconSizeCombo.SelectedIndex;
			progSettings.iconBackgroundColor = backgroundCombo.SelectedIndex;
			progSettings.formatType = formatCombo.SelectedIndex;
			progSettings.communicationPort = SavedComPort;

			if (gridCheckbox.Checked) progSettings.showListGrid = 1;
			else progSettings.showListGrid = 0;
			if (backupCheckbox.Checked) progSettings.backupMemcards = 1;
			else progSettings.backupMemcards = 0;
			if (glassCheckbox.Checked) progSettings.glassStatusBar = 1;
			else progSettings.glassStatusBar = 0;
			if (backupWarningCheckBox.Checked) progSettings.warningMessage = 1;
			else progSettings.warningMessage = 0;
			if (restorePositionCheckbox.Checked) progSettings.restoreWindowPosition = 1;
			else progSettings.restoreWindowPosition = 0;
			if (fontCombo.SelectedIndex != -1) progSettings.listFont = fontCombo.SelectedItem.ToString();

			hostWindow.applyProgramSettings(progSettings);
		}

		private void cancelButton_Click(object sender, EventArgs e) { Close(); }

		private void okButton_Click(object sender, EventArgs e)
		{
			applySettings();
			Close();
		}

		private void applyButton_Click(object sender, EventArgs e) { applySettings(); }

		private void dexDriveCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			//Save the COM port if the user selected a new one
			SavedComPort = dexDriveCombo.Text;
		}
	}
}