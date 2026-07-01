using UnityEngine; // GameObject와 Debug를 사용하기 위해 필요합니다.

public static class PossessionFactory // 빙의 behaviourId를 실제 빙의 컴포넌트로 연결합니다.
{
    public static PossessionBase Create(string behaviourId, GameObject owner) // 빙의 행동 컴포넌트를 생성합니다.
    {
        if (owner == null) // 컴포넌트를 붙일 대상이 없으면
        {
            Debug.LogError("PossessionFactory owner is null."); // 오류를 출력합니다.
            return null; // 실패를 반환합니다.
        }

        switch (behaviourId) // behaviourId에 따라 생성할 빙의 행동을 선택합니다.
        {
            case "basic_possession": // 기본 빙의입니다.
                return owner.AddComponent<BasicPossession>(); // 기본 빙의 컴포넌트를 붙이고 반환합니다.

            default: // 등록되지 않은 ID입니다.
                Debug.LogError("Unknown possession behaviourId: " + behaviourId); // 오류를 출력합니다.
                return null; // 실패를 반환합니다.
        }
    }
}
