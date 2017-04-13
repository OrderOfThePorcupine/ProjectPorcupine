#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MoonSharp.Interpreter;
using ProjectPorcupine.Rooms;
using UnityEngine;

/// <summary>
/// An atmosphere component which controls gas and temperature.
/// </summary>
[MoonSharpUserData]
public class AtmosphereComponent
{
    /// <summary>
    /// All the gasses.  Accessible by gas name.
    /// </summary>
    private Dictionary<string, float> gasses;

    /// <summary>
    /// The internal temperature storage unit.
    /// </summary>
    private TemperatureValue internalTemperature;

    public AtmosphereComponent()
    {
        TotalGas = 0;
        internalTemperature = new TemperatureValue(0);
        gasses = new Dictionary<string, float>();
    }

    /// <summary>
    /// The total gas amount.
    /// </summary>
    public float TotalGas { get; private set; }

    #region Gas

    /// <summary>
    /// Gets the amount of this gas stored in this component.
    /// </summary>
    /// <returns>The amount of this gas stored in the component.</returns>
    /// <param name="gasName">Gas you want the pressure of.</param>
    public float GetGasAmount(string gasName)
    {
        return gasses.ContainsKey(gasName) ? gasses[gasName] : 0;
    }

    /// <summary>
    /// Gets the fraction of the gas present that is this type of gas.
    /// </summary>
    /// <returns>The fraction of this type of gas.</returns>
    /// <param name="gasName">Name of the gas.</param>
    public float GetGasFraction(string gasName)
    {
        return TotalGas > 0 ? GetGasAmount(gasName) / TotalGas : 0.0f;
    }

    /// <summary>
    /// Get the names of gasses present in this component.
    /// TODO: Not efficient enough.
    /// </summary>
    /// <returns>The names of gasses present.</returns>
    public string[] GetGasNames()
    {
        return gasses.Keys.ToArray();
    }

    /// <summary>
    /// Sets the amount of gas of this type to the value.
    /// </summary>
    /// <param name="gasName">The name of the gas whose value is set.</param>
    /// <param name="amount">The amount of gas to set it to.</param>
    public void SetGas(string gasName, float newValue)
    {
        float amount = newValue - GetGasAmount(gasName);
        ChangeGas(gasName, amount);
        ChangeTemperature((amount * internalTemperature.InKelvin) / TotalGas);
        ////UnityDebugger.Debugger.Log("Atmosphere", "Setting " + gasName + ". New value is " + GetGasAmount(gasName));
    }

    /// <summary>
    /// Sets the total gas value then evenly spreads it.
    /// </summary>
    /// <param name="newValue"> The new value for the total gas to be. </param>
    public void SetGas(float newValue)
    {
        float amount = newValue - TotalGas;
        foreach (var gasName in GetGasNames())
        {
            ChangeGas(gasName, amount * GetGasFraction(gasName));
        }

        ChangeTemperature((-amount * internalTemperature.InKelvin) / TotalGas);
    }

    /// <summary>
    /// Update a gas with a new amount.
    /// </summary>
    /// <param name="gasName"> The gas to update. </param>
    /// <param name="amount"> The amount to update by. </param>
    public void ChangeGas(string gasName, float amount)
    {
        if (gasses.ContainsKey(gasName) == false)
        {
            gasses[gasName] = 0;
        }

        if (gasses[gasName] <= -amount)
        {
            TotalGas -= gasses[gasName];
            gasses.Remove(gasName);
        }
        else
        {
            TotalGas += amount;
            gasses[gasName] += amount;
        }
    }

    /// <summary>
    /// Creates gas of a determined type and temperature out of nowhere. This should only be used when there is no source for the gas. Otherwise use MoveGasTo.
    /// </summary>
    /// <param name="gasName">Name of the gas to create.</param>
    /// <param name="GetGasAmount">Amount of gas to create.</param>
    /// <param name="temperature">Temperature of the gas.</param>
    public void CreateGas(string gasName, float amount, float temperature)
    {
        if (amount < 0 || temperature < 0)
        {
            UnityDebugger.Debugger.LogError("CreateGas -- Amount or temperature can not be negative: " + amount + ", " + temperature);
            return;
        }

        ChangeGas(gasName, amount);

        ChangeTemperature((amount * internalTemperature.InKelvin) / TotalGas);
    }

