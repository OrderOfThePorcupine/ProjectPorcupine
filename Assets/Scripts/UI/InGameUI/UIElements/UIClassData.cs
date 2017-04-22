#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

/// <summary>
/// Represents data for a code UI class.
/// </summary>
public struct UIClassData
{
    /// <summary>
    /// Construct a class of UIClassData.
    /// </summary>
    /// <param name="className"> The name of the UI element class. </param>
    /// <param name="parameterData"> Any parameter data to supply. </param>
    public UIClassData(string className, Parameter parameterData)
    {
        this.ClassName = className;
        this.ParameterData = parameterData;
    }

    /// <summary>
    /// The class name.
    /// </summary>
    public string ClassName { get; private set; }

    /// <summary>
    /// Parameters for the class.
    /// </summary>
    public Parameter ParameterData { get; private set; }
}