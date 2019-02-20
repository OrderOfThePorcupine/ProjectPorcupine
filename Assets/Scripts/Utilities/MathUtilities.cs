#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;

/// <summary>
/// Different mathematical calculations. 
/// </summary>
public static class MathUtilities
{
    /// <summary>
    /// If a - b is less than double.Epsilon value then they are treated as equal.
    /// </summary>
    /// <returns>true if a - b &lt; tolerance else false.</returns>
    public static bool AreEqual(this double a, double b)
    {
        if (a.CompareTo(b) == 0)
        {
            return true;
        }

        return Math.Abs(a - b) < double.Epsilon;
    }

    /// <summary>
    /// If value is lower than double.Epsilon value then value is treated as zero.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>true if value is lower than tolerance value.</returns>
    public static bool IsZero(this double value)
    {
        return Math.Abs(value) < double.Epsilon;
    }

    /// <summary>
    /// If a - b is less than float.Epsilon value then they are treated as equal.
    /// </summary>
    /// <returns>true if a - b &lt; tolerance else false.</returns>
    public static bool AreEqual(this float a, float b)
    {
        if (a.CompareTo(b) == 0)
        {
            return true;
        }

        return Math.Abs(a - b) < float.Epsilon;
    }

    /// <summary>
    /// If value is lower than float.Epsilon value then value is treated as zero.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>true if value is lower than tolerance value.</returns>
    public static bool IsZero(this float value)
    {
        return Math.Abs(value) < float.Epsilon;
    }

    /// <summary>
    /// Clamps value between min and max and returns value.
    /// </summary>
    public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
        {
            return min;
        }

        return value.CompareTo(max) > 0 ? max : value;
    }

    /// <summary>
    /// Pass in two variables and get both min and max.
    /// More efficient then two calls.
    /// </summary>
    /// <param name="x"> The first of two inputs. </param>
    /// <param name="y"> The second of two inputs. </param>
    /// <param name="min"> The minimum value. </param>
    /// <param name="max"> The maximum value. </param>
    public static void MinAndMax<T>(T x, T y, out T min, out T max) where T : System.IComparable<T>
    {
        if (x.CompareTo(y) > 0)
        {
            max = x;
            min = y;
        }
        else
        {
            max = y;
            min = x;
        }
    }
}
