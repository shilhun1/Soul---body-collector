using System.Collections;
using UnityEngine;

/// <summary>
/// 최종보스 투사체, 장판, 예고 표시의 생성과 회수를 담당합니다.
/// 이 파일은 HWJ_FinalBossPatternSystem 컴포넌트의 런타임 공격 책임만 분리한 partial입니다.
/// </summary>
public sealed partial class HWJ_FinalBossPatternSystem
{
    private RuntimeAttackHandle CreateRuntimeAttack(
        HWJ_FinalBossPatternDefinitionSO definition,
        Vector3 worldPosition,
        Vector2 size,
        float rotationDegrees,
        Color placeholderColor,
        GameObject visualPrefab,
        float damageMultiplier,
        float mentalDamageRatio,
        float stunSeconds,
        bool repeatHits,
        Transform parent = null,
        bool armImmediately = true)
    {
        RuntimeAttackHandle handle = AcquireRuntimeAttackHandle();
        Transform attackParent = parent != null ? parent : runtimeEffectRoot;
        handle.Root.transform.SetParent(attackParent, false);
        handle.Root.transform.position = worldPosition;
        handle.Root.transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        handle.Root.transform.localScale = Vector3.one;
        handle.Collider.size = size;
        handle.Collider.offset = Vector2.zero;
        handle.PlaceholderRenderer.color = placeholderColor;
        handle.PlaceholderRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        handle.PlaceholderRenderer.enabled = visualPrefab == null;

        if (visualPrefab != null)
        {
            handle.CustomVisual = Instantiate(
                visualPrefab,
                handle.Root.transform.position,
                handle.Root.transform.rotation,
                handle.Root.transform);
            handle.CustomVisual.transform.localPosition = Vector3.zero;
            handle.CustomVisual.transform.localRotation = Quaternion.identity;
        }

        handle.Hitbox.Configure(
            combatSystem,
            handle.Collider,
            damageMultiplier,
            mentalDamageRatio,
            stunSeconds,
            repeatHits,
            definition != null ? definition.RepeatHitIntervalSeconds : 0.5f);

        if (armImmediately)
        {
            handle.Hitbox.Arm();
        }
        else
        {
            handle.Hitbox.Disarm();
        }

        return handle;
    }

    private RuntimeAttackHandle AcquireRuntimeAttackHandle()
    {
        for (int i = 0; i < runtimeAttackPool.Count; i++)
        {
            if (!runtimeAttackPool[i].InUse)
            {
                runtimeAttackPool[i].InUse = true;
                runtimeAttackPool[i].Root.SetActive(true);
                return runtimeAttackPool[i];
            }
        }

        GameObject root = new GameObject("HWJ_FinalBoss_RuntimeAttack");
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.enabled = false;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateRuntimePlaceholderSprite();
        renderer.sortingOrder = 30;
        HWJ_FinalBossAttackHitbox hitbox = root.AddComponent<HWJ_FinalBossAttackHitbox>();
        RuntimeAttackHandle created = new RuntimeAttackHandle
        {
            Root = root,
            Collider = collider,
            PlaceholderRenderer = renderer,
            Hitbox = hitbox,
            InUse = true
        };
        runtimeAttackPool.Add(created);
        return created;
    }

    private Sprite CreateRuntimePlaceholderSprite()
    {
        GameObject temporary = HWJ_FinalBossRuntimeVisualFactory.Create(
            null,
            "HWJ_FinalBoss_PlaceholderSpriteSource",
            Vector3.zero,
            null,
            Vector2.one,
            Color.white,
            0);
        Sprite sprite = temporary.GetComponent<SpriteRenderer>().sprite;
        Destroy(temporary);
        return sprite;
    }

