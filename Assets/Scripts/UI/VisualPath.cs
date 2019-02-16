#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using UnityEngine;

public class VisualPath : MonoBehaviour
{
    public Material lineMaterial;

    public static VisualPath Instance { get; private set; }

    /// <summary>
    /// Character IDs to a list of their path.
    /// </summary>
    public Dictionary<int, List<Tile>> VisualPoints { get; private set; }

    public Color PathColor = Color.red;

    /// <summary>
    /// Set a list of points that will be visualized.
    /// </summary>
    /// <param name="charName"></param>
    /// <param name="points"></param>
    public void SetVisualPoints(int charID, List<Tile> points)
    {
        // A character changed there path, so we need to update the new path
        if (VisualPoints.ContainsKey(charID))
        {
            VisualPoints[charID] = points;
            return;
        }

        VisualPoints.Add(charID, points);
    }

    /// <summary>
    /// Removes a charcters entry in the VisualPoints dictionary.
    /// </summary>
    /// <param name="charName"></param>
    public void RemoveVisualPoints(int charID)
    {
        // maybe the character has died or we just no longer wnat to see his path any more
        if (VisualPoints.ContainsKey(charID))
        {
            VisualPoints.Remove(charID);
        }
    }

    private void Awake()
    {
        // initalize dictionary
        VisualPoints = new Dictionary<int, List<Tile>>();

        // default PathColor to red
        PathColor = Color.red;
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnRenderObject()
    {
        // Apply the line material
        lineMaterial.SetPass(0);

        GL.PushMatrix();

        // Set transformation matrix for drawing to
        // match our transform
        GL.MultMatrix(transform.localToWorldMatrix);

        // Draw lines
        GL.Begin(GL.LINES);
        GL.Color(PathColor);
        foreach (int entry in VisualPoints.Keys)
        {
            for (int i = 0; i < VisualPoints[entry].Count; i++)
            {
                if (i != 0)
                {
                    GL.Vertex3(VisualPoints[entry][i - 1].X, VisualPoints[entry][i - 1].Y, VisualPoints[entry][i - 1].Z);
                }
                else
                {
                    GL.Vertex3(VisualPoints[entry][i].X, VisualPoints[entry][i].Y, VisualPoints[entry][i].Z);
                }

                GL.Vertex3(VisualPoints[entry][i].X, VisualPoints[entry][i].Y, VisualPoints[entry][i].Z);
            }
        }

        GL.End();
        GL.PopMatrix();
    }
}
