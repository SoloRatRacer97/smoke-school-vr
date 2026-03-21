using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FooterYearText : MonoBehaviour
{
    private void Start()
    {
        GetComponent<TextMeshProUGUI>().text = $"Smoke School {DateTime.Now.Year} | smokeschoolvr.com";
    }
}
