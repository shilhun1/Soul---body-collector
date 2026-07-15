using System.Collections;
using UnityEngine;

public class hys_BossRoomSpawnPoint : MonoBehaviour
{
    // 포탈에서 전달한 ID가 일치하면 플레이어를 이 위치로 옮깁니다.
    [SerializeField] private string spawnId = "BossEntrance";
    [SerializeField] private int playerSearchFrameCount = 10;

    private IEnumerator Start()
    {
        if (!hys_BossPortalEntrance.ConsumeSpawnId(spawnId))
        {
            yield break;
        }

        int remainingFrames = Mathf.Max(1, playerSearchFrameCount);

        while (remainingFrames-- > 0)
        {
            Transform player = FindPlayer();

            if (player != null)
            {
                MovePlayer(player);
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("보스방 도착 지점에서 플레이어를 찾지 못했습니다.", this);
    }

    private void MovePlayer(Transform player)
    {
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();

        if (playerBody == null)
        {
            playerBody = player.GetComponentInChildren<Rigidbody2D>();
        }

        if (playerBody != null)
        {
            playerBody.position = transform.position;
            playerBody.linearVelocity = Vector2.zero;
        }
        else
        {
            player.position = transform.position;
        }
    }

    private static Transform FindPlayer()
    {
        HWJ_RootObjectDataResolver[] resolvers =
            FindObjectsByType<HWJ_RootObjectDataResolver>(FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        return taggedPlayer != null ? taggedPlayer.transform : null;
    }
}
