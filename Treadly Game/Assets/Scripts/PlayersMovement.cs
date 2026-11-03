using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayersMovement : MonoBehaviour
{
    public static event System.Action<Transform> ShieldsLaunched;
    public static event System.Action<Transform> Player2ShieldsLaunched;
    public static event System.Action<Transform> Player3ShieldsLaunched;
    public static event System.Action<Transform> Player4ShieldsLaunched;

    private static bool applicationIsQuitting;
    private static readonly HashSet<PlayersMovement> activePlayers =
        new HashSet<PlayersMovement>();

    public static IReadOnlyCollection<PlayersMovement> ActivePlayers => activePlayers;
    public bool IsTouchingFloor => touchingFloor.Count > 0;

    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 180f;

    [SerializeField, Min(0f)]
    private float shieldLaunchForce = 10f;

    private readonly HashSet<Collider2D> touchingFloor = new HashSet<Collider2D>();
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;
    private float forwardInput;
    private float turnInput;
    private Vector2 facingDirection;
    private bool dodgerApplied;
    private int dodgerLevelsApplied;
    private bool bigOleTankApplied;
    [SerializeField, HideInInspector]
    private bool sniperApplied;
    [SerializeField, HideInInspector]
    private int sniperHitsApplied;
    [SerializeField, HideInInspector]
    private bool isUpgradeClone;
    [SerializeField, HideInInspector]
    private bool cloneApplied;
    [SerializeField, HideInInspector]
    private bool boomerBoiApplied;
    [SerializeField, HideInInspector]
    private bool blackHoleApplied;
    private float rocketShipMovingTime;
    private float rocketShipSpeedBonusApplied;
    private GameObject upgradeClonePlayer;
    private bool upgradeCloneShieldCreated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        applicationIsQuitting = false;
        activePlayers.Clear();
    }

    private void OnEnable()
    {
        activePlayers.Add(this);
    }

    private void Awake()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        facingDirection = Quaternion.Euler(0f, 0f, playerBody.rotation) * Vector2.up;
        HideLegacyFrenzyText();
    }

    private void Update()
    {
        bool isPlayer1 = CompareTag("Player1");
        bool isPlayer2 = CompareTag("Player2");
        bool isPlayer3 = CompareTag("Player3");
        bool isPlayer4 = CompareTag("Player4");

        ApplyDodgerUpgrade(isPlayer1, isPlayer2, isPlayer3, isPlayer4);
        ApplyBigOleTankUpgrade(isPlayer1, isPlayer2, isPlayer3, isPlayer4);
        ApplySniperUpgrade(isPlayer1, isPlayer2, isPlayer3, isPlayer4);

        int playerNumber = GetPlayerNumber(isPlayer1, isPlayer2, isPlayer3, isPlayer4);
        ApplyBoomerBoiUpgrade(playerNumber);
        ApplyBlackHoleUpgrade(playerNumber);
        ApplyCloneUpgrade(playerNumber);

        if (isPlayer1 && Input.GetKeyDown(KeyCode.Q))
        {
            LaunchShields(1);
        }
        else if (isPlayer2 && Input.GetKeyDown(KeyCode.Slash))
        {
            LaunchShields(2);
        }
        else if (isPlayer3 && Input.GetKeyDown(KeyCode.O))
        {
            LaunchShields(3);
        }
        else if (isPlayer4 && Input.GetKeyDown(KeyCode.T))
        {
            LaunchShields(4);
        }

        if (GameManager.IsPlayerMovementLockedByChainWave(playerNumber))
        {
            forwardInput = 0f;
            turnInput = 0f;
            UpdateRocketShipSpeedBonus(playerNumber, false);
            return;
        }

        if ((!isPlayer1 && !isPlayer2 && !isPlayer3 && !isPlayer4)
            || touchingFloor.Count == 0)
        {
            forwardInput = 0f;
            turnInput = 0f;
            UpdateRocketShipSpeedBonus(playerNumber, false);
            return;
        }

        if (isPlayer1)
        {
            forwardInput = ReadKeyAxis(KeyCode.W, KeyCode.S);
            turnInput = ReadKeyAxis(KeyCode.D, KeyCode.A);
        }
        else if (isPlayer2)
        {
            forwardInput = ReadKeyAxis(KeyCode.UpArrow, KeyCode.DownArrow);
            turnInput = ReadKeyAxis(KeyCode.RightArrow, KeyCode.LeftArrow);
        }
        else if (isPlayer3)
        {
            forwardInput = ReadKeyAxis(KeyCode.P, KeyCode.Semicolon);
            turnInput = ReadKeyAxis(KeyCode.Quote, KeyCode.L);
        }
        else
        {
            forwardInput = ReadKeyAxis(KeyCode.Y, KeyCode.H);
            turnInput = ReadKeyAxis(KeyCode.J, KeyCode.G);
        }

        if (bigOleTankApplied)
        {
            forwardInput = 0f;
        }

        UpdateRocketShipSpeedBonus(playerNumber, forwardInput != 0f);
    }

    private void FixedUpdate()
    {
        Vector2 pullVelocity = BlackHoleShield.GetPullVelocity(
            playerBody,
            playerCollider,
            out int touchingBlackHoles);

        int playerNumber = GetPlayerNumber(
            CompareTag("Player1"),
            CompareTag("Player2"),
            CompareTag("Player3"),
            CompareTag("Player4"));

        if (GameManager.IsRocketShipActive(playerNumber))
        {
            pullVelocity = Vector2.zero;
        }

        if (touchingBlackHoles >= 2)
        {
            Destroy(gameObject);
            return;
        }

        if (GameManager.IsPlayerMovementLockedByChainWave(playerNumber))
        {
            playerBody.velocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            return;
        }

        if (forwardInput == 0f && turnInput == 0f && pullVelocity == Vector2.zero)
        {
            return;
        }

        float nextRotation = playerBody.rotation - turnInput * rotationSpeed * Time.fixedDeltaTime;
        playerBody.MoveRotation(nextRotation);

        facingDirection = Quaternion.Euler(0f, 0f, nextRotation) * Vector2.up;

        Vector2 totalVelocity = pullVelocity;

        if (forwardInput != 0f)
        {
            totalVelocity += facingDirection * forwardInput * Mathf.Max(0f, moveSpeed);
        }

        if (totalVelocity != Vector2.zero)
        {
            playerBody.MovePosition(
                playerBody.position + totalVelocity * Time.fixedDeltaTime);
        }
    }

    private static float ReadKeyAxis(KeyCode positiveKey, KeyCode negativeKey)
    {
        float value = 0f;

        if (Input.GetKey(positiveKey))
        {
            value += 1f;
        }

        if (Input.GetKey(negativeKey))
        {
            value -= 1f;
        }

        return value;
    }

    private void UpdateRocketShipSpeedBonus(int playerNumber, bool isMoving)
    {
        if (!GameManager.IsRocketShipActive(playerNumber) || !isMoving)
        {
            moveSpeed = Mathf.Max(0f, moveSpeed - rocketShipSpeedBonusApplied);
            rocketShipMovingTime = 0f;
            rocketShipSpeedBonusApplied = 0f;
            return;
        }

        rocketShipMovingTime += Time.deltaTime;
        int completedSeconds = Mathf.FloorToInt(rocketShipMovingTime);

        if (completedSeconds <= 0)
        {
            return;
        }

        moveSpeed += completedSeconds;
        rocketShipSpeedBonusApplied += completedSeconds;
        rocketShipMovingTime -= completedSeconds;
    }

    private void ApplyDodgerUpgrade(
        bool isPlayer1,
        bool isPlayer2,
        bool isPlayer3,
        bool isPlayer4)
    {
        int playerNumber = isPlayer1 ? 1
            : isPlayer2 ? 2
            : isPlayer3 ? 3
            : isPlayer4 ? 4
            : 0;

        if (dodgerLevelsApplied == 0 && dodgerApplied)
        {
            dodgerLevelsApplied = 1;
        }

        int targetLevel = GameManager.GetDodgerLevel(playerNumber);
        int newLevels = targetLevel - dodgerLevelsApplied;

        if (newLevels <= 0)
        {
            return;
        }

        moveSpeed += 2.5f * newLevels;
        dodgerLevelsApplied = targetLevel;
        dodgerApplied = targetLevel > 0;
    }

    private void ApplyBigOleTankUpgrade(
        bool isPlayer1,
        bool isPlayer2,
        bool isPlayer3,
        bool isPlayer4)
    {
        if (bigOleTankApplied)
        {
            return;
        }

        int playerNumber = isPlayer1 ? 1
            : isPlayer2 ? 2
            : isPlayer3 ? 3
            : isPlayer4 ? 4
            : 0;

        if (playerNumber != 0 && GameManager.IsBigOleTankActive(playerNumber))
        {
            shieldLaunchForce = Mathf.Max(0f, shieldLaunchForce - 5f);
            bigOleTankApplied = true;
        }
    }

    private void ApplySniperUpgrade(
        bool isPlayer1,
        bool isPlayer2,
        bool isPlayer3,
        bool isPlayer4)
    {
        int playerNumber = isPlayer1 ? 1
            : isPlayer2 ? 2
            : isPlayer3 ? 3
            : isPlayer4 ? 4
            : 0;

        if (playerNumber == 0 || !GameManager.IsSniperActive(playerNumber))
        {
            return;
        }

        if (!sniperApplied)
        {
            shieldLaunchForce += 10f;
            sniperApplied = true;
        }

        int totalHits = GameManager.GetSniperHitCount(playerNumber);
        int newHits = totalHits - sniperHitsApplied;

        if (newHits > 0)
        {
            shieldLaunchForce += newHits * 2f;
            sniperHitsApplied = totalHits;
        }
    }

    private static int GetPlayerNumber(
        bool isPlayer1,
        bool isPlayer2,
        bool isPlayer3,
        bool isPlayer4)
    {
        return isPlayer1 ? 1
            : isPlayer2 ? 2
            : isPlayer3 ? 3
            : isPlayer4 ? 4
            : 0;
    }

    private void HideLegacyFrenzyText()
    {
        foreach (TMPro.TMP_Text candidate in
            GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (candidate.gameObject.name.StartsWith(
                    "Frenzy Text",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                candidate.gameObject.SetActive(false);
            }
        }
    }

    private void ApplyCloneUpgrade(int playerNumber)
    {
        if (playerNumber == 0 || !GameManager.IsCloneActive(playerNumber))
        {
            return;
        }

        if (!cloneApplied)
        {
            moveSpeed = Mathf.Max(0f, moveSpeed - 2f);
            cloneApplied = true;
        }

        if (isUpgradeClone)
        {
            return;
        }

        if (upgradeClonePlayer == null)
        {
            CreateUpgradeClone(playerNumber);
            return;
        }

        if (!upgradeCloneShieldCreated)
        {
            GameObject shieldTemplate = FindShieldOwnedByPlayer(playerNumber, transform);

            if (shieldTemplate != null)
            {
                CreateShieldForClone(shieldTemplate, upgradeClonePlayer.transform);
            }
        }
    }

    private void ApplyBoomerBoiUpgrade(int playerNumber)
    {
        if (boomerBoiApplied
            || playerNumber == 0
            || !GameManager.IsBoomerBoiActive(playerNumber))
        {
            return;
        }

        shieldLaunchForce = Mathf.Max(0f, shieldLaunchForce - 5f);
        boomerBoiApplied = true;
    }

    private void ApplyBlackHoleUpgrade(int playerNumber)
    {
        if (blackHoleApplied
            || playerNumber == 0
            || !GameManager.IsBlackHoleActive(playerNumber))
        {
            return;
        }

        moveSpeed = Mathf.Max(0f, moveSpeed - 2f);
        blackHoleApplied = true;
    }

    private void CreateUpgradeClone(int playerNumber)
    {
        GameObject shieldTemplate = FindShieldOwnedByPlayer(playerNumber, transform);
        Vector3 clonePosition = transform.position + Vector3.right * 3f;
        upgradeClonePlayer = Instantiate(gameObject, clonePosition, transform.rotation);
        upgradeClonePlayer.name = $"{gameObject.name} Clone";

        PlayersMovement cloneMovement = upgradeClonePlayer.GetComponent<PlayersMovement>();
        cloneMovement.isUpgradeClone = true;
        cloneMovement.cloneApplied = true;
        cloneMovement.dodgerApplied = dodgerApplied;
        cloneMovement.dodgerLevelsApplied = dodgerLevelsApplied;
        cloneMovement.bigOleTankApplied = bigOleTankApplied;
        cloneMovement.sniperApplied = sniperApplied;
        cloneMovement.sniperHitsApplied = sniperHitsApplied;
        cloneMovement.boomerBoiApplied = boomerBoiApplied;
        cloneMovement.blackHoleApplied = blackHoleApplied;
        cloneMovement.moveSpeed = Mathf.Max(
            0f,
            cloneMovement.moveSpeed - rocketShipSpeedBonusApplied);
        cloneMovement.rocketShipMovingTime = 0f;
        cloneMovement.rocketShipSpeedBonusApplied = 0f;

        if (upgradeClonePlayer.TryGetComponent(out Rigidbody2D cloneBody))
        {
            cloneBody.velocity = Vector2.zero;
            cloneBody.angularVelocity = 0f;
        }

        if (shieldTemplate != null)
        {
            CreateShieldForClone(shieldTemplate, upgradeClonePlayer.transform);
        }
    }

    private void CreateShieldForClone(
        GameObject shieldTemplate,
        Transform cloneTransform)
    {
        GameObject cloneShield = Instantiate(
            shieldTemplate,
            cloneTransform.position,
            shieldTemplate.transform.rotation);
        cloneShield.name = $"{shieldTemplate.name} Clone";

        ResetAndAssignFollower(cloneShield, cloneTransform);

        if (cloneShield.TryGetComponent(out Rigidbody2D cloneShieldBody))
        {
            cloneShieldBody.velocity = Vector2.zero;
            cloneShieldBody.angularVelocity = 0f;
            cloneShieldBody.simulated = true;
        }

        foreach (Collider2D shieldCollider in cloneShield.GetComponentsInChildren<Collider2D>(true))
        {
            shieldCollider.enabled = true;
        }

        upgradeCloneShieldCreated = true;
    }

    private static GameObject FindShieldOwnedByPlayer(int playerNumber, Transform owner)
    {
        foreach (GameObject shield in GameObject.FindGameObjectsWithTag($"shield{playerNumber}"))
        {
            if (ShieldBelongsToPlayer(shield, playerNumber, owner))
            {
                return shield;
            }
        }

        return null;
    }

    private static bool ShieldBelongsToPlayer(
        GameObject shield,
        int playerNumber,
        Transform owner)
    {
        switch (playerNumber)
        {
            case 1:
                return shield.TryGetComponent(out FollowPlayer1 player1Follower)
                    && player1Follower.BelongsToPlayer(owner);
            case 2:
                return shield.TryGetComponent(out FollowPlayer2 player2Follower)
                    && player2Follower.BelongsToPlayer(owner);
            case 3:
                return shield.TryGetComponent(out FollowPlayer3 player3Follower)
                    && player3Follower.BelongsToPlayer(owner);
            case 4:
                return shield.TryGetComponent(out FollowPlayer4 player4Follower)
                    && player4Follower.BelongsToPlayer(owner);
            default:
                return false;
        }
    }

    private static bool ResetAndAssignFollower(GameObject shield, Transform owner)
    {
        if (shield.TryGetComponent(out FollowPlayer1 player1Follower))
        {
            player1Follower.ResetForRespawn();
            player1Follower.SetFollowTarget(owner);
            player1Follower.enabled = true;
            return true;
        }

        if (shield.TryGetComponent(out FollowPlayer2 player2Follower))
        {
            player2Follower.ResetForRespawn();
            player2Follower.SetFollowTarget(owner);
            player2Follower.enabled = true;
            return true;
        }

        if (shield.TryGetComponent(out FollowPlayer3 player3Follower))
        {
            player3Follower.ResetForRespawn();
            player3Follower.SetFollowTarget(owner);
            player3Follower.enabled = true;
            return true;
        }

        if (shield.TryGetComponent(out FollowPlayer4 player4Follower))
        {
            player4Follower.ResetForRespawn();
            player4Follower.SetFollowTarget(owner);
            player4Follower.enabled = true;
            return true;
        }

        return false;
    }

    private void LaunchShields(int playerNumber)
    {
        if (playerNumber == 2)
        {
            Player2ShieldsLaunched?.Invoke(transform);
        }
        else if (playerNumber == 3)
        {
            Player3ShieldsLaunched?.Invoke(transform);
        }
        else if (playerNumber == 4)
        {
            Player4ShieldsLaunched?.Invoke(transform);
        }
        else
        {
            ShieldsLaunched?.Invoke(transform);
        }

        string shieldTag = $"shield{playerNumber}";
        GameObject[] shields = GameObject.FindGameObjectsWithTag(shieldTag);

        foreach (GameObject shield in shields)
        {
            if (!ShieldBelongsToPlayer(shield, playerNumber, transform))
            {
                continue;
            }

            if (shield.TryGetComponent(out Rigidbody2D shieldBody))
            {
                shieldBody.AddForce(facingDirection * shieldLaunchForce, ForceMode2D.Impulse);
            }
            else
            {
                Debug.LogWarning($"{shield.name} needs a Rigidbody2D to be launched.", shield);
            }
        }
    }

    public static void RespawnFollowingObject(
        GameObject objectToRespawn,
        float regenerationCooldown,
        Vector2 respawnOffset,
        string playerTag,
        Transform preferredPlayer)
    {
        if (!Application.isPlaying
            || applicationIsQuitting
            || objectToRespawn == null
            || !objectToRespawn.scene.isLoaded)
        {
            return;
        }

        GameObject playerObject = preferredPlayer != null
            ? preferredPlayer.gameObject
            : GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject == null
            || !playerObject.TryGetComponent(out PlayersMovement playerMovement)
            || !playerMovement.isActiveAndEnabled)
        {
            return;
        }

        playerMovement.CreateRespawnReplacement(
            objectToRespawn,
            regenerationCooldown,
            respawnOffset,
            playerMovement.transform);
    }

    private void CreateRespawnReplacement(
        GameObject objectToRespawn,
        float regenerationCooldown,
        Vector2 respawnOffset,
        Transform owningPlayer)
    {
        GameObject replacement = Instantiate(
            objectToRespawn,
            objectToRespawn.transform.position,
            objectToRespawn.transform.rotation);

        if (!ResetAndAssignFollower(replacement, owningPlayer))
        {
            Destroy(replacement);
            return;
        }

        replacement.SetActive(false);
        StartCoroutine(ActivateReplacementAfterCooldown(
            replacement,
            regenerationCooldown,
            respawnOffset));
    }

    private IEnumerator ActivateReplacementAfterCooldown(
        GameObject replacement,
        float regenerationCooldown,
        Vector2 respawnOffset)
    {
        float remainingCooldown = Mathf.Max(0f, regenerationCooldown);

        while (remainingCooldown > 0f)
        {
            if (replacement == null)
            {
                yield break;
            }

            if (!BlackHoleShield.IsPlayerTouchingAny(transform))
            {
                remainingCooldown -= Time.deltaTime;
            }

            yield return null;
        }

        while (BlackHoleShield.IsPlayerTouchingAny(transform))
        {
            if (replacement == null)
            {
                yield break;
            }

            yield return null;
        }

        if (replacement == null)
        {
            yield break;
        }

        replacement.transform.position = transform.position + (Vector3)respawnOffset;

        if (replacement.TryGetComponent(out FollowPlayer1 replacementFollower))
        {
            replacementFollower.SetFollowTarget(transform);
            replacementFollower.enabled = true;
        }

        if (replacement.TryGetComponent(out FollowPlayer2 player2Follower))
        {
            player2Follower.SetFollowTarget(transform);
            player2Follower.enabled = true;
        }

        if (replacement.TryGetComponent(out FollowPlayer3 player3Follower))
        {
            player3Follower.SetFollowTarget(transform);
            player3Follower.enabled = true;
        }

        if (replacement.TryGetComponent(out FollowPlayer4 player4Follower))
        {
            player4Follower.SetFollowTarget(transform);
            player4Follower.enabled = true;
        }

        foreach (Collider2D replacementCollider in replacement.GetComponentsInChildren<Collider2D>(true))
        {
            replacementCollider.enabled = true;
        }

        if (replacement.TryGetComponent(out Rigidbody2D replacementBody))
        {
            replacementBody.velocity = Vector2.zero;
            replacementBody.angularVelocity = 0f;
            replacementBody.simulated = true;
        }

        replacement.SetActive(true);

        int playerNumber = GetPlayerNumber(
            CompareTag("Player1"),
            CompareTag("Player2"),
            CompareTag("Player3"),
            CompareTag("Player4"));
        GameManager.TriggerChainWave(playerNumber);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        AddFloor(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        AddFloor(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        RemoveFloor(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        AddFloor(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        AddFloor(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RemoveFloor(other);
    }

    private void AddFloor(Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            touchingFloor.Add(other);
        }
    }

    private void RemoveFloor(Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            touchingFloor.Remove(other);
        }
    }

    private void OnDisable()
    {
        activePlayers.Remove(this);
        forwardInput = 0f;
        turnInput = 0f;
        touchingFloor.Clear();
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}
