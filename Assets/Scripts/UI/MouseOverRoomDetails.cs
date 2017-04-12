#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using ProjectPorcupine.Rooms;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MouseOverRoomIndex impliments the abstact class MouseOver.
/// It returns info strings that represent the tiles room Details.
/// </summary>
public class MouseOverRoomDetails : MouseOver
{
    protected override string GetMouseOverString(Tile tile)
    {
        if (tile == null || tile.Room == null)
        {
            return string.Empty;
        }

        string roomDetails = string.Format("{0:F}/{0:C}\n", World.Current.temperature.GetTemperature(tile.X, tile.Y, tile.Z));

        foreach (string gasName in tile.Room.Atmosphere.GetGasNames())
        {
            roomDetails += string.Format("{0}: ({1}) {2:0.000} atm ({3:0.0}%)\n", gasName, "-", tile.Room.GetGasPressure(gasName), tile.Room.Atmosphere.GetGasFraction(gasName) * 100);
        }

        if (tile.Room.RoomBehaviors.Count > 0)
        {
            roomDetails += "Behaviors:\n";
            foreach (RoomBehavior behavior in tile.Room.RoomBehaviors.Values)
            {
                roomDetails += behavior.Name + "\n";
            }
        }

        return roomDetails;
    }
}
