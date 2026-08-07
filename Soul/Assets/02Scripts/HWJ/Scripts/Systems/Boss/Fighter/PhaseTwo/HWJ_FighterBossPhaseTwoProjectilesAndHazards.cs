using System.Collections;
using UnityEngine;

/// <summary>
/// Creates, updates, pools, and releases projectile and ground-hazard objects.
/// This partial belongs to the single HWJ_FighterBossPhaseTwoPatternSystem component.
/// </summary>
public sealed partial class HWJ_FighterBossPhaseTwoPatternSystem
{
    private void SpawnHorizontalProjectile(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        float direction,
        float speed,
        float maximumDistance)
    {
        Vector3 start = transform.position + new Vector3(direction * 1.1f, 0.9f, 0f);
        RuntimeAttackObject projectile = AcquireRuntimeObject(
            false,
            start,
            new Vector2(1.1f, 0.35f),
            new Color(0.25f, 0.55f, 1f, 0.95f));
        projectile.Hitbox.ArmStrike(
            1,
            activeProfile != null ? activeProfile.DamageMultiplier : 1f,
            activeProfile != null ? activeProfile.ExtraKnockbackPower : 0f);
        Record(projectileCounts, profile.PatternId);
        Coroutine routine = StartCoroutine(ProjectileRoutine(
            projectile,
            Mathf.Sign(direction),
            Mathf.Max(1f, speed),
            Mathf.Max(1f, maximumDistance)));
        runtimeEffectRoutines.Add(routine);
    }

    private IEnumerator ProjectileRoutine(
        RuntimeAttackObject projectile,
        float direction,
        float speed,
        float maximumDistance)
    {
        float distance = 0f;

        while (projectile.InUse && distance < maximumDistance)
        {
            float step = speed * Time.fixedDeltaTime;

            if (RuntimeObjectHitsWall(projectile, direction, step))
            {
                break;
            }

            projectile.Root.transform.position += Vector3.right * direction * step;
            distance += step;
            yield return new WaitForFixedUpdate();
        }

        ReleaseRuntimeObject(projectile);
    }

    private void SpawnLingeringHazard(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        Vector3 position,
        Vector2 size,
        float duration)
    {
        while (ActiveLingeringHazardCount >= 2)
        {
            RuntimeAttackObject oldest = FindFirstActiveHazard();

            if (oldest == null)
            {
                break;
            }

            ReleaseRuntimeObject(oldest);
        }

        RuntimeAttackObject hazard = AcquireRuntimeObject(
            true,
            ClampToArena(position),
            size,
            new Color(0.45f, 0.3f, 1f, 0.9f));
        hazard.Hitbox.ArmStrike(
            1,
            profile.DamageMultiplier * 0.8f,
            profile.ExtraKnockbackPower * 0.35f);
        Record(hazardCounts, profile.PatternId);
        Coroutine routine = StartCoroutine(HazardRoutine(hazard, duration));
        runtimeEffectRoutines.Add(routine);
    }

    private IEnumerator HazardRoutine(RuntimeAttackObject hazard, float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
        ReleaseRuntimeObject(hazard);
    }

    private RuntimeAttackObject AcquireRuntimeObject(
        bool isHazard,
        Vector3 position,
        Vector2 size,
        Color color)
    {
        RuntimeAttackObject runtimeObject = null;

        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            if (!runtimeObjectPool[i].InUse)
            {
                runtimeObject = runtimeObjectPool[i];
                break;
            }
        }

        if (runtimeObject == null)
        {
            GameObject root = new GameObject("HWJ_FighterBoss_RuntimeAttack");
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.enabled = false;
            HWJ_FighterBossHitboxSystem hitbox = root.AddComponent<HWJ_FighterBossHitboxSystem>();
            hitbox.Configure(combatSystem, collider);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.positionCount = 2;
            line.widthMultiplier = 0.1f;
            line.numCapVertices = 2;
            line.sortingOrder = facingRenderer != null ? facingRenderer.sortingOrder + 1 : 11;

            Material material = ResolveTelegraphMaterial();

            if (material != null)
            {
                line.sharedMaterial = material;
            }

            runtimeObject = new RuntimeAttackObject
            {
                Root = root,
                Collider = collider,
                Hitbox = hitbox,
                Line = line
            };
            runtimeObjectPool.Add(runtimeObject);
        }

        runtimeObject.IsHazard = isHazard;
        runtimeObject.InUse = true;
        runtimeObject.Root.name = isHazard
            ? "TEMP_HAZARD_FighterBoss"
            : "TEMP_PROJECTILE_FighterBoss";
        runtimeObject.Root.layer = gameObject.layer;
        runtimeObject.Root.transform.position = position;
        runtimeObject.Root.transform.rotation = Quaternion.identity;
        runtimeObject.Root.transform.localScale = Vector3.one;
        runtimeObject.Root.SetActive(true);
        runtimeObject.Collider.size = size;
        runtimeObject.Collider.offset = Vector2.zero;
        runtimeObject.Hitbox.Configure(combatSystem, runtimeObject.Collider);
        ConfigureRuntimeLine(runtimeObject, size, color);
        return runtimeObject;
    }

    private void ConfigureRuntimeLine(RuntimeAttackObject runtimeObject, Vector2 size, Color color)
    {
        if (runtimeObject.Line == null)
        {
            return;
        }

        runtimeObject.Line.startColor = color;
        runtimeObject.Line.endColor = color;
        runtimeObject.Line.widthMultiplier = Mathf.Max(0.08f, size.y * 0.3f);

        if (runtimeObject.IsHazard)
        {
            const int segments = 24;
            runtimeObject.Line.loop = true;
            runtimeObject.Line.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                runtimeObject.Line.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * size.x * 0.5f,
                        Mathf.Sin(angle) * size.y * 0.5f,
                        0f));
            }
        }
        else
        {
            runtimeObject.Line.loop = false;
            runtimeObject.Line.positionCount = 2;
            runtimeObject.Line.SetPosition(0, new Vector3(-size.x * 0.5f, 0f, 0f));
            runtimeObject.Line.SetPosition(1, new Vector3(size.x * 0.5f, 0f, 0f));
        }

        runtimeObject.Line.enabled = true;
    }

    private void ReleaseRuntimeObject(RuntimeAttackObject runtimeObject)
    {
        if (runtimeObject == null || !runtimeObject.InUse)
        {
            return;
        }

        runtimeObject.Hitbox?.Disarm();

        if (runtimeObject.Line != null)
        {
            runtimeObject.Line.enabled = false;
        }

        runtimeObject.InUse = false;
        runtimeObject.Root.SetActive(false);
    }

    private void StopAndReleaseRuntimeObjects()
    {
        for (int i = 0; i < runtimeEffectRoutines.Count; i++)
        {
            if (runtimeEffectRoutines[i] != null)
            {
                StopCoroutine(runtimeEffectRoutines[i]);
            }
        }

        runtimeEffectRoutines.Clear();

        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            ReleaseRuntimeObject(runtimeObjectPool[i]);
        }
    }

}
