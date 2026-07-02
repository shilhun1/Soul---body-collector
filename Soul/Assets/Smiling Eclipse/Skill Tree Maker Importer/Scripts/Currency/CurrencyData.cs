namespace SmilingEclipse.STMImporter
{
    using System;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Skill Tree Maker Converter/Skill Points")]
    public class CurrencyData : ScriptableObject
    {
        public bool saveAndLoad = true;
        public float basePoints;
        public float points;
        public string prefix = "Points: ";
        public string currencyName = "Points";

        public Action OnPointsChanged;

        public float Points { get { return points; } set { points = value; OnPointsChanged?.Invoke(); } }
        public void ResetSave()
        {
            if (saveAndLoad == false)
            {
                ResetPoints();
            }
        }
        public void ResetPoints()
        {
            points = basePoints;
        }

        public void AddPoints(float amount)
        {
            Points += amount;
        }
        public bool TrySpentPoints(float amount)
        {
            if (CanSpentPoints(amount))
            {
                SpentPoints(amount);
                return true;
            }
            return false;
        }
        public void SpentPoints(float amount)
        {
            Points -= amount;
            OnPointsChanged?.Invoke();
        }
        public bool CanSpentPoints(float amount)
        {
            return Points >= amount;
        }
    }
}