    private void LaunchProjectile(
        RuntimeAttackHandle handle,
        Vector2 direction,
        float speed,
        float maximumDistance)
    {
        if (handle == null || !handle.InUse)
        {
            return;
        }

        handle.Root.transform.SetParent(runtimeEffectRoot, true);
        handle.Root.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        handle.Hitbox.Arm();
        StartCoroutine(ProjectileRoutine(
            handle,
            direction.normalized,
            Mathf.Max(0.1f, speed),
            Mathf.Max(0.1f, maximumDistance)));
    }

    private IEnumerator ProjectileRoutine(
        RuntimeAttackHandle handle,
        Vector2 direction,
        float speed,
        float maximumDistance)
    {
        float travelled = 0f;

        while (handle != null && handle.InUse && travelled < maximumDistance)
        {
            float step = Mathf.Min(speed * Time.fixedDeltaTime, maximumDistance - travelled);

            if (obstacleLayers.value != 0
                && Physics2D.Raycast(handle.Root.transform.position, direction, step, obstacleLayers))
            {
                break;
            }

            handle.Root.transform.position += (Vector3)(direction * step);
            travelled += step;
            yield return new WaitForFixedUpdate();
        }

        ReleaseRuntimeAttack(handle);
    }

    private RuntimeAttackHandle CreateTimedHazard(
        HWJ_FinalBossPatternDefinitionSO definition,
        Vector3 position,
        Vector2 size,
        float rotationDegrees,
        float durationSeconds,
        Color color,
        float damageMultiplier,
        float mentalDamageRatio,
        float stunSeconds,
        bool repeatHits,
        GameObject visualPrefab = null)
    {
        RuntimeAttackHandle handle = CreateRuntimeAttack(
            definition,
            position,
            size,
            rotationDegrees,
            color,
            visualPrefab != null
                ? visualPrefab
                : definition != null ? definition.HazardVisualPrefab : null,
            damageMultiplier,
            mentalDamageRatio,
            stunSeconds,
            repeatHits);
        StartCoroutine(ReleaseAttackAfter(handle, durationSeconds));
        return handle;
    }

    private IEnumerator ReleaseAttackAfter(
        RuntimeAttackHandle handle,
        float durationSeconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.02f, durationSeconds));
        ReleaseRuntimeAttack(handle);
    }

    private GameObject ShowTelegraph(
        HWJ_FinalBossPatternDefinitionSO definition,
        Vector3 position,
        Vector2 size,
        float rotationDegrees,
        Color color,
        float durationSeconds)
    {
        GameObject visual = HWJ_FinalBossRuntimeVisualFactory.Create(
            definition != null ? definition.TelegraphVisualPrefab : null,
            "HWJ_FinalBoss_Telegraph",
            position,
            runtimeEffectRoot,
            size,
            color,
            28);
        visual.transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        transientVisuals.Add(visual);
        StartCoroutine(DestroyTransientVisualAfter(visual, durationSeconds));
        return visual;
    }

    private IEnumerator DestroyTransientVisualAfter(GameObject visual, float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.02f, seconds));

        if (visual != null)
        {
            transientVisuals.Remove(visual);
            Destroy(visual);
        }
    }

    private void ReleaseRuntimeAttack(RuntimeAttackHandle handle)
    {
        if (handle == null || !handle.InUse)
        {
            return;
        }

        handle.Hitbox.Disarm();

        if (handle.CustomVisual != null)
        {
            Destroy(handle.CustomVisual);
            handle.CustomVisual = null;
        }

        handle.PlaceholderRenderer.enabled = false;
        handle.Root.transform.SetParent(runtimeEffectRoot, true);
        handle.Root.SetActive(false);
        handle.InUse = false;
    }

    private void ReleaseAllRuntimeAttacks()
    {
        for (int i = 0; i < runtimeAttackPool.Count; i++)
        {
            ReleaseRuntimeAttack(runtimeAttackPool[i]);
        }
    }

    private void DestroyTransientVisuals()
    {
        for (int i = transientVisuals.Count - 1; i >= 0; i--)
        {
            if (transientVisuals[i] != null)
            {
                Destroy(transientVisuals[i]);
            }
        }

        transientVisuals.Clear();
    }
}
