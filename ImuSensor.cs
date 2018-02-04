using System;
using System.Linq;
using Android.Things.Pio;

namespace Xamarin.Android.Things.SenseHAT
{
	/// <remarks>
	/// LSM9DS1
	/// </remarks>
	public class ImuSensor: IDisposable
	{
		const int ADDRESS = 0x6a;
		PeripheralManagerService _peripheralManagerService;
		I2cDevice _rawDevice;

		public ImuSensor()
		{
			_peripheralManagerService = new PeripheralManagerService();
			_rawDevice = _peripheralManagerService.OpenI2cDevice(_peripheralManagerService.I2cBusList.First(), ADDRESS);
		}

		protected virtual void Dispose(bool disposing)
		{
			_rawDevice?.Close();
			_rawDevice?.Dispose();
			_rawDevice = null;
			
			if (disposing)
			{
				_peripheralManagerService?.Dispose();
				_peripheralManagerService = null;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~ImuSensor()
		{
			Dispose(false);
		}
	}
}
