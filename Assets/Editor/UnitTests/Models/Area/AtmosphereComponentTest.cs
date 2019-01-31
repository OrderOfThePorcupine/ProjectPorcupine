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
using UnityEngine.TestTools;

public class AtmosphereComponentTest
{
    private AtmosphereComponent empty1, notEmpty1, notEmpty2;

    [SetUp]
    public void Setup()
    {
        empty1 = new AtmosphereComponent();

        notEmpty1 = new AtmosphereComponent();
        notEmpty2 = new AtmosphereComponent();
        notEmpty1.CreateGas("gas", 1.0f, 1.0f);
        notEmpty2.CreateGas("gas", 2.0f, 2.0f);
    }

    #region CreateGas
    [Test]
    public void CreateGas_PositiveAmountAndTemperature_TotalGasIncreasesByValue()
    {
        empty1.CreateGas("gas", 1.0f, 1.0f);
        notEmpty1.CreateGas("gas", 1.0f, 1.0f);

        Assert.AreEqual(1.0f, empty1.TotalGas);
        Assert.AreEqual(2.0f, notEmpty2.TotalGas);
    }

    [Test]
    public void CreateGas_PositiveAmountAndTemperature_GasOfTypeIncreasesByValue()
    {
        empty1.CreateGas("gas", 1.0f, 1.0f);
        notEmpty1.CreateGas("gas", 1.0f, 1.0f);

        Assert.AreEqual(1.0f, empty1.GetGasAmount("gas"));
        Assert.AreEqual(2.0f, notEmpty2.GetGasAmount("gas"));
    }

    [Test]
    public void CreateGas_PositiveAmountAndTemperature_TemperatureIncreasesCorrectly()
    {
        empty1.CreateGas("gas", 1.0f, 1.0f);
        notEmpty1.CreateGas("gas", 1.0f, 1.0f);
        notEmpty2.CreateGas("gas", 1.0f, 1.0f);

        Assert.AreEqual(1.0f, empty1.GetTemperature().InKelvin);
        Assert.AreEqual(1.0f, notEmpty1.GetTemperature().InKelvin);
        Assert.AreEqual(5.0f / 3.0f, notEmpty2.GetTemperature().InKelvin);
    }

    [Test]
    public void CreateGas_AmountIsNegative_NoChange()
    {
        LogAssert.Expect(UnityEngine.LogType.Error, "CreateGas -- Amount or temperature can not be negative: " + -1.0f + ", " + 1.0f);
        empty1.CreateGas("gas", -1.0f, 1.0f);

        Assert.AreEqual(0.0f, empty1.TotalGas);
    }

    [Test]
    public void CreateGas_TemperatureIsNegative_NoChange()
    {
        LogAssert.Expect(UnityEngine.LogType.Error, "CreateGas -- Amount or temperature can not be negative: " + 1.0f + ", " + -1.0f);
        empty1.CreateGas("gas", 1.0f, -1.0f);

        Assert.AreEqual(0.0f, empty1.TotalGas);
    }
    #endregion

    #region DestroyGas
    [Test]
    public void DestroyGas_PositiveAmountBelowTotal_TotalGasDecreasesByValue()
    {
        notEmpty1.DestroyGas("gas", 0.5f);

        Assert.AreEqual(0.5f, notEmpty1.TotalGas);
    }

    [Test]
    public void DestroyGas_PositiveAmountBelowTotal_GasOfTypeDecreasesByValue()
    {
        notEmpty1.DestroyGas("gas", 0.5f);

        Assert.AreEqual(0.5f, notEmpty1.GetGasAmount("gas"));
    }

    [Test]
    public void DestroyGas_PositiveAmountBelowTotal_TemperatureStaysTheSame()
    {
        notEmpty1.DestroyGas("gas", 0.5f);
        notEmpty2.DestroyGas("gas", 0.5f);

        Assert.AreEqual(1.0f, notEmpty1.GetTemperature().InKelvin);
        Assert.AreEqual(2.0f, notEmpty2.GetTemperature().InKelvin);
    }

