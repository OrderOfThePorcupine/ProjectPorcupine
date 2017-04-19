#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CacheFlags
{
    /// <summary>
    /// About ~0.125Kb for this class.
    /// </summary>
    public const int INITIAL_SIZE = 1000;

    public int SIZE
    {
        get
        {
            return internalArray.Count;
        }
    }

    private BitArray internalArray = new BitArray(INITIAL_SIZE);

    public bool this[int index]
    {
        get
        {
            return internalArray.Get(index);
        }

        set
        {
            internalArray.Set(index, value);
        }
    }

    public bool GetFlag(int index)
    {
        return internalArray.Get(index);
    }

    public void SetFlag(int index, bool newValue)
    {
        internalArray.Set(index, newValue);
    }

    public void Reset()
    {
        internalArray.SetAll(false);
    }
}