    /// <summary>
    /// Destroys gas evenly. This should only be used when there is no destination for the gas. Otherwise use MoveGasTo.
    /// </summary>
    /// <param name="amount">Amount to destroy.</param>
    public void DestroyGas(float amount)
    {
        if (amount < 0)
        {
            UnityDebugger.Debugger.LogError("DestroyGas -- Amount can not be negative: " + amount);
            return;
        }

        amount = Mathf.Min(TotalGas, amount);
        foreach (var gasName in GetGasNames())
        {
            ChangeGas(gasName, -amount * GetGasFraction(gasName));
        }

        ChangeTemperature((-amount * internalTemperature.InKelvin) / TotalGas);
    }

    /// <summary>
    /// Destroys gas of this type. This should only be used when there is no destination for the gas. Otherwise use MoveGasTo.
    /// </summary>
    /// <param name="gasName">Name of gas to destroy.</param>
    /// <param name="amount">Amount to destroy.</param>
    public void DestroyGas(string gasName, float amount)
    {
        if (amount < 0)
        {
            UnityDebugger.Debugger.LogError("DestroyGas -- Amount can not be negative: " + amount);
            return;
        }

        ChangeGas(gasName, -Mathf.Min(GetGasAmount(gasName), amount));
        ChangeTemperature((-amount * internalTemperature.InKelvin) / TotalGas);
    }

    /// <summary>
    /// Moves gas to another atmosphere. Temperature is transferred accordingly.
    /// </summary>
    /// <param name="destination">Destination of the gas.</param>
    /// <param name="amount">Amount of gas to move.</param>
    public void MoveGasTo(AtmosphereComponent destination, float amount)
    {
        if (destination == null)
        {
            UnityDebugger.Debugger.LogError("MoveGasTo -- Destination can not be null");
            return;
        }

        if (amount < 0 || float.IsNaN(amount))
        {
            UnityDebugger.Debugger.LogError("MoveGasTo -- Amount can not be negative: " + amount);
            return;
        }

        amount = Mathf.Min(this.TotalGas, amount);

        string[] gasNames = this.GetGasNames();
        for (int i = 0; i < gasNames.Length; i++)
        {
            float partialAmount = amount * GetGasFraction(gasNames[i]);
            this.ChangeGas(gasNames[i], -partialAmount);
            destination.ChangeGas(gasNames[i], partialAmount);
        }

        // The amount lost
        // Equation E = T * ∑G
        // Therefore ΔE = ΔT * Δ∑G
        // Therefore ΔE = amount * current temperature
        // And we can get the temperature amount by incrementing it by ΔE/∑G for the source and destination.
        float deltaTemperature = amount * this.internalTemperature.InKelvin;
        this.ChangeTemperature(-deltaTemperature / this.TotalGas);
        destination.ChangeTemperature(deltaTemperature / destination.TotalGas);
    }

    public void MoveGasTo(AtmosphereComponent destination, string gasName, float amount)
    {
        if (amount < 0)
        {
            UnityDebugger.Debugger.LogError("MoveGasTo -- Amount can not be negative: " + amount);
            return;
        }

        amount = Mathf.Min(this.GetGasAmount(gasName), amount);
        this.ChangeGas(gasName, -amount);
        destination.ChangeGas(gasName, amount);

        // The amount lost
        // Equation E = T * ∑G
        // Therefore ΔE = ΔT * Δ∑G
        // Therefore ΔE = amount * current temperature
        // And we can get the temperature amount by incrementing it by ΔE/∑G for the source and destination.
        float deltaTemperature = amount * this.internalTemperature.InKelvin;
        this.ChangeTemperature(-deltaTemperature / this.TotalGas);
        destination.ChangeTemperature(deltaTemperature / destination.TotalGas);
    }
    #endregion

    #region Temperature

    /// <summary>
    /// Gets the temperature value for this room.
    /// </summary>
    /// <returns> The temperature value as <see cref="TemperatureValue"/>. </returns>
    public TemperatureValue GetTemperature()
    {
        return TotalGas.IsZero() ? TemperatureValue.AbsoluteZero : internalTemperature;
    }

    /// <summary>
    /// Sets the temperature to a specific value.
    /// </summary>
    /// <param name="temperature">Temperature.</param>
    public void SetTemperature(float newTemperatureInKelvin)
    {
        internalTemperature.InKelvin = newTemperatureInKelvin;
    }

    /// <summary>
    /// Changes the energy.
    /// </summary>
    /// <param name="amount">The amount of energy added or removed from the total.</param>
    public void ChangeTemperature(float amountInKelvin)
    {
        internalTemperature.InKelvin += amountInKelvin;
    }
    #endregion
}