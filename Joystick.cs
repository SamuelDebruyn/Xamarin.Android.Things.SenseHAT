using System;
using System.Diagnostics;
using System.Linq;
using Android.Things.Pio;
using Android.Things.UserDriver;
using Android.Views;
using Java.Lang;
using Android.OS;
using Java.IO;

namespace Xamarin.Android.Things.SenseHAT
{
	/// <remarks>
	/// SKRHABE010
	/// </remarks>
	public class Joystick : IDisposable
	{
		const byte ADDRESS = 0x46;
		const byte DELAY_MS = 32;
		PeripheralManagerService _peripheralManagerService;
		Handler _handler;
		Action _listenerAction;

		public event EventHandler<JoystickClickedEventArgs> JoystickClicked;

		public Joystick()
		{
			_peripheralManagerService = new PeripheralManagerService();

			InitHandler();
		}

		public bool LeftKeyPressed { get; private set; }
		public bool RightKeyPressed { get; private set; }
		public bool UpKeyPressed { get; private set; }
		public bool DownKeyPressed { get; private set; }
		public bool EnterKeyPressed { get; private set; }

		void InitHandler()
		{
			_listenerAction = CheckButtonState;
			_handler = new Handler(Looper.MyLooper());
			_handler.Post(_listenerAction);
		}

		// takes about 10ms
		public void CheckButtonState()
		{
			if (_handler == null)
			{
				return;
			}

			var success = ReadSensor(out var leftKeyPressed, out var upKeyPressed, out var rightKeyPressed, out var downKeyPressed, out var enterKeyPressed);

			if(!success)
			{
				_handler.PostDelayed(_listenerAction, DELAY_MS);
				return;
			}

			var changed = LeftKeyPressed != leftKeyPressed || UpKeyPressed != upKeyPressed || RightKeyPressed != rightKeyPressed || DownKeyPressed != downKeyPressed || EnterKeyPressed != enterKeyPressed;
			if (changed)
			{
				JoystickClicked?.Invoke(this, new JoystickClickedEventArgs(leftKeyPressed, upKeyPressed, rightKeyPressed, downKeyPressed, enterKeyPressed));
			}

			LeftKeyPressed = leftKeyPressed;
			RightKeyPressed = rightKeyPressed;
			UpKeyPressed = upKeyPressed;
			DownKeyPressed = downKeyPressed;
			EnterKeyPressed = enterKeyPressed;

			_handler.PostDelayed(_listenerAction, DELAY_MS);
		}

		bool ReadSensor(out bool leftKeyPressed, out bool upKeyPressed, out bool rightKeyPressed, out bool downKeyPressed, out bool enterKeyPressed)
		{
			leftKeyPressed = false;
			upKeyPressed = false;
			rightKeyPressed = false;
			downKeyPressed = false;
			enterKeyPressed = false;
			
			var buffer = new byte[1];

			try
			{
				using (var rawDevice = _peripheralManagerService.OpenI2cDevice(_peripheralManagerService.I2cBusList.First(), ADDRESS))
				{
					rawDevice.ReadRegBuffer(0xf2, buffer, 1);
					rawDevice.Close();
				}
			}
			catch (IOException)
			{
				// skip
				return false;
			}

			var state = buffer[0];

			leftKeyPressed = (state & 0x10) > 0;
			upKeyPressed = (state & 0x04) > 0;
			rightKeyPressed = (state & 0x02) > 0;
			downKeyPressed = (state & 0x01) > 0;
			enterKeyPressed = (state & 0x08) > 0;

			return true;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_handler?.RemoveCallbacks(_listenerAction);
				_handler?.Dispose();
				_handler = null;
				_listenerAction = null;

				_peripheralManagerService?.Dispose();
				_peripheralManagerService = null;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Joystick()
		{
			Dispose(false);
		}

		public class JoystickClickedEventArgs : EventArgs
		{
			public bool LeftKeyPressed { get; }
			public bool RightKeyPressed { get; }
			public bool UpKeyPressed { get; }
			public bool DownKeyPressed { get; }
			public bool EnterKeyPressed { get; }

			public JoystickClickedEventArgs(bool leftKeyPressed, bool upKeyPressed, bool rightKeyPressed, bool downKeyPressed, bool enterKeyPressed)
			{
				LeftKeyPressed = leftKeyPressed;
				UpKeyPressed = upKeyPressed;
				RightKeyPressed = rightKeyPressed;
				DownKeyPressed = downKeyPressed;
				EnterKeyPressed = enterKeyPressed;
			}

			public override string ToString()
			{
				return $"LEFT: {LeftKeyPressed}, UP: {UpKeyPressed}, RIGHT: {RightKeyPressed}, DOWN: {DownKeyPressed}, ENTER: {EnterKeyPressed}";
			}
		}
	}
}
