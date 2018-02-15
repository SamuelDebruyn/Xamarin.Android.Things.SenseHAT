using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Things.Pio;
using Android.Views;
using Java.IO;

namespace Xamarin.Android.Things.SenseHAT
{
	/// <remarks>
	///     SKRHABE010
	/// </remarks>
	public class Joystick : IDisposable
	{
		const byte ADDRESS = 0x46;
		const byte DELAY_MS = 32;
		readonly Keycode[] _keyCodes;
		Handler _handler;
		Action _listenerAction;

		PeripheralManagerService _peripheralManagerService;

		public Joystick(Keycode[] keyCodes)
		{
			_keyCodes = keyCodes;
			_peripheralManagerService = new PeripheralManagerService();

			InitHandler();
		}

		public bool LeftKeyPressed { get; private set; }
		public bool RightKeyPressed { get; private set; }
		public bool UpKeyPressed { get; private set; }
		public bool DownKeyPressed { get; private set; }
		public bool EnterKeyPressed { get; private set; }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public event EventHandler<JoystickClickedEventArgs> JoystickClicked;

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

			if (!success)
			{
				_handler.PostDelayed(_listenerAction, DELAY_MS);
				return;
			}

			var changed = LeftKeyPressed != leftKeyPressed || UpKeyPressed != upKeyPressed || RightKeyPressed != rightKeyPressed || DownKeyPressed != downKeyPressed ||
			              EnterKeyPressed != enterKeyPressed;
			if (changed)
			{
				JoystickClicked?.Invoke(this,
					new JoystickClickedEventArgs(leftKeyPressed, upKeyPressed, rightKeyPressed, downKeyPressed, enterKeyPressed, LeftKeyPressed, RightKeyPressed, UpKeyPressed,
						DownKeyPressed, EnterKeyPressed, _keyCodes));
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

		~Joystick()
		{
			Dispose(false);
		}

		public class JoystickClickedEventArgs : EventArgs
		{
			readonly Keycode[] _keycodes;

			public JoystickClickedEventArgs(bool leftKeyPressed, bool upKeyPressed, bool rightKeyPressed, bool downKeyPressed, bool enterKeyPressed,
				bool previousLeftKeyPressed, bool previousRightKeyPressed, bool previousUpKeyPressed, bool previousDownKeyPressed, bool previousEnterKeyPressed,
				Keycode[] keycodes)
			{
				_keycodes = keycodes;

				var events = new List<KeyEvent>();

				if (leftKeyPressed != previousLeftKeyPressed)
				{
					events.Add(new KeyEvent(leftKeyPressed ? KeyEventActions.Up : KeyEventActions.Down, _keycodes[0]));
				}

				if (upKeyPressed != previousUpKeyPressed)
				{
					events.Add(new KeyEvent(upKeyPressed ? KeyEventActions.Up : KeyEventActions.Down, _keycodes[1]));
				}

				if (rightKeyPressed != previousRightKeyPressed)
				{
					events.Add(new KeyEvent(rightKeyPressed ? KeyEventActions.Up : KeyEventActions.Down, _keycodes[2]));
				}

				if (downKeyPressed != previousDownKeyPressed)
				{
					events.Add(new KeyEvent(downKeyPressed ? KeyEventActions.Up : KeyEventActions.Down, _keycodes[3]));
				}

				if (enterKeyPressed != previousEnterKeyPressed)
				{
					events.Add(new KeyEvent(enterKeyPressed ? KeyEventActions.Up : KeyEventActions.Down, _keycodes[4]));
				}

				KeyEvents = events.ToArray();
			}

			public KeyEvent[] KeyEvents { get; }
		}
	}
}