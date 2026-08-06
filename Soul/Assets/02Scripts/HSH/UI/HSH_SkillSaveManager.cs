using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SmilingEclipse.STMImporter;

[Serializable]
public class HSH_NodeLevelEntry
    {
        public string nodeName;
        public int nodeIndex;
        public int level;
        public bool isUnlocked;
    }

    [Serializable]
    public class HSH_SkillSaveData
    {
        public List<string> unlockedNodeIds = new List<string>();
        public List<string> unlockedSkillIds = new List<string>();
        public int skillPoint = 0;
        public List<HSH_NodeLevelEntry> nodeLevels = new List<HSH_NodeLevelEntry>();
    }

    /// <summary>
    /// HSH 전용 스킬트리 및 스킬 해금/포인트 데이터의 저장과 로드를 관리하는 매니저 클래스입니다.
    /// persistentDataPath의 JSON 저장소와 연동됩니다.
    /// </summary>
    public class HSH_SkillSaveManager : MonoBehaviour
    {
        public static HSH_SkillSaveManager Instance { get; private set; }

        [Header("저장 설정")]
        [SerializeField] private string saveFileName = "hsh_skill_save.json";
        [SerializeField] private bool autoLoadOnStart = true;
        [SerializeField] private bool autoSaveOnQuit = true;

        [Header("UI 참조 (선택)")]
        [SerializeField] private SkillTreeController skillTreeController;
        [SerializeField] private CurrencyData currencyData;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (skillTreeController == null)
            {
                skillTreeController = FindFirstObjectByType<SkillTreeController>();
            }
        }

        private void Start()
        {
            if (autoLoadOnStart)
            {
                LoadSkillData();
            }
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnQuit)
            {
                SaveSkillData();
            }
        }

        /// <summary>
        /// 현재 스킬 해금 상태, 스킬 포인트, 노드 레벨 데이터를 디스크에 저장합니다.
        /// </summary>
        public void SaveSkillData()
        {
            HSH_SkillSaveData saveData = new HSH_SkillSaveData();

            // 1. HWJ SkillUnlockSystem 데이터 수집
            var hwjUnlock = FindFirstObjectByType<HWJ_SkillUnlockSystem>();
            if (hwjUnlock != null)
            {
                saveData.unlockedNodeIds = new List<string>(hwjUnlock.GetUnlockedSkillNodeIds());
                saveData.unlockedSkillIds = new List<string>(hwjUnlock.GetUnlockedSkillIds());
            }

            // 2. 스킬 포인트 데이터 수집
            var levelUpSystem = FindFirstObjectByType<HWJ_LevelUpSystem>();
            if (levelUpSystem != null)
            {
                saveData.skillPoint = levelUpSystem.SkillPoint;
            }
            else if (currencyData != null)
            {
                saveData.skillPoint = Mathf.RoundToInt(currencyData.Points);
            }

            // 3. UI 씬 노드별 레벨 및 상태 데이터 수집
            SkillNode[] nodes = FindObjectsByType<SkillNode>(FindObjectsSortMode.None);
            foreach (var node in nodes)
            {
                if (node == null || node.NodeData == null) continue;

                HSH_NodeLevelEntry entry = new HSH_NodeLevelEntry
                {
                    nodeName = node.NodeData.skillName,
                    nodeIndex = node.NodeData.nodeIndex,
                    level = node.level,
                    isUnlocked = node.UnlockedInfo != null && node.UnlockedInfo.isUnlocked
                };
                saveData.nodeLevels.Add(entry);
            }

            // 4. JSON 파일 저장
            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[HSH_SkillSaveManager] 스킬 데이터 저장 완료 ({SaveFilePath})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HSH_SkillSaveManager] 스킬 데이터 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 디스크에 저장된 스킬 데이터를 불러와 런타임 및 UI 시스템에 적용합니다.
        /// </summary>
        public void LoadSkillData()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log("[HSH_SkillSaveManager] 기존 스킬 저장 파일이 존재하지 않습니다.");
                return;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                HSH_SkillSaveData saveData = JsonUtility.FromJson<HSH_SkillSaveData>(json);

                if (saveData == null) return;

                // 1. HWJ SkillUnlockSystem 해금 데이터 복원
                var hwjUnlock = FindFirstObjectByType<HWJ_SkillUnlockSystem>();
                if (hwjUnlock != null)
                {
                    hwjUnlock.RestoreUnlockedSkills(saveData.unlockedSkillIds, saveData.unlockedNodeIds);
                }

                // 2. 스킬 포인트 복원
                var levelUpSystem = FindFirstObjectByType<HWJ_LevelUpSystem>();
                if (levelUpSystem != null)
                {
                    levelUpSystem.RestoreProgress(levelUpSystem.CurrentLevel, levelUpSystem.CurrentExperience, saveData.skillPoint);
                }

                if (currencyData != null)
                {
                    currencyData.Points = saveData.skillPoint;
                }

                // 3. UI 트리의 노드 레벨 및 상태 복원 (활성화 상태일 때만 UI 코루틴 재로드)
                if (skillTreeController == null)
                {
                    skillTreeController = FindFirstObjectByType<SkillTreeController>();
                }

                if (skillTreeController != null && skillTreeController.gameObject.activeInHierarchy)
                {
                    skillTreeController.StartCoroutine(skillTreeController.Load());
                }

                Debug.Log("[HSH_SkillSaveManager] 스킬 데이터 로드 및 복원 완료.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HSH_SkillSaveManager] 스킬 데이터 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 저장된 파일 및 런타임 스킬 해금 데이터를 완전히 초기화합니다.
        /// </summary>
        public void ResetSkillSaveData()
        {
            // 1. 저장 파일 삭제
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[HSH_SkillSaveManager] 저장된 스킬 파일이 삭제되었습니다.");
            }

            // 2. HWJ 시스템 런타임 스킬 초기화
            var hwjUnlock = FindFirstObjectByType<HWJ_SkillUnlockSystem>();
            if (hwjUnlock != null)
            {
                hwjUnlock.ClearAllUnlocks(refundSkillPoints: true);
            }

            // 3. 포인트 동기화
            var levelUpSystem = FindFirstObjectByType<HWJ_LevelUpSystem>();
            if (levelUpSystem != null && currencyData != null)
            {
                currencyData.Points = levelUpSystem.SkillPoint;
            }

            // 4. UI 트리 새로고침 (활성화 상태일 때만 UI 코루틴 재로드)
            if (skillTreeController == null)
            {
                skillTreeController = FindFirstObjectByType<SkillTreeController>();
            }

            if (skillTreeController != null && skillTreeController.gameObject.activeInHierarchy)
            {
                skillTreeController.StartCoroutine(skillTreeController.Load());
            }

            Debug.Log("[HSH_SkillSaveManager] 스킬 데이터 리셋 완료.");
        }
    }
