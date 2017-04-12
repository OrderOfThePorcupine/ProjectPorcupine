#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Globalization;
using MoonSharp.Interpreter;
using UnityEngine;

/// <summary>
/// Holds temperature data in Kelvin but can be accessed in celsius, fahrenheit, and rankine.
/// </summary>
[MoonSharpUserData]
public class TemperatureValue : IFormattable
{
    /// <summary>
    /// Absolute 0, or 0 K.
    /// </summary>
    public static TemperatureValue AbsoluteZero = new TemperatureValue(0f);

    /// <summary>
    /// The freezing point of water, or 0 C.
    /// </summary>
    public static TemperatureValue CelsiusZero = new TemperatureValue(273.15f);

    /// <summary>
    /// The boiling point of water, or 100 C.
    /// </summary>
    public static TemperatureValue Celsius100 = new TemperatureValue(373.15f);

    /// <summary>
    /// Create from Kelvin units.
    /// </summary>
    /// <param name="temperatureInKelvin"> Temperature in Kelvin. </param>
    public TemperatureValue(float temperatureInKelvin)
    {
        this.InKelvin = temperatureInKelvin;
    }

    /// <summary>
    /// The current temperature in Kelvin.
    /// </summary>
    public float InKelvin { get; set; }

    /// <summary>
    /// The current temperature in Celsius.
    /// </summary>
    public float InCelsius
    {
        get
        {
            return InKelvin - 273.15f;
        }

        set
        {
            InKelvin = value + 273.15f;
        }
    }

    /// <summary>
    /// The current temperature in Farenheit.
    /// </summary>
    public float InFahrenheit
    {
        get
        {
            return (InKelvin * 1.8f) - 459.67f;
        }

        set
        {
            InKelvin = (value + 459.67f) * (5 / 9);
        }
    }

    /// <summary>
    /// The current temperature in Rankine.
    /// </summary>
    public float InRankine
    {
        get
        {
            return InKelvin * 1.8f;
        }


        set
        {
            InKelvin = value * (5 / 9);
        }
    }

    /// <summary>
    /// Returns a string of the current temperature in format: K: x, C: C(x), F: F(x), R: R(x).
    /// Where C(x)/F(x)/R(x) mean the celsius/farenheit/rankine function/conversion of x.
    /// </summary>
    /// <remarks> Good for debuggging. </remarks>
    /// <returns> A string of every format. </returns>
    public override string ToString()
    {
        return "K: " + InKelvin + " C: " + InCelsius + " F: " + InFahrenheit + " R: " + InRankine;
    }

    /// <summary>
    /// Returns a string formatted to the format supplied.
    /// </summary>
    /// <param name="format"> Format to format the string to. </param>
    /// <returns> A formmatted string. </returns>
    public string ToString(string format)
    {
        return this.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a string formatted to the format supplied.
    /// Also includes a format provider for country specific data.
    /// </summary>
    /// <param name="format"> Format to format the string to. </param>
    /// <param name="formatProvider"> The format provider to use for globalization/localization of data. </param>
    /// <returns> A formmatted string. </returns>
    public string ToString(string format, IFormatProvider formatProvider)
    {
        if (string.IsNullOrEmpty(format))
        {
            format = "G";
        }

        if (formatProvider == null)
        {
            formatProvider = CultureInfo.CurrentCulture;
        }

        switch (format.ToUpperInvariant())
        {
            case "G":
            case "C":
                return InCelsius.ToString("F2", formatProvider) + " °C";
            case "F":
                return InFahrenheit.ToString("F2", formatProvider) + " °F";
            case "K":
                return InKelvin.ToString("F2", formatProvider) + " K";
            case "R":
                return InRankine.ToString("F2", formatProvider) + " °R";
            default:
                throw new FormatException(string.Format("The {0} format string is not supported.", format));
        }
    }
}
