#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using NUnit.Framework;
using ProjectPorcupine.Entities;

[TestFixture]
public class ModTester
{
    [OneTimeSetUp]
    public void Setup()
    {
        FunctionsManager.Initialize();
        SpriteManager.Initialize();
        AudioManager.Initialize();
        CharacterNameManager.Initialize();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        AudioManager.Destroy();
    }

    [Test]
    public void T01_Introduction()
    {
        new ModsManager(ModsManager.Type.Intro);
    }

    [Test]
    public void T02_MainScene()
    {
        new ModsManager(ModsManager.Type.MainScene);
    }
}
