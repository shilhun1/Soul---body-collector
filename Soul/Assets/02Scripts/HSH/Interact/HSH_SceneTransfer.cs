using System.Collections.Generic;

public static class HSH_SceneTransfer
{
    // 다음 씬에서 플레이어가 스폰될 위치의 고유 ID
    public static string TargetSpawnID = "";

    // 씬 간에 전달하고 싶은 추가 데이터들을 자유롭게 저장할 수 있는 공간
    // 예: HSH_SceneTransfer.TransferData["Score"] = 100;
    public static Dictionary<string, object> TransferData = new Dictionary<string, object>();
}
