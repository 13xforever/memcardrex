using System;
using System.Windows.Forms;
using MemcardRex.Enums;
using MemcardRex.Enums.MemoryCard;

namespace MemcardRex
{
	internal partial class headerWindow : Form
	{
		//Name of the host application
		private string appName;
		//Custom save region (If the save uses nonstandard region)
		private ushort customSaveRegion;
		//If OK is pressed this value will be true
		public bool okPressed;
		//Save header data
		public string prodCode;
		public string saveIdentifier;
		public SaveRegion saveRegion;
		public headerWindow() { InitializeComponent(); }
		private void headerWindow_Load(object sender, EventArgs e) { }
		//Initialize dialog by loading provided values
		public void initializeDialog(string applicationName, string dialogTitle, string prodCode, string identifier, SaveRegion region)
		{
			appName = applicationName;
			Text = dialogTitle;
			prodCodeTextbox.Text = prodCode;
			identifierTextbox.Text = identifier;

			//Check what region is selected
			switch (region)
			{
				case SaveRegion.US: //America
					regionCombobox.SelectedIndex = 0;
					break;

				case SaveRegion.EU: //Europe
					regionCombobox.SelectedIndex = 1;
					break;

				case SaveRegion.JP: //Japan
					regionCombobox.SelectedIndex = 2;
					break;

				default: //Region custom, show hex
					customSaveRegion = (ushort)region;
					regionCombobox.Items.Add("0x" + region.ToString("X4"));
					regionCombobox.SelectedIndex = 3;
					break;
			}

			//A fix for selected all behaviour
			prodCodeTextbox.Select(prodCodeTextbox.Text.Length, 0);
			identifierTextbox.Select(identifierTextbox.Text.Length, 0);
		}

		private void cancelButton_Click(object sender, EventArgs e) { Close(); }

		private void okButton_Click(object sender, EventArgs e)
		{
			//Check if values are valid to be submitted
			if (prodCodeTextbox.Text.Length < 10 && identifierTextbox.Text.Length != 0)
			{
				//String is not valid
				new messageWindow().ShowMessage(this, appName, "Product code must be exactly 10 characters long.", "OK", null, true);
			}
			else
			{
				//String is valid
				prodCode = prodCodeTextbox.Text;
				saveIdentifier = identifierTextbox.Text;

				//Set the save region
				switch (regionCombobox.SelectedIndex)
				{
					default:
						saveRegion = (SaveRegion)customSaveRegion;
						break;

					case 0:
						saveRegion = SaveRegion.US;
						break;

					case 1:
						saveRegion = SaveRegion.EU;
						break;

					case 2:
						saveRegion = SaveRegion.JP;
						break;
				}

				//OK is pressed
				okPressed = true;
				Close();
			}
		}
	}
}