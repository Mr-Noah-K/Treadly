using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FollowPlayer4 : MonoBehaviour
{
    [SerializeField]
    private Vector2 respawnOffset = Vector2.up;

    [SerializeField, Min(0f)]
    private float regenerationCooldown = 1f;

    [SerializeField, Min(0f)]
    private float destructionRange = 5f;

    [SerializeField, Min(0f), InspectorName("ShieldSize")]
    private float shieldSize = 1f;

    [SerializeField] public bool miniGun;
    [SerializeField] public bool dodger;
    [SerializeField, InspectorName("Big O'le Tank")] public bool bigOleTank;
    [SerializeField] public bool sniper;
    [SerializeField] public bool homing;
    [FormerlySerializedAs("frenzy")]
    [SerializeField, InspectorName("Chain Wave")] public bool chainWave;
    [SerializeField] public bool clone;
    [SerializeField, InspectorName("BoomerBoi")] public bool boomerBoi;
    [SerializeField, InspectorName("BlackHole")] public bool blackHole;
    [SerializeField, InspectorName("RocketShip")] public bool rocketShip;

    private readonly HashSet<Collider2D> floorContacts = new HashSet<Collider2D>();
    private readonly Collider2D[] boomerBoiProximityResults = new Collider2D[16];
    private Transform player4;
    private Vector3 followOffset;
    private bool hasTouchedFloor;
    private bool isFollowing = true;
    private bool isTravelingOffFloor;
    private Vector2 offFloorStartPosition;
    [SerializeField, HideInInspector]
    private bool miniGunApplied;
    [SerializeField, HideInInspector]
    private int miniGunLevelsApplied;
    [SerializeField, HideInInspector]
    private bool dodgerApplied;
    [SerializeField, HideInInspector]
    private int dodgerLevelsApplied;
    [SerializeField, HideInInspector]
    private bool bigOleTankApplied;
    [SerializeField, HideInInspector]
    private bool sniperApplied;
    [SerializeField, HideInInspector]
    private int sniperHitsApplied;
    [SerializeField, HideInInspector]
    private bool homingApplied;
    [SerializeField, HideInInspector]
    private bool cloneApplied;
    [SerializeField, HideInInspector]
    private bool boomerBoiApplied;
    [SerializeField, HideInInspector]
    private bool isBoomerBoiExpanding;
    [SerializeField, HideInInspector]
    private bool boomerBoiExpandedThisLaunch;
    [SerializeField, HideInInspector]
    private float boomerBoiLaunchSize;
    [SerializeField, HideInInspector]
    private float boomerBoiTargetSize;
    [SerializeField, HideInInspector]
    private bool blackHoleApplied;
    [SerializeField, HideInInspector]
    private bool blackHoleRespawnScheduled;
    private BlackHoleShield blackHoleShield;

    private void OnEnable()
    {
        PlayersMovement.Player4ShieldsLaunched += StopFollowing;
    }

    private void Start()
    {
        NormalizeSizeAndRegeneration();
        CheckMiniGunUpgrade();
        CheckDodgerUpgrade();
        CheckBigOleTankUpgrade();
        CheckSniperUpgrade();
        CheckHomingUpgrade();
        CheckChainWaveUpgrade();
        CheckCloneUpgrade();
        CheckBoomerBoiUpgrade();
        CheckBlackHoleUpgrade();
        CheckRocketShipUpgrade();
        ApplyShieldSize();

        if (player4 == null)
        {
            FindPlayer4();
        }
    }

    private void Update()
    {
        CheckMiniGunUpgrade();
        CheckDodgerUpgrade();
        CheckBigOleTankUpgrade();
        CheckSniperUpgrade();
        CheckHomingUpgrade();
        CheckChainWaveUpgrade();
        CheckCloneUpgrade();
        CheckBoomerBoiUpgrade();
        CheckBlackHoleUpgrade();
        CheckRocketShipUpgrade();
        UpdateBoomerBoiExpansion();
    }

    private void FixedUpdate()
    {
        if (isBoomerBoiExpanding && TryGetComponent(out Rigidbody2D expandingBody))
        {
            expandingBody.velocity = Vector2.zero;
            expandingBody.angularVelocity = 0f;
            return;
        }

        if (!isFollowing && homingApplied && TryGetComponent(out Rigidbody2D shieldBody))
        {
            GameManager.SteerShieldTowardNearestPlayer(shieldBody, "Player4", 15f);
        }
    }

    private void OnValidate()
    {
        NormalizeSizeAndRegeneration();
        ApplyShieldSize();
    }

    private void ApplyShieldSize()
    {
        transform.localScale = Vector3.one * shieldSize;
    }

    private void LateUpdate()
    {
        if (isTravelingOffFloor
            && Vector2.Distance(offFloorStartPosition, transform.position) >= destructionRange)
        {
            Destroy(gameObject);
            return;
        }

        if (!isFollowing)
        {
            return;
        }

        if (player4 == null)
        {
            FindPlayer4();

            if (player4 == null)
            {
                return;
            }
        }

        transform.position = player4.position + followOffset;
    }

    private void FindPlayer4()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player4");

        if (playerObject != null)
        {
            player4 = playerObject.transform;
            followOffset = transform.position - player4.position;
        }
    }

    private void StopFollowing(Transform launchingPlayer)
    {
        if (player4 == null)
        {
            FindPlayer4();
        }

        if (player4 == launchingPlayer)
        {
            isFollowing = false;
            boomerBoiLaunchSize = shieldSize;
            boomerBoiExpandedThisLaunch = false;
            blackHoleShield?.MarkLaunched();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.collider);
        AddFloorContact(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        AddFloorContact(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        RemoveFloorContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other);
        AddFloorContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        AddFloorContact(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RemoveFloorContact(other);
    }

    private void AddFloorContact(Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            floorContacts.Add(other);
            hasTouchedFloor = true;
            isTravelingOffFloor = false;
        }
    }

    private void RemoveFloorContact(Collider2D other)
    {
        if (!other.CompareTag("Floor"))
        {
            return;
        }

        floorContacts.Remove(other);

        if (hasTouchedFloor && floorContacts.Count == 0)
        {
            offFloorStartPosition = transform.position;
            isTravelingOffFloor = true;

            if (destructionRange <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void HandleImpact(Collider2D other)
    {
        GameObject otherObject = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (otherObject == gameObject)
        {
            return;
        }

        if (IsShield(otherObject))
        {
            if (otherObject.CompareTag("shield4"))
            {
                return;
            }

            if (BlackHoleShield.ShouldIgnoreBlackHoleContact(gameObject, otherObject))
            {
                return;
            }

            bool thisShieldIsBlackHole = BlackHoleShield.TryFormFromShieldContact(gameObject);
            bool otherShieldIsBlackHole = BlackHoleShield.TryFormFromShieldContact(otherObject);
            bool thisShieldIsExpanding = TriggerBoomerBoiExpansionFromContact();
            bool otherShieldIsExpanding = GameManager.TriggerBoomerBoiExpansionFromContact(otherObject);

            if (thisShieldIsBlackHole && otherShieldIsBlackHole)
            {
                return;
            }

            if (thisShieldIsBlackHole)
            {
                Destroy(otherObject);
                return;
            }

            if (otherShieldIsBlackHole)
            {
                Destroy(gameObject);
                return;
            }

            if (thisShieldIsExpanding && otherShieldIsExpanding)
            {
                return;
            }

            if (thisShieldIsExpanding)
            {
                Destroy(otherObject);
                return;
            }

            if (otherShieldIsExpanding)
            {
                Destroy(gameObject);
                return;
            }

            Destroy(otherObject);
            Destroy(gameObject);
            return;
        }

        if (IsPlayer(otherObject) && !otherObject.CompareTag("Player4"))
        {
            if (blackHoleShield != null && blackHoleShield.IsFormed)
            {
                return;
            }

            TriggerBoomerBoiExpansionFromContact();
            CheckSniperUpgrade();

            if (GameManager.IsSniperActive(4))
            {
                GameManager.RegisterSniperHit(4);
            }

            Destroy(otherObject);
        }
    }

    private static bool IsShield(GameObject target)
    {
        return target.CompareTag("shield1")
            || target.CompareTag("shield2")
            || target.CompareTag("shield3")
            || target.CompareTag("shield4");
    }

    private static bool IsPlayer(GameObject target)
    {
        return target.CompareTag("Player1")
            || target.CompareTag("Player2")
            || target.CompareTag("Player3")
            || target.CompareTag("Player4");
    }

    private void OnDestroy()
    {
        PlayersMovement.Player4ShieldsLaunched -= StopFollowing;

        if (!blackHoleRespawnScheduled)
        {
            PlayersMovement.RespawnFollowingObject(
                gameObject,
                GetCurrentRegenerationCooldown(),
                respawnOffset,
                "Player4",
                player4);
        }
    }

    public void ResetForRespawn()
    {
        floorContacts.Clear();
        hasTouchedFloor = false;
        isFollowing = true;
        isTravelingOffFloor = false;
        offFloorStartPosition = Vector2.zero;
        player4 = null;
        followOffset = Vector3.zero;
        miniGun = false;
        dodger = false;
        bigOleTank = false;
        sniper = false;
        homing = false;
        chainWave = false;
        clone = false;
        boomerBoi = false;
        blackHole = false;
        rocketShip = false;
        blackHoleShield = GetComponent<BlackHoleShield>();
        blackHoleShield?.ResetForRespawn();
        blackHoleRespawnScheduled = false;

        if (boomerBoiExpandedThisLaunch)
        {
            shieldSize = boomerBoiLaunchSize;
            ApplyShieldSize();
        }

        isBoomerBoiExpanding = false;
        boomerBoiExpandedThisLaunch = false;
        boomerBoiLaunchSize = 0f;
        boomerBoiTargetSize = 0f;
    }

    private void OnDisable()
    {
        PlayersMovement.Player4ShieldsLaunched -= StopFollowing;
    }

    private void CheckMiniGunUpgrade()
    {
        if (miniGun)
        {
            GameManager.ActivateMiniGun(4);
            miniGun = false;
        }

        if (miniGunLevelsApplied == 0 && miniGunApplied)
        {
            miniGunLevelsApplied = 1;
        }

        int targetLevel = GameManager.GetMiniGunLevel(4);
        int newLevels = targetLevel - miniGunLevelsApplied;

        if (newLevels <= 0)
        {
            return;
        }

        regenerationCooldown -= 1f * newLevels;
        shieldSize -= 1.5f * newLevels;
        NormalizeSizeAndRegeneration();
        miniGunLevelsApplied = targetLevel;
        miniGunApplied = targetLevel > 0;
        ApplyShieldSize();
    }

    private void NormalizeSizeAndRegeneration()
    {
        if (regenerationCooldown <= 0f)
        {
            regenerationCooldown = 0.25f;
        }

        if (shieldSize <= 0f)
        {
            shieldSize = 0.25f;
        }
    }

    private void CheckDodgerUpgrade()
    {
        if (dodger)
        {
            GameManager.ActivateDodger(4);
            dodger = false;
        }

        if (dodgerLevelsApplied == 0 && dodgerApplied)
        {
            dodgerLevelsApplied = 1;
        }

        int targetLevel = GameManager.GetDodgerLevel(4);
        int newLevels = targetLevel - dodgerLevelsApplied;

        if (newLevels <= 0)
        {
            return;
        }

        shieldSize -= 0.5f * newLevels;
        regenerationCooldown += 0.5f * newLevels;
        NormalizeSizeAndRegeneration();
        dodgerLevelsApplied = targetLevel;
        dodgerApplied = targetLevel > 0;
        ApplyShieldSize();
    }

    private void CheckBigOleTankUpgrade()
    {
        if (bigOleTank)
        {
            GameManager.ActivateBigOleTank(4);
            bigOleTank = false;
        }

        if (bigOleTankApplied || !GameManager.IsBigOleTankActive(4))
        {
            return;
        }

        regenerationCooldown += 2f;
        shieldSize += 5f;
        bigOleTankApplied = true;
        ApplyShieldSize();
    }

    private void CheckSniperUpgrade()
    {
        if (sniper)
        {
            GameManager.ActivateSniper(4);
            sniper = false;
        }

        if (!GameManager.IsSniperActive(4))
        {
            return;
        }

        if (!sniperApplied)
        {
            shieldSize -= 0.5f;
            NormalizeSizeAndRegeneration();
            sniperApplied = true;
        }

        int totalHits = GameManager.GetSniperHitCount(4);
        int newHits = totalHits - sniperHitsApplied;

        if (newHits > 0)
        {
            shieldSize -= newHits * 0.25f;
            regenerationCooldown -= newHits * 0.25f;
            NormalizeSizeAndRegeneration();
            sniperHitsApplied = totalHits;
        }

        ApplyShieldSize();
    }

    private void CheckHomingUpgrade()
    {
        if (homing)
        {
            GameManager.ActivateHoming(4);
            homing = false;
        }

        if (homingApplied || !GameManager.IsHomingActive(4))
        {
            return;
        }

        regenerationCooldown += 1f;
        homingApplied = true;
    }

    private void CheckChainWaveUpgrade()
    {
        if (chainWave)
        {
            GameManager.ActivateChainWave(4);
            chainWave = false;
        }
    }

    private float GetCurrentRegenerationCooldown()
    {
        if (GameManager.IsRocketShipActive(4))
        {
            return GameManager.RocketShipRegenerationCooldown;
        }

        return regenerationCooldown <= 0f ? 0.25f : regenerationCooldown;
    }

    private void CheckRocketShipUpgrade()
    {
        if (rocketShip)
        {
            GameManager.ActivateRocketShip(4);
            rocketShip = false;
        }

        if (GameManager.IsRocketShipActive(4))
        {
            regenerationCooldown = GameManager.RocketShipRegenerationCooldown;
        }
    }

    private void CheckCloneUpgrade()
    {
        if (clone)
        {
            GameManager.ActivateClone(4);
            clone = false;
        }

        if (cloneApplied || !GameManager.IsCloneActive(4))
        {
            return;
        }

        shieldSize -= 2f;
        NormalizeSizeAndRegeneration();
        cloneApplied = true;
        ApplyShieldSize();
    }

    private void CheckBoomerBoiUpgrade()
    {
        if (boomerBoi)
        {
            GameManager.ActivateBoomerBoi(4);
            boomerBoi = false;
        }

        if (boomerBoiApplied || !GameManager.IsBoomerBoiActive(4))
        {
            return;
        }

        regenerationCooldown += 1f;
        shieldSize += 1f;
        boomerBoiApplied = true;
        ApplyShieldSize();
    }

    private void CheckBlackHoleUpgrade()
    {
        if (blackHole)
        {
            GameManager.ActivateBlackHole(4);
            blackHole = false;
        }

        if (!GameManager.IsBlackHoleActive(4))
        {
            return;
        }

        if (!blackHoleApplied)
        {
            regenerationCooldown += 2f;
            shieldSize += 2f;
            blackHoleApplied = true;
            ApplyShieldSize();
        }

        if (blackHoleShield == null)
        {
            blackHoleShield = GetComponent<BlackHoleShield>();

            if (blackHoleShield == null)
            {
                blackHoleShield = gameObject.AddComponent<BlackHoleShield>();
            }
        }

        if (!isFollowing)
        {
            blackHoleShield.MarkLaunched();
        }
    }

    private void UpdateBoomerBoiExpansion()
    {
        if (isBoomerBoiExpanding)
        {
            shieldSize = Mathf.MoveTowards(
                shieldSize,
                boomerBoiTargetSize,
                2f * Time.deltaTime);
            ApplyShieldSize();

            if (Mathf.Approximately(shieldSize, boomerBoiTargetSize))
            {
                isBoomerBoiExpanding = false;

                if (blackHoleShield == null || !blackHoleShield.IsFormed)
                {
                    Destroy(gameObject);
                }
            }

            return;
        }

        if (!boomerBoiApplied || isFollowing || boomerBoiExpandedThisLaunch)
        {
            return;
        }

        if (GameManager.HasBoomerBoiTargetNearby(
            gameObject,
            "Player4",
            boomerBoiProximityResults))
        {
            BeginBoomerBoiExpansion();
        }
    }

    private void BeginBoomerBoiExpansion()
    {
        if (boomerBoiExpandedThisLaunch)
        {
            return;
        }

        boomerBoiLaunchSize = shieldSize;
        boomerBoiTargetSize = boomerBoiLaunchSize + 5f;
        boomerBoiExpandedThisLaunch = true;
        isBoomerBoiExpanding = true;

        if (TryGetComponent(out Rigidbody2D shieldBody))
        {
            shieldBody.velocity = Vector2.zero;
            shieldBody.angularVelocity = 0f;
        }
    }

    public bool TriggerBoomerBoiExpansionFromContact()
    {
        if (!isFollowing && boomerBoiApplied && !boomerBoiExpandedThisLaunch)
        {
            BeginBoomerBoiExpansion();
        }

        return isBoomerBoiExpanding;
    }

    public void ScheduleBlackHoleShieldRespawn()
    {
        if (blackHoleRespawnScheduled)
        {
            return;
        }

        blackHoleRespawnScheduled = true;
        PlayersMovement.RespawnFollowingObject(
            gameObject,
            GetCurrentRegenerationCooldown(),
            respawnOffset,
            "Player4",
            player4);
    }

    public void SetFollowTarget(Transform target)
    {
        player4 = target;
        followOffset = target == null ? Vector3.zero : transform.position - target.position;
    }

    public bool BelongsToPlayer(Transform target)
    {
        if (player4 == null)
        {
            FindPlayer4();
        }

        return player4 == target;
    }
}
