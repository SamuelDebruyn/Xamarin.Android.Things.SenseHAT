# Xamarin.Android.Things.SenseHAT

This is a driver for Sense HAT for [Android Things](https://developer.android.com/things/) in Xamarin.

The [Sense HAT](https://www.raspberrypi.org/products/sense-hat/) is an add-on board for a Raspberry Pi with an 8x8 RGB LED matrix, a 5 button joystick and the following sensors:

* Gyroscope
* Accelerometer
* Magnetometer
* Temperature
* Barometric pressure
* Humidity

## Work in progress

I am actively working on this driver and it is not production ready. Only the LED matrix and the joystick are working.

## Installation

I'll setup CI and a package on NuGet when this has become more stable. You'll need the [Android Things Xamarin bindings](https://www.nuget.org/packages/Xamarin.Android.Things) to get started with developing Android Things apps in Xamarin.

## Compatibility with 'native' Android Things

Xamarin is working on [.NET Embedding](https://developer.xamarin.com/guides/cross-platform/dotnet-embedding/) and I'm planning to look into this to make this library available for Android Things developers using Java/Kotlin.

## Sources

This work is based on the following:

* [RPi.SenseHat](https://github.com/emmellsoft/RPi.SenseHat): Windows IoT Core driver for the Sense HAT (C#)
* [Android Things drivers](https://github.com/androidthings/contrib-drivers): library of existing drivers for Android Things (Java)
* [RTIMULib2](https://github.com/RTIMULib/RTIMULib2): 9-dof, 10-dof and 11-dof IMU library to work with sensors (C++)
* [Sense HAT Python module](https://github.com/RPi-Distro/python-sense-hat): python module to work with the Sense HAT (Python)
