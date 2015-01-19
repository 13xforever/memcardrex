using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MemcardRex.Enums;
using MemcardRex.Enums.MemoryCard;

namespace MemcardRex
{
	public partial class informationWindow : Form
	{
		private int iconBackColor;
		//Save icons
		private Bitmap[] iconData;
		private int iconIndex;
		private int iconInterpolationMode;
		private int iconSize;
		private int maxCount = 1;
		public informationWindow() { InitializeComponent(); }
		private void OKbutton_Click(object sender, EventArgs e) { Close(); }
		//Initialize default values
		public void initializeDialog(string saveTitle, string saveProdCode, string saveIdentifier, SaveRegion saveRegion, int saveSize, int iconFrames, int interpolationMode, int iconPropertiesSize, Bitmap[] saveIcons, IList<int> slotNumbers, int backColor)
		{
			string ocupiedSlots = null;
			iconInterpolationMode = interpolationMode;
			iconSize = iconPropertiesSize;
			saveTitleLabel.Text = saveTitle;
			productCodeLabel.Text = saveProdCode;
			identifierLabel.Text = saveIdentifier;
			sizeLabel.Text = saveSize + " KB";
			iconFramesLabel.Text = iconFrames.ToString();
			maxCount = iconFrames;
			iconData = saveIcons;
			iconBackColor = backColor;

			//Show region string
			switch (saveRegion)
			{
				default: //Region custom, show hex
					regionLabel.Text = "0x" + saveRegion.ToString("X4");
					break;

				case SaveRegion.US:
					regionLabel.Text = "America";
					break;

				case SaveRegion.EU:
					regionLabel.Text = "Europe";
					break;

				case SaveRegion.JP:
					regionLabel.Text = "Japan";
					break;
			}

			//Get ocupied slots
			for (var i = 0; i < slotNumbers.Count; i++)
			{
				ocupiedSlots += (slotNumbers[i] + 1) + ", ";
			}

			//Show ocupied slots
			slotLabel.Text = ocupiedSlots.Remove(ocupiedSlots.Length - 2);

			//Draw first icon so there is no delay
			drawIcons(iconIndex);

			//Enable Paint timer in case of multiple frames
			if (iconFrames > 1) iconPaintTimer.Enabled = true;
		}

		//Draw scaled icons
		private void drawIcons(int selectedIndex)
		{
			var tempBitmap = new Bitmap(48, 48);
			var iconGraphics = Graphics.FromImage(tempBitmap);

			//Set icon interpolation mode
			if (iconInterpolationMode == 0) iconGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			else iconGraphics.InterpolationMode = InterpolationMode.Bilinear;

			iconGraphics.PixelOffsetMode = PixelOffsetMode.Half;

			//Check what background color should be set
			switch (iconBackColor)
			{
				case 1: //Black
					iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00)), new Region(new Rectangle(0, 0, 48, 48)));
					break;

				case 2: //Gray
					iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x30, 0x30, 0x30)), new Region(new Rectangle(0, 0, 48, 48)));
					break;

				case 3: //Blue
					iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x44, 0x44, 0x98)), new Region(new Rectangle(0, 0, 48, 48)));
					break;
			}

			iconGraphics.DrawImage(iconData[selectedIndex], 0, 0, 32 + (iconSize*16), 32 + (iconSize*16));

			iconRender.Image = tempBitmap;
			iconGraphics.Dispose();
		}

		private void informationWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			//Disable Paint timer
			iconPaintTimer.Enabled = false;
		}

		private void iconPaintTimer_Tick(object sender, EventArgs e)
		{
			if (iconIndex < (maxCount - 1)) iconIndex++;
			else iconIndex = 0;
			drawIcons(iconIndex);
		}

		private void informationWindow_Load(object sender, EventArgs e) { }
	}
}