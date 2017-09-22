#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using Newtonsoft.Json;

public class ShipObject 
{
    public ShipObject(string type, int x, int y)
    {
        Type = type;
        X = x;
        Y = y;
    }
    public string Type { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    public int X { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    public int Y { get; set; }
}