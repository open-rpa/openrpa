using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Open3270;
using Open3270.TN3270;
using System.Diagnostics;


namespace TerminalDemo
{
	public class Terminal : INotifyPropertyChanged
	{
		Open3270.TNEmulator emu = new TNEmulator();
		string screenText;
		bool isConnected;
		bool isConnecting;

		public Terminal()
		{
			this.emu = new TNEmulator();
			this.emu.Disconnected += emu_Disconnected;
			this.emu.CursorLocationChanged += emu_CursorLocationChanged;
		}


		void emu_Disconnected(TNEmulator where, string Reason)
		{
			this.IsConnected = false;
			this.IsConnecting = false;
			this.ScreenText = Reason;
		}

		public void Connect()
		{

			emu.Config.FastScreenMode = true;

			//Retrieve host settings
			emu.Config.HostName = Properties.Settings.Default.Hostname;
			emu.Config.HostPort = Properties.Settings.Default.HostPort;
			emu.Config.TermType = Properties.Settings.Default.TerminalType;
			emu.Config.UseSSL = Properties.Settings.Default.UseSSL;

			//Begin the connection process asynchomously
			this.IsConnecting = true;
			Task.Factory.StartNew(ConnectToHost).ContinueWith((t) =>
				{
					//Update the display when we are finished connecting
					this.IsConnecting = false;
					this.IsConnected = emu.IsConnected;
					this.ScreenText = emu.CurrentScreenXML.Dump();
				});
		}

		private void ConnectToHost()
		{
			emu.Connect();

			//Account for delays
			emu.Refresh(true, 1000);
		}

		public bool IsConnecting
		{
			get
			{
				return this.isConnecting;
			}
			set
			{
				this.isConnecting = value;
				this.OnPropertyChanged("IsConnecting");
			}
		}



		/// <summary>
		/// Indicates when the terminal is connected to the host.
		/// </summary>
		public bool IsConnected
		{
			get
			{
				return this.isConnected;
			}
			set
			{
				this.isConnected = value;
				this.OnPropertyChanged("IsConnected");
			}
		}


		/// <summary>
		/// This is the text buffer to display.
		/// </summary>
		public string ScreenText
		{
			get
			{
				return this.screenText;
			}
			set
			{
				this.screenText = value;
				this.OnPropertyChanged("ScreenText");
			}
		}



		int caretIndex;

		public int CaretIndex
		{
			get
			{
				return this.caretIndex;
			}
			set
			{
				this.caretIndex = value;
				this.OnPropertyChanged("CaretIndex");
			}
		}




		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanged



		/// <summary>
		/// Sends text to the terminal.
		/// This is used for typical alphanumeric text entry.
		/// </summary>
		/// <param name="text">The text to send</param>
		internal void SendText(string text)
		{
			this.emu.SetText(text);
			this.ScreenText = this.emu.CurrentScreenXML.Dump();
		}


		/// <summary>
		/// Sends a character to the terminal.
		/// This is used for special characters like F1, Tab, et cetera.
		/// </summary>
		/// <param name="key">The key to send.</param>
		public void SendKey(TnKey key)
		{
			this.emu.SendKey(true, key, 2000);
			if (key != TnKey.Tab && key != TnKey.BackTab)
			{
				this.Refresh();
			}
		}

		/// <summary>
		/// Forces a refresh and updates the screen display
		/// </summary>
		public void Refresh()
		{
			this.Refresh(100);
		}

	
		/// <summary>
		/// Forces a refresh and updates the screen display
		/// </summary>
		/// <param name="screenCheckInterval">This is the speed in milliseconds at which the library will poll 
		/// to determine if we have a valid screen of data to display.</param>
		public void Refresh(int screenCheckInterval)
		{
			//This line keeps checking to see when we've received a valid screen of data from the mainframe.
			//It loops until the TNEmulator.Refresh() method indicates that waiting for the screen did not time out.
			//This helps prevent blank screens, etc.
			while (!this.emu.Refresh(true, screenCheckInterval)) { }

			this.ScreenText = this.emu.CurrentScreenXML.Dump();
			this.UpdateCaretIndex();
		}


		public void UpdateCaretIndex()
		{
			
			this.CaretIndex = this.emu.CursorY * 81 + this.emu.CursorX;
		}

		void emu_CursorLocationChanged(object sender, EventArgs e)
		{
			this.UpdateCaretIndex();
		}

		/// <summary>
		/// Sends field information to the debug console.
		/// This can be used to define well-known field positions in your application.
		/// </summary>
		internal void DumpFillableFields()
		{
			string output = string.Empty;

			XMLScreenField field;

			for (int i = 0; i < this.emu.CurrentScreenXML.Fields.Length; i++)
			{
				field = this.emu.CurrentScreenXML.Fields[i];
				if (!field.Attributes.Protected)
				{
					Debug.WriteLine(string.Format("public static int fieldName = {0};   //{1},{2}  Length:{3}   {4}", i, field.Location.top + 1, field.Location.left + 1, field.Location.length, field.Text));
				}
			}
		}
	}
}
