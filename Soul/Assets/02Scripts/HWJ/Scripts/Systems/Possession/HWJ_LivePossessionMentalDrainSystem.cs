// HWJ_LivePossessionMentalDrainSystem.cs

using UnityEngine;

[RequireComponent(typeof(HWJ_PossessionSystem))]
[DisallowMultipleComponent]
public class HWJ_LivePossessionMentalDrainSystem : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;

    [Header("정신력 시간 감소")]
    [SerializeField, Min(0.01f)] private float drainInterval = 1f;
    [SerializeField, Min(0f)] private float drainAmount = 1f;

    private float drainTimer;

    private void Awake()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }
    }

    private void Update()
    {
        if (possessionSystem == null
            || !possessionSystem.TryGetActiveLiveMentalState(
                out HWJ_LivePossessionMentalState mentalState))
        {
            drainTimer = 0f;
            return;
        }

        drainTimer += Time.deltaTime;

        float interval = Mathf.Max(0.01f, drainInterval);

        while (drainTimer >= interval)
        {
            drainTimer -= interval;

            bool depleted = mentalState.ApplyMentalDrain(
                Mathf.Max(0f, drainAmount));

            if (!depleted)
            {
                continue;
            }

            possessionSystem.ReleasePossessedBodyByMentalDepletion();
            drainTimer = 0f;
            return;
        }
    }
}