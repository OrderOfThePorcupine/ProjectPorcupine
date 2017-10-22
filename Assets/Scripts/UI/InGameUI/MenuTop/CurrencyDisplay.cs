#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class CurrencyDisplay : MonoBehaviour
{
    private Text text;
    private string[] currencies;

    private void Awake()
    {
        text = GetComponent<Text>();
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "_World")
        {
            gameObject.SetActive(false);
            return;
        }

        currencies = GameController.CurrentWorld.Wallet.GetCurrencyNames();
    }

    // Update is called once per frame
    private void Update()
    {
        StringBuilder content = new StringBuilder();

        // Populate the text box
        foreach (string currency in currencies)
        {
            content.Append(currency + ":");
            content.Append(GameController.CurrentWorld.Wallet[currency].Balance);
            content.AppendLine();
        }

        text.text = content.ToString();
    }
}
