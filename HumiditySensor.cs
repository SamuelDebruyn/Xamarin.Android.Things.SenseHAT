using System;
using System.Linq;
using Android.OS;
using Android.Things.Pio;

namespace Xamarin.Android.Things.SenseHAT
{
	/// <remarks>
	///     HTS221
	/// </remarks>
	public class HumiditySensor : IDisposable
	{
		const byte DELAY_MS = 16;

		//  HTS221 I2C Slave Address

		const byte ADDRESS = 0x5f;
		const byte REG_ID = 0x0f;
		const byte ID = 0xbc;

		//  Register map

		const byte WHO_AM_I = 0x0f;
		const byte AV_CONF = 0x10;
		const byte CTRL1 = 0x20;
		const byte CTRL2 = 0x21;
		const byte CTRL3 = 0x22;
		const byte STATUS = 0x27;
		const byte HUMIDITY_OUT_L = 0x28;
		const byte HUMIDITY_OUT_H = 0x29;
		const byte TEMP_OUT_L = 0x2a;
		const byte TEMP_OUT_H = 0x2b;
		const byte H0_H_2 = 0x30;
		const byte H1_H_2 = 0x31;
		const byte T0_C_8 = 0x32;
		const byte T1_C_8 = 0x33;
		const byte T1_T0 = 0x35;
		const byte H0_T0_OUT = 0x36;
		const byte H1_T0_OUT = 0x3a;
		const byte T0_OUT = 0x3c;
		const byte T1_OUT = 0x3e;
		
		Handler _handler;
		Func<short, double> _humidityConversion;
		Action _listenerAction;
		PeripheralManagerService _peripheralManagerService;
		I2cDevice _rawDevice;
		Func<short, double> _temperatureConversion;

		public HumiditySensor()
		{
			_peripheralManagerService = new PeripheralManagerService();
			_rawDevice = _peripheralManagerService.OpenI2cDevice(_peripheralManagerService.I2cBusList.First(), ADDRESS);

			InitHandler();
		}

		public double? Temperature { get; private set; }
		public double? Humidity { get; private set; }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public event EventHandler<TemperatureChangedEventArgs> TemperatureChanged;
		public event EventHandler<HumidityChangedEventArgs> HumidityChanged;

		void InitHandler()
		{
			_rawDevice.WriteRegWord(CTRL1, 0x87);
			_rawDevice.WriteRegWord(AV_CONF, 0x1b);

			_temperatureConversion = GetTemperatureConversionFunc();
			_humidityConversion = GetHumidityConversionFunc();

			_listenerAction = CheckSensor;
			_handler = new Handler(Looper.MyLooper());
			_handler.Post(_listenerAction);
		}

		public void CheckSensor()
		{
			if (_rawDevice == null || _rawDevice.Handle == IntPtr.Zero || _handler == null || _temperatureConversion == null || _humidityConversion == null)
			{
				return;
			}

			ReadSensor(out var humidity, out var temperature);

			if (humidity.HasValue)
			{
				if (!Equals(Humidity, humidity))
				{
					HumidityChanged?.Invoke(this, new HumidityChangedEventArgs(humidity.Value));
				}

				Humidity = humidity;
			}

			if (temperature.HasValue)
			{
				if (!Equals(Temperature, temperature))
				{
					TemperatureChanged?.Invoke(this, new TemperatureChangedEventArgs(temperature.Value));
				}
				
				Temperature = temperature;
			}

			_handler.PostDelayed(_listenerAction, DELAY_MS);
		}

		void ReadSensor(out double? humidity, out double? temperature)
		{
			var status = _rawDevice.ReadRegByte(STATUS);

			if ((status & 0x02) == 0x02)
			{
				var rawHumidity = _rawDevice.ReadRegWord(HUMIDITY_OUT_L + 0x80);
				humidity = _humidityConversion(rawHumidity);
			}
			else
			{
				humidity = null;
			}

			if ((status & 0x01) == 0x01)
			{
				var rawTemperature = _rawDevice.ReadRegWord(TEMP_OUT_L + 0x80);
				temperature = _temperatureConversion(rawTemperature);
			}
			else
			{
				temperature = null;
			}
		}

		Func<short, double> GetTemperatureConversionFunc()
		{
			var tempRawMsb = _rawDevice.ReadRegByte(T1_T0 + 0x80);
			var temp0Lsb = _rawDevice.ReadRegByte(T0_C_8 + 0x80);
			var t0C8 = (ushort) ((((ushort) tempRawMsb & 0x03) << 8) | (ushort) temp0Lsb);
			var t0 = t0C8 / 8.0;
			var temp1Lsb = _rawDevice.ReadRegByte(T1_C_8 + 0x80);
			var t1C8 = (ushort) (((ushort) (tempRawMsb & 0x0C) << 6) | (ushort) temp1Lsb);
			var t1 = t1C8 / 8.0;
			var t0Out = _rawDevice.ReadRegWord(T0_OUT + 0x80);
			var t1Out = _rawDevice.ReadRegWord(T1_OUT + 0x80);

			// Temperature calibration slope
			var m = (t1 - t0) / (t1Out - t0Out);

			// Temperature calibration y intercept
			var b = t0 - m * t0Out;

			return rawTemperature => rawTemperature * m + b;
		}

		Func<short, double> GetHumidityConversionFunc()
		{
			var h0H2 = _rawDevice.ReadRegByte(H0_H_2 + 0x80);
			var h0 = h0H2 / 2.0;
			var h1H2 = _rawDevice.ReadRegByte(H1_H_2 + 0x80);
			var h1 = h1H2 / 2.0;
			var h0T0Out = _rawDevice.ReadRegWord(H0_T0_OUT + 0x80);
			var h1T0Out = _rawDevice.ReadRegWord(H1_T0_OUT + 0x80);

			// Humidity calibration slope
			var m = (h1 - h0) / (h1T0Out - h0T0Out);

			// Humidity calibration y intercept
			var b = h0 - m * h0T0Out;

			return rawHumidity => rawHumidity * m + b;
		}

		protected virtual void Dispose(bool disposing)
		{
			_rawDevice?.Close();
			_rawDevice?.Dispose();
			_rawDevice = null;

			if (disposing)
			{
				_handler?.RemoveCallbacks(_listenerAction);
				_handler?.Dispose();
				_handler = null;
				_listenerAction = null;

				_temperatureConversion = null;
				_humidityConversion = null;

				_peripheralManagerService?.Dispose();
				_peripheralManagerService = null;
			}
		}

		~HumiditySensor()
		{
			Dispose(false);
		}

		public class TemperatureChangedEventArgs : EventArgs
		{
			public TemperatureChangedEventArgs(double convertedTemperature)
			{
				Temperature = convertedTemperature;
			}

			public double Temperature { get; }
		}

		public class HumidityChangedEventArgs : EventArgs
		{
			public HumidityChangedEventArgs(double convertedHumidity)
			{
				Humidity = convertedHumidity;
			}

			public double Humidity { get; }
		}
	}
}