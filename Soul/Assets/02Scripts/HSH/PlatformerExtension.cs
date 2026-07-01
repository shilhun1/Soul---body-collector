using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlatformEffector2D))]
public class PlatformerExtension : MonoBehaviour
{
    private PlatformEffector2D effector;
    private Coroutine reverseRoutine;

    private void Awake()
    {
        effector = GetComponent<PlatformEffector2D>();
        
        if (effector != null)
        {
            // 밑에서 위로 통과가 가능하도록 One Way 활성화
            effector.useOneWay = true;
        }

        // 콜라이더가 Effector를 사용하도록 자동 설정
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.usedByEffector = true;
        }
    }

    public void OnDownWay()
    {
        Debug.Log("ondown start");
        if (effector == null) return;

        if (reverseRoutine != null)
        {
            StopCoroutine(reverseRoutine);
            Debug.Log("ondown stop");
        }
        reverseRoutine = StartCoroutine(ReverseRotationalOffset());
    }

    private IEnumerator ReverseRotationalOffset()
    {
        effector.rotationalOffset = 180f;
        yield return new WaitForSeconds(0.5f);
        effector.rotationalOffset = 0f;
        reverseRoutine = null;
    }
}
