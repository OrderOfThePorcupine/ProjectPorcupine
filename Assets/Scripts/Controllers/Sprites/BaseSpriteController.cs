#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSpriteController<T>
{
    protected Dictionary<T, GameObject> objectGameObjectMap;
    protected GameObject objectParent;

    public BaseSpriteController(string parentName)
    {
        objectParent = new GameObject(parentName);
        objectGameObjectMap = new Dictionary<T, GameObject>();
    }

    public virtual void RemoveAll()
    {
        objectGameObjectMap.Clear();
        GameObject.Destroy(objectParent);
    }

    /// <summary>
    /// Register world.
    /// </summary>
    /// <param name="world"> World to register. </param>
    public abstract void AssignWorld(World world);

    /// <summary>
    /// Unregister world.
    /// </summary>
    /// <param name="world"> World to unregister. </param>
    public abstract void UnAssignWorld(World world);

    protected abstract void OnCreated(T obj);

    protected abstract void OnChanged(T obj);

    protected abstract void OnRemoved(T obj);
}
