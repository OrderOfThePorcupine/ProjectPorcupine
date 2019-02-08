#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using NUnit.Framework;

public class ModTester
{

    [Test]
    public void T01_Introduction()
    {
        new AudioManager();
        ModsManager manager = new ModsManager(ModsManager.Type.Intro);
    }

    [Test]
    public void T02_MainScene()
    {
        new AudioManager();
        ModsManager manager = new ModsManager(ModsManager.Type.MainScene);
    }
}
