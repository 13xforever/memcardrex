using System;
using System.Windows.Forms;

namespace MemcardRex
{
	public partial class commentsWindow : Form
	{
		//If OK is pressed this will be true
		public bool okPressed;
		public string saveComment;
		public commentsWindow() { InitializeComponent(); }
		private void commentsWindow_Load(object sender, EventArgs e) { }
		//Load initial values
		public void initializeDialog(string dialogTitle, string saveComment)
		{
			//Set window title to save name
			Text = dialogTitle;
			commentsTextBox.Text = saveComment;

			//A fix for selected all behaviour
			commentsTextBox.Select(commentsTextBox.Text.Length, 0);
		}

		private void cancelButton_Click(object sender, EventArgs e) { Close(); }

		private void okButton_Click(object sender, EventArgs e)
		{
			//Return value given by the dialog
			saveComment = commentsTextBox.Text;

			okPressed = true;
			Close();
		}
	}
}