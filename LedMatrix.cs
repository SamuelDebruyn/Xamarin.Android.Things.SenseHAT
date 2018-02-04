using System;
using System.Diagnostics;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Things.Pio;
using System.Linq;

namespace Xamarin.Android.Things.SenseHAT
{
	public class LedMatrix : IDisposable
	{
		const int SIZE = 8;
		const int ADDRESS = 0x46;
		const int BUFFER_SIZE = SIZE * SIZE * 3 + 1;
		const float TEST_TEXT_SIZE = 48f;

		PeripheralManagerService _peripheralManagerService;
		I2cDevice _rawMatrix;

		public LedMatrix()
		{
			_peripheralManagerService = new PeripheralManagerService();
			_rawMatrix = _peripheralManagerService.OpenI2cDevice(_peripheralManagerService.I2cBusList.First(), ADDRESS);
		}

		public void Draw(Bitmap bitmap)
		{
			var bytes = new byte[BUFFER_SIZE];
			bytes[0] = 0;

			using (var scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, SIZE, SIZE, true))
			{
				for (var y = 0; y < SIZE; y++)
				{
					for (var x = 0; x < SIZE; x++)
					{
						var pixel = scaledBitmap.GetPixel(x, y);
						DrawPixel(bytes, x, y, pixel);
					}
				}
			}

			_rawMatrix.Write(bytes, bytes.Length);
		}

		public void Draw(Drawable drawable)
		{
			if (drawable is BitmapDrawable bitmapDrawable)
			{
				Draw(bitmapDrawable.Bitmap);
				return;
			}

			if (drawable is ColorDrawable colorDrawable)
			{
				Draw(colorDrawable.Color);
				return;
			}

			Bitmap bitmap;
			if (drawable.IntrinsicWidth <= 0 || drawable.IntrinsicHeight <= 0)
			{
				bitmap = Bitmap.CreateBitmap(0, 0, Bitmap.Config.Argb8888);
			}
			else
			{
				bitmap = Bitmap.CreateBitmap(drawable.IntrinsicWidth, drawable.IntrinsicHeight, Bitmap.Config.Argb8888);
			}

			var canvas = new Canvas(bitmap);
			drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
			drawable.Draw(canvas);

			Draw(bitmap);

			canvas.Dispose();
			bitmap.Dispose();
		}

		public void Draw(char character, Paint paint, Color? backgroundColor = null)
		{
			var text = character.ToString();

			var left = 0;
			var bottom = SIZE;
			var bounds = SetTextSize(paint, text);

			if(bounds.Left < 0)
			{
				left = bounds.Left;
			}

			if(bounds.Left > 0)
			{
				left = -bounds.Left;
			}

			if(bounds.Bottom > 0)
			{
				bottom = SIZE - bounds.Bottom;
			}

			using (var bitmap = Bitmap.CreateBitmap(SIZE, SIZE, Bitmap.Config.Argb8888))
			{
				using (var canvas = new Canvas(bitmap))
				{
					if (backgroundColor.HasValue)
					{
						canvas.DrawColor(backgroundColor.Value);
					}
					canvas.DrawText(text, left, bottom, paint);
				}
				Draw(bitmap);
			}
		}

		public void Draw(char character, Color color, Color? backgroundColor = null)
		{
			using (var paint = new Paint { Color = color })
			{
				Draw(character, paint, backgroundColor);
			}
		}

		public void Draw(int color)
		{
			var bytes = new byte[BUFFER_SIZE];

			bytes[0] = 0;
			for (var y = 0; y < SIZE; y++)
			{
				for (var x = 0; x < SIZE; x++)
				{
					DrawPixel(bytes, x, y, color);
				}
			}

			_rawMatrix.Write(bytes, bytes.Length);
		}

		static void DrawPixel(byte[] buffer, int x, int y, int pixel)
		{
			var alpha = Color.GetAlphaComponent(pixel) / 255f;

			var red = (int)(Color.GetRedComponent(pixel) * alpha);
			var green = (int)(Color.GetGreenComponent(pixel) * alpha);
			var blue = (int)(Color.GetBlueComponent(pixel) * alpha);

			var shiftedRed = (byte)(red >> 3);
			var shiftedGreen = (byte)(green >> 3);
			var shiftedBlue = (byte)(blue >> 3);
			
			Debug.WriteLine($"Drawing ({x+1}, {y+1}) color ({red}, {green}, {blue}, {alpha})");

			buffer[1 + x + SIZE * 0 + 3 * SIZE * y] = shiftedRed;
			buffer[1 + x + SIZE * 1 + 3 * SIZE * y] = shiftedGreen;
			buffer[1 + x + SIZE * 2 + 3 * SIZE * y] = shiftedBlue;
		}

		protected virtual void Dispose(bool disposing)
		{
			_rawMatrix?.Close();
			_rawMatrix?.Dispose();
			_rawMatrix = null;

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

		~LedMatrix()
		{
			Dispose(false);
		}

		static Rect SetTextSize(Paint paint, string text)
		{
			var maxSize = SIZE;
			paint.TextSize = TEST_TEXT_SIZE;
			var bounds = new Rect();
			paint.GetTextBounds(text, 0, text.Length, bounds);
			var betterSize = Math.Min(TEST_TEXT_SIZE * maxSize / bounds.Width(), TEST_TEXT_SIZE * maxSize / bounds.Height());
			paint.TextSize = betterSize;

			var realBounds = new Rect();
			paint.GetTextBounds(text, 0, text.Length, realBounds);
			return realBounds;
		}
	}
}
