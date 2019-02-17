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

[RequireComponent(typeof(LineRenderer))]
public class VisualPath : MonoBehaviour
{
    private LineRenderer lineRenderer;

    /// <summary>
    /// Set a list of points that will be visualized.
    /// </summary>
    /// <param name="charName"></param>
    /// <param name="points"></param>
    public void SetVisualPoints(IEnumerable<Tile> tiles)
    {
        List<Vector3> vertexes = new List<Vector3>();
        foreach (Tile tile in tiles)
        {
            vertexes.Add(tile.Vector3);
        }
        lineRenderer.positionCount = vertexes.Count;
        lineRenderer.SetPositions(vertexes.ToArray());
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

    }

}
