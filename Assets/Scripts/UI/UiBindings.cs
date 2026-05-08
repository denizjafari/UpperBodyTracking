using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRRehab.UI
{
    /// <summary>
    /// Tiny adapters that expose Unity UI events as plain C# events with the
    /// values pre-extracted. Saves every controller from re-writing the same
    /// "AddListener → cast → null-check" boilerplate.
    /// </summary>
    public static class UiBindings
    {
        public static void OnClick(this Button btn, Action handler)
        {
            if (btn == null || handler == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => handler());
        }

        public static void OnToggled(this Toggle t, Action<bool> handler)
        {
            if (t == null || handler == null) return;
            t.onValueChanged.RemoveAllListeners();
            t.onValueChanged.AddListener(v => handler(v));
        }

        public static void OnChanged(this TMP_Dropdown dd, Action<int> handler)
        {
            if (dd == null || handler == null) return;
            dd.onValueChanged.RemoveAllListeners();
            dd.onValueChanged.AddListener(i => handler(i));
        }
    }
}
