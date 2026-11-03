using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class BlackHoleShield : MonoBehaviour
{
    private const float FormationDistance = 0.5f;
    private const float Lifetime = 10f;
    private const int ProximityCapacity = 32;

    private static readonly List<BlackHoleShield> activeBlackHoles =
        new List<BlackHoleShield>();

    private readonly Collider2D[] proximityResults = new Collider2D[ProximityCapacity];
    private Collider2D shieldCollider;
    private Rigidbody2D shieldBody;
    private bool hasLaunched;
    private bool isFormed;
    private float formedAt;

    public bool IsFormed => isFormed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        activeBlackHoles.Clear();
    }

    private void Awake()
    {
        shieldCollider = GetComponent<Collider2D>();
        shieldBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (isFormed)
        {
            RegisterBlackHole();
        }
    }

    private void Update()
    {
        if (isFormed)
        {
            if (Time.time - formedAt >= Lifetime)
            {
                Destroy(gameObject);
            }

            return;
        }

        if (hasLaunched && IsNearAnotherShield())
        {
            FormBlackHole();
        }
    }

    private void FixedUpdate()
    {
        if (!isFormed)
        {
            return;
        }

        RegisterBlackHole();
        shieldBody.velocity = Vector2.zero;
        shieldBody.angularVelocity = 0f;
    }

    public void MarkLaunched()
    {
        hasLaunched = true;
    }

    public bool TryFormFromShieldContact()
    {
        if (hasLaunched && !isFormed)
        {
            FormBlackHole();
        }

        return isFormed;
    }

    public static bool TryFormFromShieldContact(GameObject shield)
    {
        return shield.TryGetComponent(out BlackHoleShield blackHole)
            && blackHole.TryFormFromShieldContact();
    }

    public static bool ShouldIgnoreBlackHoleContact(
        GameObject firstShield,
        GameObject secondShield)
    {
        if (firstShield == null || secondShield == null)
        {
            return false;
        }

        bool firstHasBlackHole = firstShield.TryGetComponent(
            out BlackHoleShield firstBlackHole);
        bool secondHasBlackHole = secondShield.TryGetComponent(
            out BlackHoleShield secondBlackHole);

        return firstHasBlackHole
            && secondHasBlackHole
            && (firstBlackHole.IsFormed || secondBlackHole.IsFormed);
    }

    public void ResetForRespawn()
    {
        UnregisterBlackHole();
        hasLaunched = false;
        isFormed = false;
        formedAt = 0f;
    }

    private bool IsNearAnotherShield()
    {
        float queryRadius = shieldCollider.bounds.extents.magnitude + FormationDistance;
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            shieldCollider.bounds.center,
            queryRadius,
            proximityResults);

        for (int index = 0; index < hitCount; index++)
        {
            Collider2D hit = proximityResults[index];

            if (hit == null || hit == shieldCollider)
            {
                continue;
            }

            GameObject target = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (target == gameObject
                || !IsShield(target)
                || target.CompareTag(gameObject.tag)
                || ShouldIgnoreBlackHoleContact(gameObject, target))
            {
                continue;
            }

            ColliderDistance2D distance = shieldCollider.Distance(hit);

            if (distance.isOverlapped || distance.distance <= FormationDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void FormBlackHole()
    {
        isFormed = true;
        formedAt = Time.time;
        shieldBody.velocity = Vector2.zero;
        shieldBody.angularVelocity = 0f;
        TriggerBoomerBoiExpansion();
        ScheduleReplacementShield();
        RegisterBlackHole();
    }

    private void TriggerBoomerBoiExpansion()
    {
        if (TryGetComponent(out FollowPlayer1 player1))
        {
            player1.TriggerBoomerBoiExpansionFromContact();
        }
        else if (TryGetComponent(out FollowPlayer2 player2))
        {
            player2.TriggerBoomerBoiExpansionFromContact();
        }
        else if (TryGetComponent(out FollowPlayer3 player3))
        {
            player3.TriggerBoomerBoiExpansionFromContact();
        }
        else if (TryGetComponent(out FollowPlayer4 player4))
        {
            player4.TriggerBoomerBoiExpansionFromContact();
        }
    }

    private void ScheduleReplacementShield()
    {
        if (TryGetComponent(out FollowPlayer1 player1))
        {
            player1.ScheduleBlackHoleShieldRespawn();
        }
        else if (TryGetComponent(out FollowPlayer2 player2))
        {
            player2.ScheduleBlackHoleShieldRespawn();
        }
        else if (TryGetComponent(out FollowPlayer3 player3))
        {
            player3.ScheduleBlackHoleShieldRespawn();
        }
        else if (TryGetComponent(out FollowPlayer4 player4))
        {
            player4.ScheduleBlackHoleShieldRespawn();
        }
    }

    private void RegisterBlackHole()
    {
        if (!activeBlackHoles.Contains(this))
        {
            activeBlackHoles.Add(this);
        }
    }

    private void UnregisterBlackHole()
    {
        activeBlackHoles.Remove(this);
    }

    private void OnDisable()
    {
        UnregisterBlackHole();
    }

    private void OnDestroy()
    {
        UnregisterBlackHole();
    }

    public static Vector2 GetPullVelocity(
        Rigidbody2D playerBody,
        Collider2D playerCollider,
        out int touchingBlackHoles)
    {
        touchingBlackHoles = 0;
        Vector2 pullVelocity = Vector2.zero;

        if (playerBody == null || playerCollider == null)
        {
            return pullVelocity;
        }

        RefreshActiveBlackHoles();

        for (int index = activeBlackHoles.Count - 1; index >= 0; index--)
        {
            BlackHoleShield blackHole = activeBlackHoles[index];

            if (blackHole == null || !blackHole.isActiveAndEnabled || !blackHole.isFormed)
            {
                activeBlackHoles.RemoveAt(index);
                continue;
            }

            ColliderDistance2D distance = blackHole.shieldCollider.Distance(playerCollider);
            float edgeDistance = Mathf.Max(0f, distance.distance);

            if (distance.isOverlapped || distance.distance <= 0f)
            {
                touchingBlackHoles++;
            }

            float pullSpeed = edgeDistance <= 1f ? 5f
                : edgeDistance <= 3f ? 2.5f
                : edgeDistance <= 5f ? 0.5f
                : 0f;

            Vector2 direction = blackHole.shieldBody.position - playerBody.position;

            if (pullSpeed > 0f && direction.sqrMagnitude > 0.000001f)
            {
                pullVelocity += direction.normalized * pullSpeed;
            }
        }

        return pullVelocity;
    }

    public static bool IsPlayerTouchingAny(Transform player)
    {
        if (player == null || !player.TryGetComponent(out Collider2D playerCollider))
        {
            return false;
        }

        RefreshActiveBlackHoles();

        for (int index = activeBlackHoles.Count - 1; index >= 0; index--)
        {
            BlackHoleShield blackHole = activeBlackHoles[index];

            if (blackHole == null || !blackHole.isActiveAndEnabled || !blackHole.isFormed)
            {
                activeBlackHoles.RemoveAt(index);
                continue;
            }

            ColliderDistance2D distance = blackHole.shieldCollider.Distance(playerCollider);

            if (distance.isOverlapped || distance.distance <= 0f)
            {
                return true;
            }
        }

        return false;
    }

    private static void RefreshActiveBlackHoles()
    {
        BlackHoleShield[] blackHoles = FindObjectsOfType<BlackHoleShield>();

        foreach (BlackHoleShield blackHole in blackHoles)
        {
            if (blackHole != null
                && blackHole.isActiveAndEnabled
                && blackHole.isFormed
                && !activeBlackHoles.Contains(blackHole))
            {
                activeBlackHoles.Add(blackHole);
            }
        }
    }

    private static bool IsShield(GameObject target)
    {
        return target.CompareTag("shield1")
            || target.CompareTag("shield2")
            || target.CompareTag("shield3")
            || target.CompareTag("shield4");
    }
}
