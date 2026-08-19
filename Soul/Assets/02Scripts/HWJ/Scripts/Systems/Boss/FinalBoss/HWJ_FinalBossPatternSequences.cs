using System.Collections;
using UnityEngine;

/// <summary>
/// 기획서의 최종보스 1·2페이즈 패턴 순서를 실행합니다.
/// 이 파일은 HWJ_FinalBossPatternSystem의 시퀀스 책임만 분리한 partial입니다.
/// </summary>
public sealed partial class HWJ_FinalBossPatternSystem
{
    private IEnumerator RunPatternSequence(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        switch (definition.PatternKind)
        {
            case HWJ_FinalBossPatternKind.Phase1BlackOrbVolley:
                yield return Phase1BlackOrbVolley(definition, target);
                break;
            case HWJ_FinalBossPatternKind.Phase1SpearLine:
                yield return Phase1SpearLine(definition, target);
                break;
            case HWJ_FinalBossPatternKind.Phase1Barrier:
                yield return Phase1Barrier(definition);
                break;
            case HWJ_FinalBossPatternKind.Phase1PortalSummon:
                yield return PortalSummon(definition, target);
                break;
            case HWJ_FinalBossPatternKind.Phase1FloorFireLightning:
                yield return Phase1FloorFireLightning(definition);
                break;
            case HWJ_FinalBossPatternKind.Phase2WeaponBarrage:
                yield return Phase2WeaponBarrage(definition, target);
                break;
            case HWJ_FinalBossPatternKind.Phase2BarrierOrbVolley:
                yield return Phase2BarrierOrbVolley(definition, target);
                break;
            case HWJ_FinalBossPatternKind.Phase2BlackFlameCharge:
                yield return Phase2BlackFlameCharge(definition, target);
                break;
            case HWJ_FinalBossPatternKind.Phase2DoublePortalSummon:
                yield return PortalSummon(definition, target);
                break;
            case HWJ_FinalBossPatternKind.Phase2EightWayLightning:
                yield return Phase2EightWayLightning(definition);
                break;
        }
    }

    private IEnumerator Phase1BlackOrbVolley(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        SetBossVisualAirborne(2.5f);
        RuntimeAttackHandle[] orbs = CreateOrbitingProjectiles(
            definition,
            definition.ProjectileCount,
            new Color(0.08f, 0.02f, 0.12f, 0.95f));
        yield return WaitForCastMoment(definition);

        for (int i = 0; i < orbs.Length; i++)
        {
            Vector2 direction = target != null
                ? (target.position - orbs[i].Root.transform.position).normalized
                : Vector2.left;
            LaunchProjectile(
                orbs[i],
                direction,
                definition.ProjectileSpeed,
                definition.ProjectileMaximumDistance);

            if (i < orbs.Length - 1)
            {
                yield return new WaitForSeconds(definition.ProjectileIntervalSeconds);
            }

        }
    }

    private IEnumerator Phase1SpearLine(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        Vector2 previewDirection = DirectionToTarget(target);
        Vector3 previewCenter = ProjectileOrigin
            + (Vector3)(previewDirection * definition.ProjectileMaximumDistance * 0.5f);
        float previewAngle = Mathf.Atan2(previewDirection.y, previewDirection.x) * Mathf.Rad2Deg;
        ShowTelegraph(
            definition,
            previewCenter,
            new Vector2(definition.ProjectileMaximumDistance, 0.2f),
            previewAngle,
            new Color(0.45f, 0.1f, 0.65f, 0.4f),
            definition.PreparationSeconds);
        yield return WaitForCastMoment(definition);

        Vector2 direction = DirectionToTarget(target);
        RuntimeAttackHandle spear = CreateRuntimeAttack(
            definition,
            ProjectileOrigin,
            definition.ProjectileSize,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
            new Color(0.07f, 0.01f, 0.1f, 1f),
            definition.ProjectileVisualPrefab,
            definition.DamageMultiplier,
            0f,
            0f,
            false,
            null,
            false);
        LaunchProjectile(
            spear,
            direction,
            definition.ProjectileSpeed,
            definition.ProjectileMaximumDistance);

        Vector3 lineCenter = ProjectileOrigin
            + (Vector3)(direction * definition.ProjectileMaximumDistance * 0.5f);
        CreateTimedHazard(
            definition,
            lineCenter,
            new Vector2(definition.ProjectileMaximumDistance, Mathf.Max(0.2f, definition.HazardSize.y)),
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
            definition.HazardDurationSeconds,
            new Color(0.02f, 0.01f, 0.03f, 0.9f),
            definition.DamageMultiplier,
            0f,
            0f,
            false,
            definition.HazardVisualPrefab);
    }

