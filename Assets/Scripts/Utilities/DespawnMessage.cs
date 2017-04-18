using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple class.  All it does is contain a function to repool.
/// The system can just do a send message for all children,
/// and the repools will pull out before its too late.
/// </summary>
public class DespawnMessage : MonoBehaviour
{
    /// <summary>
    /// Remove from pool.
    /// </summary>
    public void Despawn()
    {
        this.transform.SetParent(null);
        SimplePool.Despawn(this.gameObject);
    }
}
