using UnityEngine;

public class HSH_SpawnPoint : MonoBehaviour
{
    [Header("스폰 포인트 고유 ID")]
    public string spawnID = "StartPoint1";

    private void Start()
    {
        // 씬 시작 시, 전달받은 목적지 ID가 자신과 일치한다면 플레이어를 이쪽으로 스폰시킴
        if (!string.IsNullOrEmpty(HSH_SceneTransfer.TargetSpawnID) && 
            HSH_SceneTransfer.TargetSpawnID == spawnID)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = transform.position;
                Debug.Log($"[HSH_SpawnPoint] 플레이어가 {spawnID} 위치로 이동되었습니다.");
            }
            else
            {
                Debug.LogWarning("[HSH_SpawnPoint] Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
            }
            
            // 데이터 소비 후 초기화 (다른 씬 이동 시 꼬임 방지)
            HSH_SceneTransfer.TargetSpawnID = "";
        }
    }

    private void OnDrawGizmos()
    {
        // 에디터 상에서 스폰 위치를 눈으로 쉽게 확인하기 위한 시각적 가이드 (초록색 원)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