    private IEnumerator Phase1Barrier(HWJ_FinalBossPatternDefinitionSO definition)
    {
        yield return WaitForCastMoment(definition);
        barrierSystem?.ActivateBarrier(
            definition.BarrierHpRatio,
            definition.BarrierDurationSeconds,
            definition.BarrierVisualPrefab);
    }

    private IEnumerator PortalSummon(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        yield return WaitForCastMoment(definition);
        yield return RunPortalSummonSequence(definition, target);
    }

    private IEnumerator Phase1FloorFireLightning(
        HWJ_FinalBossPatternDefinitionSO definition)
    {
        Vector2 roomCenter = ResolveBossRoomCenter();
        Vector2 roomSize = ResolveBossRoomSize();
        float floorY = roomCenter.y - roomSize.y * 0.5f + definition.HazardSize.y * 0.5f;
        ShowTelegraph(
            definition,
            new Vector3(roomCenter.x, floorY, transform.position.z),
            new Vector2(roomSize.x, definition.HazardSize.y),
            0f,
            new Color(0.7f, 0.1f, 0.75f, 0.35f),
            definition.PreparationSeconds);
        yield return WaitForCastMoment(definition);

        CreateTimedHazard(
            definition,
            new Vector3(roomCenter.x, floorY, transform.position.z),
            new Vector2(roomSize.x, definition.HazardSize.y),
            0f,
            definition.HazardDurationSeconds,
            new Color(0.25f, 0.01f, 0.3f, 0.85f),
                definition.DamageMultiplier,
                0f,
                0f,
                true,
                definition.HazardVisualPrefab);

        int strikeCount = Mathf.Max(2, definition.ProjectileCount);
        float left = roomCenter.x - roomSize.x * 0.5f;
        float step = roomSize.x / strikeCount;

        for (int i = 0; i < strikeCount; i++)
        {
            float x = left + step * (i + 0.5f);
            Vector3 strikePosition = new Vector3(x, roomCenter.y, transform.position.z);
            ShowTelegraph(
                definition,
                strikePosition,
                new Vector2(step * 0.85f, roomSize.y),
                0f,
                new Color(0.75f, 0.2f, 0.95f, 0.35f),
                0.18f);
            yield return new WaitForSeconds(0.18f);
            CreateTimedHazard(
                definition,
                strikePosition,
                new Vector2(step * 0.85f, roomSize.y),
                0f,
                0.15f,
                new Color(0.12f, 0.01f, 0.2f, 0.95f),
                definition.DamageMultiplier,
                0f,
                definition.StunSeconds,
                false,
                definition.SecondaryHazardVisualPrefab);
        }

        bossBrain?.ForceGroggy(4f);
    }

    private IEnumerator Phase2WeaponBarrage(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        SetBossVisualAirborne(2.5f);
        yield return WaitForCastMoment(definition);
        int weaponCount = Mathf.Max(9, definition.ProjectileCount);

        for (int i = 0; i < weaponCount; i++)
        {
            int weaponGroup = Mathf.Min(2, i / 3);
            Vector2 direction = DirectionToTarget(target);
            Vector2 size = definition.ProjectileSize;
            Color color;
            GameObject weaponVisual;

            if (weaponGroup == 0)
            {
                color = new Color(0.5f, 0.5f, 0.55f, 1f);
                weaponVisual = definition.ProjectileVisualPrefab;
            }
            else if (weaponGroup == 1)
            {
                size.x *= 1.35f;
                color = new Color(0.25f, 0.25f, 0.3f, 1f);
                weaponVisual = definition.SecondaryProjectileVisualPrefab;
            }
            else
            {
                size *= 1.4f;
                color = new Color(0.35f, 0.08f, 0.08f, 1f);
                weaponVisual = definition.TertiaryProjectileVisualPrefab;
            }

            RuntimeAttackHandle weapon = CreateRuntimeAttack(
                definition,
                ProjectileOrigin + Vector3.up * ((i % 3) - 1) * 0.7f,
                size,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                color,
                weaponVisual,
                definition.DamageMultiplier,
                0f,
                0f,
                false,
                null,
                false);
            LaunchProjectile(
                weapon,
                direction,
                definition.ProjectileSpeed,
                definition.ProjectileMaximumDistance);

            if (i == weaponCount - 1)
            {
                yield return new WaitForSeconds(0.2f);
                Vector2 roomSize = ResolveBossRoomSize();
                CreateTimedHazard(
                    definition,
                    new Vector3(
                        ResolveBossRoomCenter().x,
                        target != null ? target.position.y : transform.position.y,
                        transform.position.z),
                    new Vector2(roomSize.x, Mathf.Max(0.5f, definition.HazardSize.y)),
                    0f,
                    0.25f,
                    new Color(0.35f, 0.05f, 0.05f, 0.9f),
                    definition.DamageMultiplier,
                    0f,
                    0f,
                    false,
                    definition.HazardVisualPrefab);
            }

            if (i < weaponCount - 1)
            {
                yield return new WaitForSeconds(definition.ProjectileIntervalSeconds);
            }
        }
    }

