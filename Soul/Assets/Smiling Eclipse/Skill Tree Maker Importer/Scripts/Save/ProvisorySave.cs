namespace SmilingEclipse.STMImporter
{
    using UnityEngine;

    public static class ProvisorySave
    {
        public static bool saveAndLoad = true;
        public static void Save<T>(T value, string id, int index = 0)
        {
            if (saveAndLoad == false) { return; }
            string key = id + index;
            //Debug.Log("saved key: " + key);


            if (value is int intValue)
            {
                PlayerPrefs.SetInt(key, intValue);
            }
            else if (value is float floatValue)
            {
                PlayerPrefs.SetFloat(key, floatValue);
            }
            else if (value is string stringValue)
            {
                PlayerPrefs.SetString(key, stringValue);
            }
            else
            {
                string json = JsonUtility.ToJson(value);
                PlayerPrefs.SetString(key, json);
            }

            PlayerPrefs.Save();
        }

        public static T Load<T>(T defaultValue, string id, int index = 0)
        {
            string key = id + index;
            //Debug.Log("loaded key: " + key);
            if (saveAndLoad == false) { return defaultValue; }

            if (!PlayerPrefs.HasKey(key)) { return defaultValue; }


            // Checa tipo do T
            if (typeof(T) == typeof(int))
            {
                object val = PlayerPrefs.GetInt(key, (int)(object)defaultValue);
                return (T)val;
            }
            else if (typeof(T) == typeof(float))
            {
                object val = PlayerPrefs.GetFloat(key, (float)(object)defaultValue);
                return (T)val;
            }
            else if (typeof(T) == typeof(string))
            {
                object val = PlayerPrefs.GetString(key, (string)(object)defaultValue);
                return (T)val;
            }
            else
            {
                string json = PlayerPrefs.GetString(key, JsonUtility.ToJson(defaultValue));
                return JsonUtility.FromJson<T>(json);
            }
        }

        public static bool HasKey(string id, int index = 0)
        {
            string key = id + index;
            return PlayerPrefs.HasKey(key);
        }

        public static void Delete(string id, int index = 0)
        {
            string key = id + index;
            PlayerPrefs.DeleteKey(key);
        }

        public static void DeleteSave()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}