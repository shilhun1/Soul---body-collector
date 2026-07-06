using System.Collections;
using UnityEngine;

public class HSH_SpikeTrap : MonoBehaviour
{
    [Header("고정형(가시) 함정 설정")]
    public float damage = 20f; // 데미지 수치
    
    [Header("감지 및 돌출 설정")]
    public float detectionDistance = 3f; // 위로 플레이어를 감지할 거리
    public float delayTime = 0.7f; // 감지 후 솟아오르기까지 걸리는 시간 (0.7초)
    public float protrudeHeight = 1f; // 위로 솟아오르는 높이
    public float activeDuration = 1.5f; // 솟아오른 상태를 유지하는 시간

    private bool isAttacking = false;
    private bool isProtruding = false; // 현재 튀어나와 있어서 데미지를 줄 수 있는 상태인지
    private bool hasDamagedThisAttack = false; // 한 번 솟아오를 때 여러 번 데미지 안 받게 방지
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position;
    }

    private void Update()
    {
        // 공격 중이 아닐 때만 플레이어를 감지합니다.
        if (!isAttacking)
        {
            // 위쪽으로 레이캐스트를 쏴서 플레이어 감지 (자신 콜라이더 무시 위해 RaycastAll 사용)
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.up, detectionDistance);
            
            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    // 플레이어를 감지하면 공격 루틴(코루틴) 시작!
                    StartCoroutine(AttackRoutine());
                    break;
                }
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hasDamagedThisAttack = false; // 데미지 판정 초기화
        
        // 1. 플레이어 감지 후 설정한 딜레이(0.7초) 대기
        yield return new WaitForSeconds(delayTime);
        
        // 2. 가시 팍! 돌출 시작
        isProtruding = true;
        Vector3 targetPos = originalPosition + (Vector3.up * protrudeHeight);
        
        float elapsedTime = 0f;
        float popUpTime = 0.05f; // 0.05초만에 아주 빠르게 솟아오름
        while (elapsedTime < popUpTime)
        {
            transform.position = Vector3.Lerp(originalPosition, targetPos, elapsedTime / popUpTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        // 3. 튀어나온 상태(위험 상태)로 유지
        yield return new WaitForSeconds(activeDuration);
        
        // 4. 다시 바닥으로 들어가기 (이때부터는 데미지 안 줌)
        isProtruding = false;
        elapsedTime = 0f;
        float goDownTime = 0.5f; // 0.5초 동안 서서히 들어감
        while (elapsedTime < goDownTime)
        {
            transform.position = Vector3.Lerp(targetPos, originalPosition, elapsedTime / goDownTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
        
        // 5. 쿨타임 (함정이 쏙 들어가고 나서 다시 감지하기까지의 시간)
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    // Trigger 영역에 닿아있을 때 (가시가 솟아오르는 중에 플레이어 몸체와 닿아도 반응하기 위해 Stay 사용)
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 튀어나와 있는 상태이고, 이번 공격에 아직 데미지를 주지 않았다면!
        if (isProtruding && !hasDamagedThisAttack && collision.CompareTag("Player"))
        {
            ApplyDamage(collision.gameObject);
            hasDamagedThisAttack = true;
        }
    }

    // 물리적 충돌체에 부딪혔을 때
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isProtruding && !hasDamagedThisAttack && collision.gameObject.CompareTag("Player"))
        {
            ApplyDamage(collision.gameObject);
            hasDamagedThisAttack = true;
        }
    }

    private void ApplyDamage(GameObject player)
    {
        // 1. 실제 플레이어의 CharacterBase 체력도 깎습니다.
        CharacterBase cb = player.GetComponent<CharacterBase>();
        if (cb != null)
        {
            cb.currentHp -= (int)damage;
            if (cb.currentHp < 0) cb.currentHp = 0;
        }

        // 2. UI 체력바도 깎습니다.
        HSH_BarUI[] barUIs = FindObjectsOfType<HSH_BarUI>(true);
        bool isDamaged = false;

        foreach (var barUI in barUIs)
        {
            if (barUI.currentType == HSH_BarUI.BarType.HP || barUI.currentType == HSH_BarUI.BarType.GhostHP)
            {
                barUI.DecreaseValue(damage);
                isDamaged = true;
            }
        }

        if (isDamaged)
        {
            Debug.Log($"[SpikeTrap] 가시가 솟아올라 플레이어를 찔렀습니다! 데미지: {damage}");
        }
    }

    // 에디터 씬 뷰에서 감지 범위를 빨간 선으로 편하게 볼 수 있도록 그려주는 함수
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * detectionDistance);
    }
}
