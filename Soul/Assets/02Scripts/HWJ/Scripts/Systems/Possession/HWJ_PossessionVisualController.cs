using UnityEngine;

/// <summary>
/// 플레이어 원본 외형 캐시, 빙의체 외형 적용, 저장 스냅샷 복원을 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PossessionVisualController : MonoBehaviour
{
    [SerializeField] private bool copyPossessedBodyVisual = true;

    private SpriteRenderer ownerSpriteRenderer;
    private Sprite ownerOriginalSprite;
    private Color ownerOriginalColor;
    private bool ownerOriginalFlipX;
    private bool ownerOriginalFlipY;
    private bool hasOwnerSpriteCache;

    private Animator ownerAnimator;
    private RuntimeAnimatorController ownerOriginalAnimatorController;
    private bool hasOwnerAnimatorCache;
    private HWJ_CharacterMotionSystem ownerMotionSystem;

    private void Awake()
    {
        CacheOwnerVisual();
    }

    public void CacheOwnerVisual()
    {
        if (ownerSpriteRenderer == null)
        {
            ownerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (ownerSpriteRenderer != null && !hasOwnerSpriteCache)
        {
            ownerOriginalSprite = ownerSpriteRenderer.sprite;
            ownerOriginalColor = ownerSpriteRenderer.color;
            ownerOriginalFlipX = ownerSpriteRenderer.flipX;
            ownerOriginalFlipY = ownerSpriteRenderer.flipY;
            hasOwnerSpriteCache = true;
        }

        if (ownerAnimator == null)
        {
            ownerAnimator = GetComponentInChildren<Animator>();
        }

        if (ownerAnimator != null && !hasOwnerAnimatorCache)
        {
            ownerOriginalAnimatorController = ownerAnimator.runtimeAnimatorController;
            hasOwnerAnimatorCache = true;
        }

        if (ownerMotionSystem == null)
        {
            ownerMotionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }
    }

    public void ApplyFromTarget(GameObject possessedBody)
    {
        CacheOwnerVisual();

        if (!copyPossessedBodyVisual || possessedBody == null)
        {
            return;
        }

        SpriteRenderer possessedRenderer =
            possessedBody.GetComponentInChildren<SpriteRenderer>();

        if (ownerSpriteRenderer != null && possessedRenderer != null)
        {
            ownerSpriteRenderer.sprite = possessedRenderer.sprite;
            ownerSpriteRenderer.color = possessedRenderer.color;
            ownerSpriteRenderer.flipX = possessedRenderer.flipX;
            ownerSpriteRenderer.flipY = possessedRenderer.flipY;
            ownerMotionSystem?.RefreshFacingBaseline();
        }

        Animator possessedAnimator = possessedBody.GetComponentInChildren<Animator>();

        if (ownerAnimator != null && possessedAnimator != null)
        {
            ownerAnimator.runtimeAnimatorController = possessedAnimator.runtimeAnimatorController;
        }
    }

    public void ApplySnapshot(
        Sprite sprite,
        Color color,
        bool flipX,
        bool flipY,
        RuntimeAnimatorController animatorController)
    {
        CacheOwnerVisual();

        if (ownerSpriteRenderer != null && sprite != null)
        {
            ownerSpriteRenderer.sprite = sprite;
            ownerSpriteRenderer.color = color;
            ownerSpriteRenderer.flipX = flipX;
            ownerSpriteRenderer.flipY = flipY;
            ownerMotionSystem?.RefreshFacingBaseline();
        }

        if (ownerAnimator != null && animatorController != null)
        {
            ownerAnimator.runtimeAnimatorController = animatorController;
        }
    }

    public void ApplyModelData(HWJ_RootObjectDataSO rootObjectData)
    {
        if (rootObjectData == null || rootObjectData.Model == null)
        {
            return;
        }

        if (rootObjectData.Model.modelPrefab != null)
        {
            ApplyFromTarget(rootObjectData.Model.modelPrefab);
        }

        if (ownerAnimator != null && rootObjectData.Model.animatorController != null)
        {
            ownerAnimator.runtimeAnimatorController = rootObjectData.Model.animatorController;
        }
    }

    public bool TryGetCurrentSnapshot(
        out Sprite sprite,
        out Color color,
        out bool flipX,
        out bool flipY,
        out RuntimeAnimatorController animatorController)
    {
        CacheOwnerVisual();

        sprite = null;
        color = Color.white;
        flipX = false;
        flipY = false;
        animatorController = null;

        bool hasVisual = false;

        if (ownerSpriteRenderer != null)
        {
            sprite = ownerSpriteRenderer.sprite;
            color = ownerSpriteRenderer.color;
            flipX = ownerSpriteRenderer.flipX;
            flipY = ownerSpriteRenderer.flipY;
            hasVisual = sprite != null;
        }

        if (ownerAnimator != null)
        {
            animatorController = ownerAnimator.runtimeAnimatorController;
            hasVisual = hasVisual || animatorController != null;
        }

        return hasVisual;
    }

    public void RestoreOwnerVisual()
    {
        CacheOwnerVisual();

        if (ownerSpriteRenderer != null && hasOwnerSpriteCache)
        {
            ownerSpriteRenderer.sprite = ownerOriginalSprite;
            ownerSpriteRenderer.color = ownerOriginalColor;
            ownerSpriteRenderer.flipX = ownerOriginalFlipX;
            ownerSpriteRenderer.flipY = ownerOriginalFlipY;
            ownerMotionSystem?.RefreshFacingBaseline();
        }

        if (ownerAnimator != null && hasOwnerAnimatorCache)
        {
            ownerAnimator.runtimeAnimatorController = ownerOriginalAnimatorController;
        }
    }
}
