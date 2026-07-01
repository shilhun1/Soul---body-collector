using UnityEngine; // GameObject와 Debug를 사용하기 위해 필요합니다.

public static class BossPatternFactory // 보스 패턴 ID를 실제 보스 패턴 컴포넌트로 연결합니다.
{
    public static BossPatternBase Create(string patternId, GameObject owner) // 보스 패턴 컴포넌트를 생성합니다.
    {
        if (owner == null) // 컴포넌트를 붙일 대상이 없으면
        {
            Debug.LogError("BossPatternFactory owner is null."); // 오류를 출력합니다.
            return null; // 실패를 반환합니다.
        }

        Debug.LogError("Unknown boss patternId: " + patternId); // 아직 등록된 보스 패턴이 없다는 로그입니다.
        return null; // 현재는 보스 패턴 생성 실패를 반환합니다.
    }
}
