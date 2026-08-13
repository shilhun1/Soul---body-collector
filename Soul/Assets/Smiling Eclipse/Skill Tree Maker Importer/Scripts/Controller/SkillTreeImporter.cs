#if UNITY_EDITOR
namespace SmilingEclipse.STMImporter
{
    using Newtonsoft.Json.Linq;
    
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    public class SkillTreeImporter : EditorWindow
    {
        private string jsonPath;
        private DefaultAsset folderAsset; // arrasta a pasta aqui
        private Dictionary<string, SkillNodeData> skillNodeMap = new Dictionary<string, SkillNodeData>();
        private JArray skillsArray;
        private string skillTreeName = "";


        private string FolderPath => AssetDatabase.GetAssetPath(folderAsset);
        private string ScriptablesPath => FolderPath + "/Skill Trees";
        private string IconsPath => FolderPath + "/Icons";
        private string dataPath;
        private string databasePath;

        private const string JsonPathKey = "SkillTreeMakerImporter_JSONPath";
        private const string FolderAssetKey = "SkillTreeMakerImporter_FolderAsset";

        [MenuItem("Tools/Skill Tree Maker Importer")]
        public static void ShowWindow()
        {
            GetWindow<SkillTreeImporter>("Skill Tree Maker Importer");
        }
        [MenuItem("Tools/Clear PlayerPrefs",priority = 9999)]
        public static void ClearPlayerPrefs()
        {
            ProvisorySave.DeleteSave();
        }
        private void OnEnable()
        {
            // Restore JSON path
            jsonPath = EditorPrefs.GetString(JsonPathKey, "");

            // Restore folder asset (by GUID)
            string folderGuid = EditorPrefs.GetString(FolderAssetKey, "");
            if (!string.IsNullOrEmpty(folderGuid))
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);
                folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
            }
        }
        private void OnDisable()
        {
            // Save JSON path
            if (!string.IsNullOrEmpty(jsonPath))
                EditorPrefs.SetString(JsonPathKey, jsonPath);
            else
                EditorPrefs.DeleteKey(JsonPathKey);

            // Save folder asset as GUID
            if (folderAsset != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(folderAsset));
                EditorPrefs.SetString(FolderAssetKey, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(FolderAssetKey);
            }
        }
        private void OnGUI()
        {
            GUILayout.Label("Import Skill Tree From JSON", EditorStyles.boldLabel);

            if (GUILayout.Button("Select JSON"))
                jsonPath = EditorUtility.OpenFilePanel("Select the JSON", "", "json");

            EditorGUILayout.LabelField("JSON:", jsonPath);

            folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Importer Folder:",
                folderAsset, typeof(DefaultAsset), false);
            if (folderAsset == null || String.IsNullOrEmpty(jsonPath)) { return; }

            if (GUILayout.Button("Import Skills") && !string.IsNullOrEmpty(jsonPath))
            {
                if (folderAsset == null)
                {
                    Debug.LogError("Folder not selected or null, please fix");
                    return;
                }
                databasePath = "";
                dataPath = "";
                skillNodeMap.Clear();
                TryCreateFolders();

                AssetCreation();
            }
        }
        private void TryCreateFolders()
        {
            string json = File.ReadAllText(jsonPath);
            JObject root = JObject.Parse(json);
            skillTreeName = root["Class Name"]?.ToString().Trim();

            skillTreeName = Regex.Replace(skillTreeName ?? string.Empty, @"[^\w\s\-]", "");

            // Se ficar vazio, define nome padr�o
            if (string.IsNullOrEmpty(skillTreeName))
            {
                skillTreeName = "Unnamed Skill Tree";
            }

            string skillTreePath = "";
            bool skillTreeFolderCreated = CreateFolderIfMissing(ScriptablesPath, skillTreeName, out skillTreePath);
            Debug.Log(skillTreePath);
            bool databaseRes = CreateFolderIfMissing(skillTreePath, "Database", out databasePath);
            bool dataRes = CreateFolderIfMissing(skillTreePath, "Data", out dataPath);
            Debug.Log($"Path: {skillTreePath} data result: {databaseRes} database result: {databaseRes}");



        }
        void AssetCreation()
        {
            if (String.IsNullOrEmpty(skillTreeName)) { Debug.LogError("Skill Tree Name is null or empty, please fix"); return; }
            string json = File.ReadAllText(jsonPath);
            JObject root = JObject.Parse(json);
            skillsArray = (JArray)root["Skills"];

            List<SkillNodeData> datas = new();

            // Criar os Datas
            int index = 0;
            foreach (var skillToken in skillsArray)
            {
                string name = skillToken["Skill Name"]?.ToString();
                string desc = skillToken["Skill Description"]?.ToString();
                int maxPoints = skillToken["Skill Max Points"]?.ToObject<int>() ?? 0;
                int currentPoints = skillToken["Skill Current Points"]?.ToObject<int>() ?? 0;
                string iconPath = skillToken["Skill Image"]?.ToString();
                JToken positionToken = skillToken["Skill Position Exact"];
                Vector2 position = Vector2.zero;

                if (positionToken != null)
                {
                    float x = positionToken["x"]?.ToObject<float>() ?? 0;
                    float y = positionToken["y"]?.ToObject<float>() ?? 0;
                    position = new Vector2(x, y);
                }
                string dataAssetPath = (Path.Combine(dataPath, $"Node Data_{name}"));
                SkillNodeData data = null;
                if (AssetExists(dataAssetPath) == false) { data = ScriptableObject.CreateInstance<SkillNodeData>(); }
                else { data = AssetDatabase.LoadAssetAtPath<SkillNodeData>(dataAssetPath); }


                datas.Add(data);
                data.nodeIndex = index;
                data.skillName = name;
                data.description = desc;
                data.maxLevel = maxPoints;
                data.startLevel = currentPoints;
                data.position = position;

                if (!string.IsNullOrEmpty(iconPath))
                {
                    string assetIconPath = IconsPath + iconPath;
                    Debug.Log($"{IconsPath}-> asset icon path: {assetIconPath}");
                    if (AssetExists(assetIconPath))
                    {
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetIconPath);
                        if (sprite == null)
                        {
                            // Caso seja sprite-sheet ou texture importada como Texture2D
                            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetIconPath);
                            foreach (var o in all)
                            {
                                if (o is Sprite s)
                                {
                                    sprite = s;
                                    break;
                                }
                            }
                        }

                        if (sprite != null)
                        {
                            data.icon = sprite;
                            EditorUtility.SetDirty(data);
                        }
                        else
                        {
                            Debug.LogError($"N�o foi poss�vel carregar sprite de: {assetIconPath}");
                        }
                    }
                    index++;
                }

                //string safeName = name.Replace("/", "_");
                if (!AssetExists(dataAssetPath)) { CreateData(data); }
                skillNodeMap[name] = data;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //-----------------------------

            //    ###### Part 2 ######

            //-----------------------------

            // Resolver pr�-requisitos e depend�ncias
            foreach (var skillToken in skillsArray)
            {
                string name = skillToken["Skill Name"]?.ToString();
                SkillNodeData data = skillNodeMap[name];

                // Pr�-requisitos
                List<SkillNodeData> prereqList = new List<SkillNodeData>();
                string prerequisiteName = skillToken["Skill Prerequisite"]?.ToString();
                if (!string.IsNullOrEmpty(prerequisiteName) && prerequisiteName != "-")
                {
                    string[] prereqs = prerequisiteName.Split(',');
                    foreach (var p in prereqs)
                    {
                        string clean = p.Split('(')[0].Trim();
                        if (skillNodeMap.TryGetValue(clean, out var prereqAsset))
                            prereqList.Add(prereqAsset);
                    }
                }
                data.parentNodes = prereqList.ToList();

                // Depend�ncias
                var deps = skillToken["Skill Dependencies"] as JArray;
                if (deps != null && deps.Count > 0)
                {
                    List<SkillNodeData> depList = new List<SkillNodeData>();
                    foreach (var depToken in deps)
                    {
                        string depName = depToken.ToString().Split('(')[0].Trim();
                        if (skillNodeMap.TryGetValue(depName, out var depSkill))
                            depList.Add(depSkill);
                    }
                    data.childNodes = depList.ToList();
                }

                EditorUtility.SetDirty(data);
            }
            SkillNodeDatabase database = CreateDatabase(datas);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Importa��o conclu�da! ScriptableObjects salvos em: {dataPath}");
        }

        // ======================
        // Helpers para legibilidade
        // ======================

        void CreateFolder(string path, string folderName)
        {
            AssetDatabase.CreateFolder(path, folderName);
        }

        bool CreateFolderIfMissing(string path, string folderName, out string fullPath)
        {
            fullPath = $"{path}/{folderName}";
            if (FolderExists(fullPath) == false)
            {
                Debug.Log($"Criou Pasta: {folderName} no path: {path} entao o fullPath � {fullPath}");
                CreateFolder(path, folderName);
                return true;
            }
            return false;
        }

        void DeleteFolderIfExist(string path, string folderName)
        {
            string fullPath = $"{path}/{folderName}";
            if (FolderExists(fullPath))
            {
                FileUtil.DeleteFileOrDirectory(fullPath);
                FileUtil.DeleteFileOrDirectory(fullPath + ".meta");
                AssetDatabase.Refresh();
            }
        }

        void CreateAsset(UnityEngine.Object asset, string path, string assetName)
        {
            string fullPath = $"{path}/{assetName}.asset";
            AssetDatabase.CreateAsset(asset, fullPath);
        }

        bool FolderExists(string path)
        {
            return AssetDatabase.AssetPathExists(path);
        }
        bool AssetExists(string path)
        {
            return AssetDatabase.AssetPathExists(path);
        }

        SkillNodeData CreateData(SkillNodeData data)
        {
            CreateAsset(data, dataPath, $"Node Data_{data.skillName}");
            return data;
        }
        SkillNodeDatabase CreateDatabase(List<SkillNodeData> datas)
        {
            SkillNodeDatabase database = null;
            string databaseAssetPath = Path.Combine(databasePath, $"Database_{skillTreeName}");
            if (AssetExists(databaseAssetPath) == false) { database = ScriptableObject.CreateInstance<SkillNodeDatabase>(); }
            else { database = AssetDatabase.LoadAssetAtPath<SkillNodeDatabase>(databaseAssetPath); database.datas.Clear(); }
            database.skillTreeName = skillTreeName;
            database.datas = datas.ToList();
            if (AssetExists(databaseAssetPath) == false) { CreateAsset(database, databasePath, $"Database_{skillTreeName}"); }
            return database;
        }

        private Sprite Tex2DToSprite(Texture2D tex, float pixelsPerUnit = 100f)
        {
            if (tex == null)
            {
                Debug.LogError("Tex2DToSprite: input Texture2D is null.");
                return null;
            }

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = tex.name; // define um nome �til
            return sprite;
        }

    }
}
#endif