    private IEnumerator Phase2BarrierOrbVolley(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        RuntimeAttackHandle[] orbs = CreateOrbitingProjectiles(
            definition,
            definition.ProjectileCount,
            new Color(0.16f, 0.02f, 0.25f, 1f));
        yield return WaitForCastMoment(definition);
        barrierSystem?.ActivateBarrier(
            definition.BarrierHpRatio,
            definition.BarrierDurationSeconds,
            definition.BarrierVisualPrefab);

        for (int i = 0; i < orbs.Length; i++)
        {
            // 5발을 0.8초마다 발사하면 마지막 발사가 4초 시점에 나갑니다.
            yield return new WaitForSeconds(definition.ProjectileIntervalSeconds);
            Vector2 direction = target != null
                ? (target.position - orbs[i].Root.transform.position).normalized
                : Vector2.left;
            LaunchProjectile(
                orbs[i],
                direction,
                definition.ProjectileSpeed,
                definition.ProjectileMaximumDistance);

        }
    }

    private IEnumerator Phase2BlackFlameCharge(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        Vector2 lockedDirection = DirectionToTarget(target);
        float warningDistance = Mathf.Min(
            definition.ChargeMaximumDistance,
            Vector2.Distance(transform.position, target.position));
        ShowTelegraph(
            definition,
            transform.position + (Vector3)(lockedDirection * warningDistance * 0.5f),
            new Vector2(warningDistance, definition.ProjectileSize.y),
            Mathf.Atan2(lockedDirection.y, lockedDirection.x) * Mathf.Rad2Deg,
            new Color(0.08f, 0.01f, 0.1f, 0.45f),
            definition.PreparationSeconds);
        yield return WaitForCastMoment(definition);
        yield return RunActualBlackFlameCharge(definition, target.position);
    }

    private IEnumerator RunActualBlackFlameCharge(
        HWJ_FinalBossPatternDefinitionSO definition,
        Vector3 targetSnapshot)
    {
        Vector2 start = transform.position;
        Vector2 direction = ((Vector2)targetSnapshot - start).normalized;
        float distance = Mathf.Min(
            Vector2.Distance(start, targetSnapshot),
            definition.ChargeMaximumDistance);
        Vector2 destination = ClampToBossRoom(start + direction * distance);
        RuntimeAttackHandle chargeHitbox = CreateRuntimeAttack(
            definition,
            transform.position,
            definition.ProjectileSize,
            0f,
            new Color(0.03f, 0.01f, 0.04f, 0.9f),
            definition.ProjectileVisualPrefab,
            0f,
            definition.PossessionMentalDamageRatio,
            definition.StunSeconds,
            false,
            transform,
            true);
        allowBossRootMovement = true;

        while (Vector2.Distance(transform.position, destination) > 0.05f
            && chargeHitbox.Hitbox.TotalSuccessfulHits == 0)
        {
            Vector2 current = body != null ? body.position : (Vector2)transform.position;
            Vector2 next = Vector2.MoveTowards(
                current,
                destination,
                definition.ChargeSpeed * Time.fixedDeltaTime);

            if (body != null)
            {
                body.MovePosition(next);
            }
            else
            {
                transform.position = new Vector3(next.x, next.y, transform.position.z);
            }

            yield return new WaitForFixedUpdate();
        }

        ReleaseRuntimeAttack(chargeHitbox);
        yield return ReturnBossToStationaryAnchor(definition.ReturnToAnchorSeconds);
        allowBossRootMovement = false;
        SetBossPosition(stationaryAnchorPosition);
    }

