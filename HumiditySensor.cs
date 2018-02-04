using System;
using System.Linq;
using Android.Things.Pio;

namespace Xamarin.Android.Things.SenseHAT
{
	/// <remarks>
	/// HTS221
	/// </remarks>
	public class HumiditySensor: IDisposable
	{
		const int ADDRESS = 0x5f;
		PeripheralManagerService _peripheralManagerService;
		I2cDevice _rawDevice;

		public HumiditySensor()
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

		~HumiditySensor()
		{
			Dispose(false);
		}
	}
}