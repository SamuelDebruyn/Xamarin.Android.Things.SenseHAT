using System;
using System.Linq;
using Android.Things.UserDriver;
using Android.Views;

namespace Xamarin.Android.Things.SenseHAT
{
	public class JoystickInputDriver : IDisposable
	{
		readonly Keycode[] _keyCodes;
		InputDriver _inputDriver;
		Joystick _joystick;

		public JoystickInputDriver(Keycode[] keyCodes)
		{
			_keyCodes = keyCodes;
			_joystick = new Joystick(keyCodes);
		}

		public JoystickInputDriver() : this(new[] {Keycode.DpadLeft, Keycode.DpadUp, Keycode.DpadRight, Keycode.DpadDown, Keycode.DpadCenter})
		{
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Register()
		{
			_joystick.JoystickClicked -= OnJoystickClicked;
			_joystick.JoystickClicked += OnJoystickClicked;

			if (_inputDriver != null)
			{
				return;
			}

			_inputDriver = new InputDriver.Builder((int) InputSourceType.ClassButton)
				.SetName("SKRHABE010")
				.SetKeys(_keyCodes.Cast<int>().ToArray())
				.Build();
			UserDriverManager.Manager.RegisterInputDriver(_inputDriver);
		}

		void OnJoystickClicked(object sender, Joystick.JoystickClickedEventArgs e)
		{
			_inputDriver?.Emit(e.KeyEvents);
		}

		public void Unregister()
		{
			_joystick.JoystickClicked -= OnJoystickClicked;

			if (_inputDriver == null)
			{
				return;
			}

			UserDriverManager.Manager.UnregisterInputDriver(_inputDriver);
			_inputDriver = null;
		}

		protected virtual void Dispose(bool disposing)
		{
			Unregister();

			if (disposing)
			{
				_inputDriver?.Dispose();
				_joystick?.Dispose();

				_inputDriver = null;
				_joystick = null;
			}
		}

		~JoystickInputDriver()
		{
			Dispose(false);
		}
	}
}