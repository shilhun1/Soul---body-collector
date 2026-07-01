using UnityEngine; // MonoBehaviour와 Mathf를 사용하기 위해 필요합니다.

public class PlayerStatController : MonoBehaviour // 플레이어 최종 능력치를 계산하고 CharacterBase에 적용합니다.
{
    public StatData baseStats; // 인스펙터에서 설정할 기본 능력치입니다.
    public StatData CurrentStats { get; private set; } // 현재 최종 능력치입니다.

    [SerializeField] private PlayerLevelRuntime levelRuntime; // 레벨 보너스를 계산할 때 사용할 컴포넌트입니다.

    private CharacterBase character; // 실제 체력과 현재 능력치를 가진 캐릭터 컴포넌트입니다.
    private StatData possessionBonus; // 빙의로 얻는 임시 능력치 보너스입니다.
    private StatData skillTreeBonus; // 스킬 트리로 얻는 능력치 보너스입니다.
    private StatData permanentOrbBonus = new StatData(); // 능력치 구슬로 얻는 영구 보너스입니다.

    private void Awake() // 오브젝트가 생성될 때 실행됩니다.
    {
        if (levelRuntime == null) // 레벨 런타임이 연결되지 않았다면
        {
            levelRuntime = GetComponent<PlayerLevelRuntime>(); // 같은 오브젝트에서 찾습니다.
        }

        character = GetComponent<CharacterBase>(); // 같은 오브젝트에서 CharacterBase를 찾습니다.
        RecalculateStats(); // 시작 시 능력치를 계산합니다.
    }

    public void AddExp(int exp) // 경험치를 추가하고 능력치를 갱신합니다.
    {
        if (levelRuntime == null) // 레벨 런타임이 없으면
        {
            return; // 처리하지 않습니다.
        }

        levelRuntime.AddExp(exp); // 경험치를 추가합니다.
        RecalculateStats(); // 레벨업 보너스가 바뀌었을 수 있으니 다시 계산합니다.
    }

    public void AddPermanentStatBonus(StatData bonus) // 능력치 구슬 보너스를 영구 추가합니다.
    {
        if (bonus == null) // 보너스가 없으면
        {
            return; // 처리하지 않습니다.
        }

        permanentOrbBonus.Add(bonus); // 영구 보너스에 더합니다.
        RecalculateStats(); // 최종 능력치를 다시 계산합니다.
    }

    public void SetPossessionBonus(StatData bonus) // 빙의 보너스를 설정합니다.
    {
        possessionBonus = bonus; // null이면 빙의 보너스를 제거하는 의미입니다.
        RecalculateStats(); // 최종 능력치를 다시 계산합니다.
    }

    public void SetSkillTreeBonus(StatData bonus) // 스킬 트리 보너스를 설정합니다.
    {
        skillTreeBonus = bonus; // 누적 스킬 트리 보너스를 저장합니다.
        RecalculateStats(); // 최종 능력치를 다시 계산합니다.
    }

    public void RecalculateStats() // 기본 능력치와 모든 보너스를 합산합니다.
    {
        int previousMaxHp = character != null && character.currentStats != null // 기존 최대 체력을 확인합니다.
            ? character.currentStats.maxHp // 기존 능력치가 있으면 이전 최대 체력을 사용합니다.
            : 0; // 없으면 0으로 처리합니다.

        CurrentStats = baseStats != null ? baseStats.Clone() : new StatData(); // 기본 능력치 복사본에서 시작합니다.

        if (levelRuntime != null) // 레벨 런타임이 있으면
        {
            for (int level = 2; level <= levelRuntime.CurrentLevel; level++) // 2레벨부터 현재 레벨까지 반복합니다.
            {
                LevelUpJsonData levelData = GameDataManager.Instance.GetLevelUp(level); // 해당 레벨 보너스 데이터를 가져옵니다.

                if (levelData != null) // 데이터가 있으면
                {
                    CurrentStats.Add(levelData.statBonus); // 레벨 보너스를 더합니다.
                }
            }
        }

        CurrentStats.Add(permanentOrbBonus); // 구슬 영구 보너스를 더합니다.
        CurrentStats.Add(skillTreeBonus); // 스킬 트리 보너스를 더합니다.
        CurrentStats.Add(possessionBonus); // 빙의 보너스를 더합니다.

        ApplyStatsToCharacter(previousMaxHp); // 계산된 능력치를 캐릭터에 적용합니다.
    }

    private void ApplyStatsToCharacter(int previousMaxHp) // 최종 능력치를 CharacterBase에 적용합니다.
    {
        if (character == null) // 캐릭터 컴포넌트가 없으면
        {
            return; // 적용하지 않습니다.
        }

        int hpIncrease = CurrentStats.maxHp - previousMaxHp; // 최대 체력 증가량을 계산합니다.
        character.currentStats = CurrentStats.Clone(); // 캐릭터 능력치에 복사본을 적용합니다.

        if (character.currentHp <= 0) // 현재 체력이 없으면
        {
            character.currentHp = CurrentStats.maxHp; // 최대 체력으로 초기화합니다.
        }
        else if (hpIncrease > 0) // 최대 체력이 증가했다면
        {
            character.currentHp = Mathf.Min(CurrentStats.maxHp, character.currentHp + hpIncrease); // 현재 체력도 증가량만큼 올립니다.
        }
        else // 최대 체력이 줄었거나 그대로라면
        {
            character.currentHp = Mathf.Min(character.currentHp, CurrentStats.maxHp); // 현재 체력이 최대 체력을 넘지 않게 제한합니다.
        }
    }
}
