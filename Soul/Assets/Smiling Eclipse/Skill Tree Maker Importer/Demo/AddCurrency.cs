using SmilingEclipse.STMImporter;
using System.Collections.Generic;
using UnityEngine;
namespace SmilingEclipse.STMImporter
{
    public class AddCurrency : MonoBehaviour
    {
        public CurrencyData currencyData;
        public float value = 1f;



        public void Add()
        {
            currencyData.AddPoints(value);

        }
    }
}