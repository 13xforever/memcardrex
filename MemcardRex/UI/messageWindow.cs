//Customizable replacement for the Windows message window
//Shendo 2012

using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace MemcardRex
{
	public partial class messageWindow : Form
	{
		//A string which will be returned
		private string ReturnString;
		public messageWindow() { InitializeComponent(); }

		public string ShowMessage(Form HostForm, string WindowCaption, string Message, string Button1Text, string Button2Text, bool PlayExclamationSound)
		{
			return ShowMessage(HostForm, WindowCaption, Message, Button1Text, Button2Text, PlayExclamationSound, false);
		}

		public string ShowMessage(Form HostForm, string WindowCaption, string Message, string Button1Text, string Button2Text, bool PlayExclamationSound, bool Button2Selected)
		{
			//Add a window caption
			Text = WindowCaption;

			//Show the message on the label
			MessageLabel.Text = Message;

			//Fill the button text data
			Button1.Text = Button1Text;
			Button2.Text = Button2Text;

			//If Button 2 contains any text make it visible
			if (Button2Text != null) Button2.Visible = true;

			//If Button 2 should be selected change Tab index for Button1
			if (Button2Selected) Button1.TabIndex = 3;

			//Play an exclamation sound
			if (PlayExclamationSound) SystemSounds.Exclamation.Play();

			//Resize the form according to the message width
			ClientSize = new Size(MessageLabel.Width + 32, MessageLabel.Height + 80);

			//Resize the background color label
			BackgroundColorLabel.Size = new Size(120, Button1.Location.Y - 6);

			//Show this window
			ShowDialog(HostForm);

			//When the dialog is closed return the value of a button
			return ReturnString;
		}

		private void Button1_Click(object sender, EventArgs e)
		{
			ReturnString = Button1.Text;
			Close();
		}

		private void Button2_Click(object sender, EventArgs e)
		{
			ReturnString = Button2.Text;
			Close();
		}
	}
}