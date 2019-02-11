#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Collections.Generic;
using System.Linq;
using Animation;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TraderPrototype : IPrototypable
{
    private float rarity;

    public string Type { get; set; }

    public List<string> PotentialNames { get; set; }

    public float MinCurrencyBalance { get; set; }

    public float MaxCurrencyBalance { get; set; }

    public string CurrencyName { get; set; }

    public float MinSaleMarginMultiplier { get; set; }

    public float MaxSaleMarginMultiplier { get; set; }

    public List<TraderPotentialInventory> PotentialStock { get; set; }

    public Dictionary<string, SpritenameAnimation> Animations { get; set; }

    /// <summary>
    /// Value from 0 to 1, higher value represent higher availability of the trade resource.
    /// </summary>
    public float Rarity
    {
        get
        {
            return rarity;
        }

        set
        {
            rarity = Mathf.Clamp(value, 0f, 1f);
        }
    }

    /// <summary>
    /// Reads the prototype from the specified JObject.
    /// </summary>
    /// <param name="jsonProto">The JProperty containing the prototype.</param>
    public void ReadJsonPrototype(JProperty jsonProto)
    {
        Type = jsonProto.Name;
        JToken innerJson = jsonProto.Value;
        PotentialNames = new List<string>(PrototypeReader.ReadJsonArray<string>(innerJson["PotentialNames"]));

        CurrencyName = PrototypeReader.ReadJson(CurrencyName, innerJson["CurrencyName"]);
        MinCurrencyBalance = PrototypeReader.ReadJson(MinCurrencyBalance, innerJson["MinCurrencyBalance"]);
        MaxCurrencyBalance = PrototypeReader.ReadJson(MaxCurrencyBalance, innerJson["MaxCurrencyBalance"]);
        MinSaleMarginMultiplier = PrototypeReader.ReadJson(MinSaleMarginMultiplier, innerJson["MinSaleMarginMultiplier"]);
        MaxSaleMarginMultiplier = PrototypeReader.ReadJson(MaxSaleMarginMultiplier, innerJson["MaxSaleMarginMultiplier"]);
        PotentialStock = PrototypeReader.ReadTraderPotentialInventory(innerJson["PotentialStock"]);

        Animations = PrototypeReader.ReadAnimations(innerJson["Animations"]);
    }
    
    /// <summary>
    /// Create a random Trader out of this TraderPrototype.
    /// </summary>
    public Trader CreateTrader()
    {
        Currency curency = PrototypeManager.Currency.Get(CurrencyName).Clone();
        curency.Balance = Random.Range(MinCurrencyBalance, MaxCurrencyBalance);

        Trader t = new Trader
        {
            Currency = curency,
            Name = PotentialNames[Random.Range(0, PotentialNames.Count - 1)],
            SaleMarginMultiplier = Random.Range(MinSaleMarginMultiplier, MaxSaleMarginMultiplier),
            Stock = new List<Inventory>()
        };

        foreach (TraderPotentialInventory potentialStock in PotentialStock)
        {
            bool itemIsInStock = Random.Range(0f, 1f) > potentialStock.Rarity;

            if (itemIsInStock)
            {
                if (!string.IsNullOrEmpty(potentialStock.Type))
                {
                    Inventory inventory = new Inventory(
                        potentialStock.Type,
                        Random.Range(potentialStock.MinQuantity, potentialStock.MaxQuantity));

                    t.Stock.Add(inventory);
                }
                else if (!string.IsNullOrEmpty(potentialStock.Category))
                {
                    List<Inventory> potentialObjects = GetInventoryCommonWithCategory(potentialStock.Category);

                    foreach (Inventory potentialObject in potentialObjects)
                    {
                        Inventory inventory = new Inventory(
                            potentialObject.Type,
                            Random.Range(potentialStock.MinQuantity, potentialStock.MaxQuantity));

                        t.Stock.Add(inventory);
                    }
                }
            }
        }

        return t;
    }

    private List<Inventory> GetInventoryCommonWithCategory(string category)
    {
        return PrototypeManager.Inventory.Values.Where(i => i.Category == category).ToList();
    }
}
