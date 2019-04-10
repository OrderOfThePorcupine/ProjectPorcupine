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
using Newtonsoft.Json.Linq;
using UnityEngine;

public enum ActionResult
{
    None,
    Abort,
    Cancel,
    Ignore,
    No,
    Accept,
    Yes,
    Quit,
    Exit,
    OK,
    Retry
}

public delegate void OnClose(ActionResult returnValue);

public struct Actionable
{
    public string Name { get; private set; }
    public ActionResult Return;

    public Actionable(ActionResult returnVal, string name = null)
    {
        this.Name = name ?? "button_" + returnVal.ToString().ToLower();
        this.Return = returnVal;
    }
}

public class DialogBoxActionBuilder : IEnumerable<Actionable>
{
    private List<Actionable> options = new List<Actionable>();

    public System.Collections.IEnumerator GetEnumerator()
    {
        return options.GetEnumerator();
    }

    /// <summary>
    /// Gets each option.
    /// </summary>
    /// <returns>Each option.</returns>
    IEnumerator<Actionable> IEnumerable<Actionable>.GetEnumerator()
    {
        foreach (Actionable option in options)
        {
            yield return option;
        }
    }

    public static DialogBoxActionBuilder YesCancel(string customYesText = null, string customCancelText = null)
    {
        return new DialogBoxActionBuilder().Yes(customYesText).Cancel(customCancelText);
    }

    public static DialogBoxActionBuilder YesNo(string customYesText = null, string customNoText = null)
    {
        return new DialogBoxActionBuilder().Yes(customYesText).No(customNoText);
    }

    public static DialogBoxActionBuilder YesNoCancel(string customYesText = null, string customNoText = null, string customCancelText = null)
    {
        return DialogBoxActionBuilder.YesNo().Cancel(customCancelText);
    }

    public static DialogBoxActionBuilder AcceptCancel(string customAcceptText = null, string customCancelText = null)
    {
        return new DialogBoxActionBuilder().Accept(customAcceptText).Cancel(customCancelText);
    }

    public DialogBoxActionBuilder Custom(ActionResult res, string text)
    {
        options.Add(new Actionable(res, text));
        return this;
    }

    public DialogBoxActionBuilder Abort(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Abort, customText));
        return this;
    }

    public DialogBoxActionBuilder Cancel(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Cancel, customText));
        return this;
    }

    public DialogBoxActionBuilder Ignore(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Ignore, customText));
        return this;
    }

    public DialogBoxActionBuilder No(string customText = null)
    {
        options.Add(new Actionable(ActionResult.No, customText));
        return this;
    }

    public DialogBoxActionBuilder Accept(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Accept, customText));
        return this;
    }

    public DialogBoxActionBuilder Yes(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Yes, customText));
        return this;
    }

    public DialogBoxActionBuilder OK(string customText = null)
    {
        options.Add(new Actionable(ActionResult.OK, customText));
        return this;
    }

    public DialogBoxActionBuilder Retry(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Retry, customText));
        return this;
    }

    public DialogBoxActionBuilder Quit(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Quit, customText));
        return this;
    }

    public DialogBoxActionBuilder Exit(string customText = null)
    {
        options.Add(new Actionable(ActionResult.Exit, customText));
        return this;
    }
}