    private IEnumerator ReturnBossToStationaryAnchor(float seconds)
    {
        Vector2 start = body != null ? body.position : (Vector2)transform.position;
        float duration = Mathf.Max(0.01f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            Vector2 next = Vector2.Lerp(
                start,
                stationaryAnchorPosition,
                Mathf.Clamp01(elapsed / duration));

            if (body != null)
            {
                body.MovePosition(next);
            }
            else
            {
                transform.position = new Vector3(next.x, next.y, transform.position.z);
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator Phase2EightWayLightning(
        HWJ_FinalBossPatternDefinitionSO definition)
    {
        if (bossVisualRoot != null)
        {
            bossVisualRoot.localPosition = visualRootInitialLocalPosition + Vector3.up * 2.5f;
        }

        Vector3 origin = bossVisualRoot != null
            ? bossVisualRoot.position
            : transform.position;
        int count = definition.RadialAttackCount;

        for (int i = 0; i < count; i++)
        {
            float angle = 360f * i / count;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            ShowTelegraph(
                definition,
                origin + (Vector3)(direction * definition.ProjectileMaximumDistance * 0.5f),
                new Vector2(definition.ProjectileMaximumDistance, definition.HazardSize.y),
                angle,
                new Color(0.55f, 0.1f, 0.8f, 0.35f),
                definition.PreparationSeconds);
        }

        yield return WaitForCastMoment(definition);

        for (int i = 0; i < count; i++)
        {
            float angle = 360f * i / count;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            CreateTimedHazard(
                definition,
                origin + (Vector3)(direction * definition.ProjectileMaximumDistance * 0.5f),
                new Vector2(definition.ProjectileMaximumDistance, definition.HazardSize.y),
                angle,
                0.25f,
                new Color(0.05f, 0.01f, 0.08f, 0.95f),
                definition.DamageMultiplier,
                0f,
                definition.StunSeconds,
                false,
                definition.HazardVisualPrefab);
        }

        yield return new WaitForSeconds(0.25f);
    }

    private void SetBossVisualAirborne(float height)
    {
        if (bossVisualRoot != null)
        {
            bossVisualRoot.localPosition = visualRootInitialLocalPosition
                + Vector3.up * Mathf.Max(0f, height);
        }
    }

    private RuntimeAttackHandle[] CreateOrbitingProjectiles(
        HWJ_FinalBossPatternDefinitionSO definition,
        int count,
        Color color)
    {
        RuntimeAttackHandle[] handles = new RuntimeAttackHandle[Mathf.Max(1, count)];
        Vector3 origin = ProjectileOrigin;

        for (int i = 0; i < handles.Length; i++)
        {
            float ratio = handles.Length <= 1 ? 0.5f : (float)i / (handles.Length - 1);
            float angle = Mathf.Lerp(150f, 30f, ratio) * Mathf.Deg2Rad;
            Vector3 position = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.5f;
            handles[i] = CreateRuntimeAttack(
                definition,
                position,
                definition.ProjectileSize,
                0f,
                color,
                definition.ProjectileVisualPrefab,
                definition.DamageMultiplier,
                0f,
                definition.StunSeconds,
                false,
                null,
                false);
        }

        return handles;
    }

    private Vector3 ProjectileOrigin => projectileSocket != null
        ? projectileSocket.position
        : transform.position;

    private Vector2 DirectionToTarget(Transform target)
    {
        if (target == null)
        {
            return Vector2.left;
        }

        Vector2 direction = target.position - ProjectileOrigin;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
    }

    private Vector2 ResolveBossRoomCenter()
    {
        return bossBrain != null ? bossBrain.BossRoomCenter : (Vector2)stationaryAnchorPosition;
    }

    private Vector2 ResolveBossRoomSize()
    {
        return bossBrain != null ? bossBrain.BossRoomSize : new Vector2(28f, 14f);
    }

    private Vector2 ClampToBossRoom(Vector2 position)
    {
        Vector2 center = ResolveBossRoomCenter();
        Vector2 half = ResolveBossRoomSize() * 0.5f;
        return new Vector2(
            Mathf.Clamp(position.x, center.x - half.x, center.x + half.x),
            Mathf.Clamp(position.y, center.y - half.y, center.y + half.y));
    }
}