    [Test]
    public void DestroyGas_AmountIsNegative_NoChange()
    {
        LogAssert.Expect(UnityEngine.LogType.Error, "DestroyGas -- Amount can not be negative: " + -0.5f);
        notEmpty1.DestroyGas("gas", -0.5f);

        Assert.AreEqual(1.0f, notEmpty1.TotalGas);
    }

    [Test]
    public void DestroyGas_AmountIsAboveTotal_TotalGasDecreasesByAmountOfType()
    {
        notEmpty1.DestroyGas("gas", 1.5f);
        notEmpty2.CreateGas("otherGas", 1.0f, 1.0f);
        notEmpty2.DestroyGas("otherGas", 1.5f);

        Assert.AreEqual(0.0f, notEmpty1.TotalGas);
        Assert.AreEqual(2.0f, notEmpty2.TotalGas);
    }

    [Test]
    public void DestroyGas_AmountIsAboveTotal_GasOfTypeIsZero()
    {
        notEmpty1.DestroyGas("gas", 1.5f);

        Assert.AreEqual(0.0f, notEmpty1.GetGasAmount("gas"));
    }
    #endregion

    #region MoveGasTo
    [Test]
    public void MoveGasTo_DestinationIsNull_NoChange()
    {
        LogAssert.Expect(UnityEngine.LogType.Error, "MoveGasTo -- Destination can not be null");
        notEmpty1.MoveGasTo(null, 1.0f);

        Assert.AreEqual(1.0f, notEmpty1.TotalGas);
    }

    [Test]
    public void MoveGasTo_PositiveAmountBelowTotal_SourceTotalDecreasesByValue()
    {
        notEmpty1.MoveGasTo(empty1, 0.5f);

        Assert.AreEqual(0.5f, notEmpty1.TotalGas);
    }

    [Test]
    public void MoveGasTo_PositiveAmountBelowTotal_DestinationTotalIncreasesByValue()
    {
        notEmpty1.MoveGasTo(empty1, 0.5f);

        Assert.AreEqual(0.5f, empty1.TotalGas);
    }

    [Test]
    public void MoveGasTo_PositiveAmountBelowTotal_SourceGasOfTypeDecreasesByValue()
    {
        notEmpty1.MoveGasTo(empty1, 0.5f);

        Assert.AreEqual(0.5f, notEmpty1.GetGasAmount("gas"));
    }

    [Test]
    public void MoveGasTo_PositiveAmountBelowTotal_DestinationGasOfTypeIncreasesByValue()
    {
        notEmpty1.MoveGasTo(empty1, 0.5f);

        Assert.AreEqual(0.5f, empty1.GetGasAmount("gas"));
    }

    [Test]
    public void MoveGasTo_PositiveAmountBelowTotal_SourceTemperatureStaysTheSame()
    {
        notEmpty1.MoveGasTo(empty1, 0.5f);

        Assert.AreEqual(1.0f, notEmpty1.GetTemperature().InKelvin);
    }

    [Test]
    public void MoveGasTo_PositiveAmountBelowTotal_DestinationTemperatureChangesCorrectly()
    {
        notEmpty2.MoveGasTo(empty1, 0.5f);
        notEmpty2.MoveGasTo(notEmpty1, 0.5f);

        Assert.AreEqual(2.0f, empty1.GetTemperature().InKelvin);
        Assert.AreEqual(4.0f / 3.0f, notEmpty1.GetTemperature().InKelvin);
    }

    [Test]
    public void MoveGasTo_NegativeAmount_NoChange()
    {
        LogAssert.Expect(UnityEngine.LogType.Error, "MoveGasTo -- Amount can not be negative: " + -0.5f);
        notEmpty1.MoveGasTo(empty1, -0.5f);

        Assert.AreEqual(1.0f, notEmpty1.TotalGas);
    }

    [Test]
    public void MoveGasTo_AmountIsAboveTotal_SourceAmountIsZero()
    {
        notEmpty1.MoveGasTo(empty1, 1.5f);

        Assert.AreEqual(0.0f, notEmpty1.TotalGas);
    }

    [Test]
    public void MoveGasTo_AmountIsAboveTotal_DestinationAmountIncreasesBySourceTotal()
    {
        notEmpty1.MoveGasTo(empty1, 1.5f);

        Assert.AreEqual(1.0f, empty1.TotalGas);
    }
    #endregion
}
