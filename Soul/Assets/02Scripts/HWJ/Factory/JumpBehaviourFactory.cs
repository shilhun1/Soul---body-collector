using UnityEngine; // GameObject와 Debug를 사용하기 위해 필요합니다.

public static class JumpBehaviourFactory // 점프 behaviourId를 실제 점프 컴포넌트로 연결합니다.
{
    public static JumpBehaviourBase Create(JumpJsonData data, GameObject owner) // 점프 데이터로 점프 컴포넌트를 생성합니다.
    {
        if (data == null) // 점프 데이터가 없으면 생성할 수 없습니다.
        {
            Debug.LogError("JumpJsonData is null."); // 에러 로그를 출력합니다.
            return null; // 생성 실패를 반환합니다.
        }

        string id = string.IsNullOrEmpty(data.behaviourId) ? data.id : data.behaviourId; // behaviourId가 비어 있으면 데이터 ID를 대신 사용합니다.
        return Create(id, owner); // 결정된 ID로 점프 컴포넌트를 생성합니다.
    }

    public static JumpBehaviourBase Create(string behaviourId, GameObject owner) // ID로 점프 컴포넌트를 생성합니다.
    {
        if (owner == null) // 컴포넌트를 붙일 대상이 없으면 생성할 수 없습니다.
        {
            Debug.LogError("JumpBehaviourFactory owner is null."); // 에러 로그를 출력합니다.
            return null; // 생성 실패를 반환합니다.
        }

        switch (behaviourId) // behaviourId에 따라 점프 방식을 선택합니다.
        {
            case "smooth_jump": // 부드러운 액션 게임용 점프입니다.
            case "player_default_jump": // 플레이어 기본 점프 ID입니다.
            case "skul_like_jump": // 스컬 느낌으로 튜닝할 때 사용할 수 있는 별칭입니다.
                return owner.AddComponent<SmoothJumpBehaviour>(); // 부드러운 점프 컴포넌트를 붙여 반환합니다.

            case "basic_jump": // 단순한 기본 점프입니다.
            case "beast_jump": // 야수형 빙의체 점프 ID 예시입니다.
                return owner.AddComponent<BasicJumpBehaviour>(); // 기본 점프 컴포넌트를 붙여 반환합니다.

            case "double_jump": // 더블 점프입니다.
            case "double_jump_data": // 더블 점프 데이터 ID입니다.
            case "bat_jump": // 박쥐형 빙의체 점프 ID 예시입니다.
                return owner.AddComponent<DoubleJumpBehaviour>(); // 더블 점프 컴포넌트를 붙여 반환합니다.

            default: // 등록되지 않은 ID입니다.
                Debug.LogError("Unknown jump behaviourId: " + behaviourId); // 알 수 없는 ID라는 에러를 출력합니다.
                return owner.AddComponent<SmoothJumpBehaviour>(); // 플레이 감각을 위해 부드러운 점프로 안전하게 대체합니다.
        }
    }
}
