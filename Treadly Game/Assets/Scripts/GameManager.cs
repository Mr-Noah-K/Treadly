using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public const float RocketShipRegenerationCooldown = 10f;

    private const string UpgradeUIPanelTag = "UpgradeUI";
    private const string PointUIPanelTag = "Point UI";
    private const string PlayerText1Tag = "PlayerT1";
    private const string PlayerText2Tag = "PlayerT2";
    private const string PlayerText3Tag = "PlayerT3";
    private const string ActivePlayerCountPreference = "ActivePlayerCount";
    private const string NumberOfRoundsPreference = "NumberOfRounds";

    private enum UpgradeType
    {
        MiniGun,
        Dodger,
        BigOleTank,
        Sniper,
        Homing,
        ChainWave,
        Clone,
        BoomerBoi,
        BlackHole,
        RocketShip
    }

    private static readonly UpgradeType[] allUpgrades =
    {
        UpgradeType.MiniGun,
        UpgradeType.Dodger,
        UpgradeType.BigOleTank,
        UpgradeType.Sniper,
        UpgradeType.Homing,
        UpgradeType.ChainWave,
        UpgradeType.Clone,
        UpgradeType.BoomerBoi,
        UpgradeType.BlackHole,
        UpgradeType.RocketShip
    };

    [Header("Persistent MiniGun Upgrades")]
    [SerializeField] private bool player1MiniGun;
    [SerializeField] private bool player2MiniGun;
    [SerializeField] private bool player3MiniGun;
    [SerializeField] private bool player4MiniGun;

    private static readonly int[] miniGunLevels = new int[5];

    [Header("Persistent Dodger Upgrades")]
    [SerializeField] private bool player1Dodger;
    [SerializeField] private bool player2Dodger;
    [SerializeField] private bool player3Dodger;
    [SerializeField] private bool player4Dodger;

    private static readonly int[] dodgerLevels = new int[5];

    [Header("Runtime Stackable Upgrade Levels")]
    [SerializeField] private int player1MiniGunStacks;
    [SerializeField] private int player2MiniGunStacks;
    [SerializeField] private int player3MiniGunStacks;
    [SerializeField] private int player4MiniGunStacks;
    [SerializeField] private int player1DodgerStacks;
    [SerializeField] private int player2DodgerStacks;
    [SerializeField] private int player3DodgerStacks;
    [SerializeField] private int player4DodgerStacks;

    [Header("Persistent Big O'le Tank Upgrades")]
    [SerializeField, InspectorName("Player 1 Big O'le Tank")]
    private bool player1BigOleTank;
    [SerializeField, InspectorName("Player 2 Big O'le Tank")]
    private bool player2BigOleTank;
    [SerializeField, InspectorName("Player 3 Big O'le Tank")]
    private bool player3BigOleTank;
    [SerializeField, InspectorName("Player 4 Big O'le Tank")]
    private bool player4BigOleTank;

    private static readonly bool[] bigOleTankActive = new bool[5];

    [Header("Persistent Sniper Upgrades")]
    [SerializeField] private bool player1Sniper;
    [SerializeField] private bool player2Sniper;
    [SerializeField] private bool player3Sniper;
    [SerializeField] private bool player4Sniper;

    private static readonly bool[] sniperActive = new bool[5];
    private static readonly int[] sniperHitCount = new int[5];

    [Header("Persistent Homing Upgrades")]
    [SerializeField] private bool player1Homing;
    [SerializeField] private bool player2Homing;
    [SerializeField] private bool player3Homing;
    [SerializeField] private bool player4Homing;

    private static readonly bool[] homingActive = new bool[5];

    [Header("Persistent Chain Wave Upgrades")]
    [FormerlySerializedAs("player1Frenzy")]
    [SerializeField, InspectorName("Player 1 Chain Wave")] private bool player1ChainWave;
    [FormerlySerializedAs("player2Frenzy")]
    [SerializeField, InspectorName("Player 2 Chain Wave")] private bool player2ChainWave;
    [FormerlySerializedAs("player3Frenzy")]
    [SerializeField, InspectorName("Player 3 Chain Wave")] private bool player3ChainWave;
    [FormerlySerializedAs("player4Frenzy")]
    [SerializeField, InspectorName("Player 4 Chain Wave")] private bool player4ChainWave;

    private static readonly bool[] chainWaveActive = new bool[5];
    private static readonly float[] chainWaveMovementLockEndTime = new float[5];

    [Header("Persistent Clone Upgrades")]
    [SerializeField] private bool player1Clone;
    [SerializeField] private bool player2Clone;
    [SerializeField] private bool player3Clone;
    [SerializeField] private bool player4Clone;

    private static readonly bool[] cloneActive = new bool[5];

    [Header("Persistent BoomerBoi Upgrades")]
    [SerializeField] private bool player1BoomerBoi;
    [SerializeField] private bool player2BoomerBoi;
    [SerializeField] private bool player3BoomerBoi;
    [SerializeField] private bool player4BoomerBoi;

    private static readonly bool[] boomerBoiActive = new bool[5];

    [Header("Persistent BlackHole Upgrades")]
    [SerializeField] private bool player1BlackHole;
    [SerializeField] private bool player2BlackHole;
    [SerializeField] private bool player3BlackHole;
    [SerializeField] private bool player4BlackHole;

    private static readonly bool[] blackHoleActive = new bool[5];

    [Header("Persistent RocketShip Upgrades")]
    [SerializeField] private bool player1RocketShip;
    [SerializeField] private bool player2RocketShip;
    [SerializeField] private bool player3RocketShip;
    [SerializeField] private bool player4RocketShip;

    private static readonly bool[] rocketShipActive = new bool[5];
    private static readonly int[] playerRoundPoints = new int[5];
    private static readonly int[] playerTotalPoints = new int[5];
    private static int activePlayerCount = 4;
    private static int numberOfRounds = 1;
    private static int completedRounds;
    private static int matchWinningPlayerNumber;
    private static bool randomStartingUpgradesEnabled;
    private static bool randomStartingUpgradesGranted;

    public static int ActivePlayerCount => activePlayerCount;
    public static int NumberOfRounds => numberOfRounds;
    public static int CompletedRounds => completedRounds;
    public static int MatchWinningPlayerNumber => matchWinningPlayerNumber;
    public static bool RandomStartingUpgradesEnabled =>
        randomStartingUpgradesEnabled;

    private GameObject upgradeUIPanel;
    private GameObject pointUIPanel;
    private TMPro.TMP_Text playerText1;
    private TMPro.TMP_Text playerText2;
    private TMPro.TMP_Text playerText3;
    private readonly UnityEngine.UI.Button[,] upgradeButtons =
        new UnityEngine.UI.Button[3, 3];
    private readonly UpgradeType[,] offeredUpgrades = new UpgradeType[3, 3];
    private readonly int[] upgradePlayerNumbers = new int[3];
    private readonly bool[] upgradeRowsSelected = new bool[3];
    private readonly UnityEngine.UI.Graphic[,] pointIndicators =
        new UnityEngine.UI.Graphic[5, 3];
    private int selectedUpgradeRows;
    private int roundWinningPlayerNumber;
    private bool roundHadMultipleGroundedPlayerTypes;
    private bool upgradeUIRevealed;
    private bool pointSequenceStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetUpgradeState()
    {
        activePlayerCount = Mathf.Clamp(
            PlayerPrefs.GetInt(ActivePlayerCountPreference, 4),
            2,
            4);
        numberOfRounds = Mathf.Clamp(
            PlayerPrefs.GetInt(NumberOfRoundsPreference, 1),
            1,
            3);
        completedRounds = 0;
        matchWinningPlayerNumber = 0;
        randomStartingUpgradesEnabled = false;
        randomStartingUpgradesGranted = false;

        for (int playerNumber = 1; playerNumber <= 4; playerNumber++)
        {
            miniGunLevels[playerNumber] = 0;
            dodgerLevels[playerNumber] = 0;
            bigOleTankActive[playerNumber] = false;
            sniperActive[playerNumber] = false;
            sniperHitCount[playerNumber] = 0;
            homingActive[playerNumber] = false;
            chainWaveActive[playerNumber] = false;
            chainWaveMovementLockEndTime[playerNumber] = 0f;
            cloneActive[playerNumber] = false;
            boomerBoiActive[playerNumber] = false;
            blackHoleActive[playerNumber] = false;
            rocketShipActive[playerNumber] = false;
            playerRoundPoints[playerNumber] = 0;
            playerTotalPoints[playerNumber] = 0;
        }
    }

    private void Awake()
    {
        RemoveInactivePlayersAndShields();

        for (int playerNumber = activePlayerCount + 1;
             playerNumber <= 4;
             playerNumber++)
        {
            playerRoundPoints[playerNumber] = 0;
        }

        ActivateInspectorUpgrades();
        GrantRandomStartingUpgrades();
        SyncInspectorUpgradeFields();
        CacheUpgradeUI();

        if (upgradeUIPanel != null)
        {
            upgradeUIPanel.SetActive(false);
        }

        if (pointUIPanel != null)
        {
            pointUIPanel.SetActive(false);
        }
    }

    private void Update()
    {
        ActivateInspectorUpgrades();
        CheckForLastPlayerStanding();
    }

    private void CacheUpgradeUI()
    {
        foreach (Transform candidate in FindObjectsOfType<Transform>(true))
        {
            if (upgradeUIPanel == null && candidate.CompareTag(UpgradeUIPanelTag))
            {
                upgradeUIPanel = candidate.gameObject;
            }

            if (pointUIPanel == null && candidate.CompareTag(PointUIPanelTag))
            {
                pointUIPanel = candidate.gameObject;
            }
            else if (playerText1 == null && candidate.CompareTag(PlayerText1Tag))
            {
                playerText1 = candidate.GetComponent<TMPro.TMP_Text>();
            }
            else if (playerText2 == null && candidate.CompareTag(PlayerText2Tag))
            {
                playerText2 = candidate.GetComponent<TMPro.TMP_Text>();
            }
            else if (playerText3 == null && candidate.CompareTag(PlayerText3Tag))
            {
                playerText3 = candidate.GetComponent<TMPro.TMP_Text>();
            }

            CacheUpgradeButton(candidate);
            CachePointIndicator(candidate);
        }
    }

    private void CachePointIndicator(Transform candidate)
    {
        int playerNumber;
        int pointIndex;

        switch (candidate.tag)
        {
            case "Player1P1": playerNumber = 1; pointIndex = 0; break;
            case "Player1P2": playerNumber = 1; pointIndex = 1; break;
            case "Player1P3": playerNumber = 1; pointIndex = 2; break;
            case "Player2P1": playerNumber = 2; pointIndex = 0; break;
            case "Player2P2": playerNumber = 2; pointIndex = 1; break;
            case "Player2P3": playerNumber = 2; pointIndex = 2; break;
            case "Player3P1": playerNumber = 3; pointIndex = 0; break;
            case "Player3P2": playerNumber = 3; pointIndex = 1; break;
            case "Player3P3": playerNumber = 3; pointIndex = 2; break;
            case "Player4P1": playerNumber = 4; pointIndex = 0; break;
            case "Player4P2": playerNumber = 4; pointIndex = 1; break;
            case "Player4P3": playerNumber = 4; pointIndex = 2; break;
            default: return;
        }

        if (pointIndicators[playerNumber, pointIndex] == null)
        {
            pointIndicators[playerNumber, pointIndex] =
                candidate.GetComponent<UnityEngine.UI.Graphic>();
        }
    }

    private void CacheUpgradeButton(Transform candidate)
    {
        int row;
        int column;

        switch (candidate.tag)
        {
            case "Player1B1": row = 0; column = 0; break;
            case "Player1B2": row = 0; column = 1; break;
            case "Player1B3": row = 0; column = 2; break;
            case "Player2B1": row = 1; column = 0; break;
            case "Player2B2": row = 1; column = 1; break;
            case "Player2B3": row = 1; column = 2; break;
            case "Player3B1": row = 2; column = 0; break;
            case "Player3B2": row = 2; column = 1; break;
            case "Player3B3": row = 2; column = 2; break;
            default: return;
        }

        if (upgradeButtons[row, column] == null)
        {
            upgradeButtons[row, column] =
                candidate.GetComponent<UnityEngine.UI.Button>();
        }
    }

    private void CheckForLastPlayerStanding()
    {
        if (upgradeUIRevealed)
        {
            return;
        }

        int groundedPlayerTypes = 0;
        int lastGroundedPlayerNumber = 0;

        for (int playerNumber = 1; playerNumber <= 4; playerNumber++)
        {
            foreach (PlayersMovement player in PlayersMovement.ActivePlayers)
            {
                if (player != null
                    && player.isActiveAndEnabled
                    && player.IsTouchingFloor
                    && player.CompareTag($"Player{playerNumber}"))
                {
                    groundedPlayerTypes++;
                    lastGroundedPlayerNumber = playerNumber;
                    break;
                }
            }
        }

        if (groundedPlayerTypes >= 2)
        {
            roundHadMultipleGroundedPlayerTypes = true;
            return;
        }

        if (roundHadMultipleGroundedPlayerTypes && groundedPlayerTypes == 1)
        {
            RevealUpgradeUI(lastGroundedPlayerNumber);
        }
    }

    private void RevealUpgradeUI(int winningPlayerNumber)
    {
        CacheUpgradeUI();
        roundWinningPlayerNumber = winningPlayerNumber;

        if (upgradeUIPanel == null)
        {
            upgradeUIRevealed = true;
            Debug.LogWarning("No UI Panel tagged UpgradeUI was found.", this);
            return;
        }

        TMPro.TMP_Text[] playerTexts = { playerText1, playerText2, playerText3 };
        int textIndex = 0;
        selectedUpgradeRows = 0;

        for (int playerNumber = 1; playerNumber <= 4; playerNumber++)
        {
            if (playerNumber == winningPlayerNumber)
            {
                continue;
            }

            if (playerTexts[textIndex] != null)
            {
                playerTexts[textIndex].text = $"Player: {playerNumber}";
            }

            upgradePlayerNumbers[textIndex] = playerNumber;
            bool playerIsInactive = playerNumber > activePlayerCount;
            upgradeRowsSelected[textIndex] = playerIsInactive;

            if (playerIsInactive)
            {
                selectedUpgradeRows++;
            }

            textIndex++;
        }

        PopulateUpgradeButtons();
        upgradeUIRevealed = true;
        upgradeUIPanel.SetActive(true);
    }

    private void PopulateUpgradeButtons()
    {
        for (int row = 0; row < 3; row++)
        {
            int playerNumber = upgradePlayerNumbers[row];

            if (upgradeRowsSelected[row])
            {
                SetUpgradeRowVisible(row, false);
                continue;
            }

            System.Collections.Generic.List<UpgradeType> eligibleUpgrades =
                new System.Collections.Generic.List<UpgradeType>();

            foreach (UpgradeType upgrade in allUpgrades)
            {
                if (!IsUpgradeAlreadyOwned(upgrade, playerNumber))
                {
                    eligibleUpgrades.Add(upgrade);
                }
            }

            for (int index = eligibleUpgrades.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                UpgradeType temporary = eligibleUpgrades[index];
                eligibleUpgrades[index] = eligibleUpgrades[swapIndex];
                eligibleUpgrades[swapIndex] = temporary;
            }

            for (int column = 0; column < 3; column++)
            {
                UnityEngine.UI.Button button = upgradeButtons[row, column];

                if (button == null)
                {
                    continue;
                }

                bool hasUpgradeChoice = column < eligibleUpgrades.Count;
                button.gameObject.SetActive(hasUpgradeChoice);

                if (!hasUpgradeChoice)
                {
                    continue;
                }

                UpgradeType upgrade = eligibleUpgrades[column];
                offeredUpgrades[row, column] = upgrade;

                SetUpgradeButtonText(button, upgrade);

                int selectedRow = row;
                int selectedColumn = column;
                button.onClick.AddListener(
                    () => SelectUpgrade(selectedRow, selectedColumn));
            }
        }
    }

    private static void SetUpgradeButtonText(
        UnityEngine.UI.Button button,
        UpgradeType upgrade)
    {
        TMPro.TMP_Text nameText = null;
        TMPro.TMP_Text descriptionText = null;
        TMPro.TMP_Text firstNonNameText = null;

        foreach (TMPro.TMP_Text text in
            button.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (text.gameObject.name == "Text (TMP)")
            {
                nameText = text;
                continue;
            }

            if (firstNonNameText == null)
            {
                firstNonNameText = text;
            }

            if (text.gameObject.name.IndexOf(
                    "Description",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                descriptionText = text;
            }
        }

        if (nameText != null)
        {
            nameText.text = GetUpgradeName(upgrade);
        }

        descriptionText ??= firstNonNameText;

        if (descriptionText != null)
        {
            descriptionText.text = GetUpgradeDescription(upgrade);
        }
    }

    private static bool IsUpgradeAlreadyOwned(
        UpgradeType upgrade,
        int playerNumber)
    {
        switch (upgrade)
        {
            case UpgradeType.MiniGun:
            case UpgradeType.Dodger:
                return false;
            case UpgradeType.BigOleTank:
                return IsBigOleTankActive(playerNumber);
            case UpgradeType.Sniper:
                return IsSniperActive(playerNumber);
            case UpgradeType.Homing:
                return IsHomingActive(playerNumber);
            case UpgradeType.ChainWave:
                return IsChainWaveActive(playerNumber);
            case UpgradeType.Clone:
                return IsCloneActive(playerNumber);
            case UpgradeType.BoomerBoi:
                return IsBoomerBoiActive(playerNumber);
            case UpgradeType.BlackHole:
                return IsBlackHoleActive(playerNumber);
            case UpgradeType.RocketShip:
                return IsRocketShipActive(playerNumber);
            default:
                return true;
        }
    }

    private void SelectUpgrade(int row, int column)
    {
        if (row < 0
            || row >= 3
            || column < 0
            || column >= 3
            || upgradeRowsSelected[row])
        {
            return;
        }

        int playerNumber = upgradePlayerNumbers[row];

        if (playerNumber < 1 || playerNumber > activePlayerCount)
        {
            return;
        }

        ActivateUpgrade(offeredUpgrades[row, column], playerNumber);
        SyncInspectorUpgradeFields();
        upgradeRowsSelected[row] = true;
        selectedUpgradeRows++;

        SetUpgradeRowVisible(row, false);

        if (selectedUpgradeRows >= 3)
        {
            StartCoroutine(ShowPointsThenReload());
        }
    }

    private System.Collections.IEnumerator ShowPointsThenReload()
    {
        if (pointSequenceStarted)
        {
            yield break;
        }

        pointSequenceStarted = true;

        if (upgradeUIPanel != null)
        {
            upgradeUIPanel.SetActive(false);
        }

        CacheUpgradeUI();

        if (roundWinningPlayerNumber >= 1 && roundWinningPlayerNumber <= 4)
        {
            playerRoundPoints[roundWinningPlayerNumber] = Mathf.Min(
                3,
                playerRoundPoints[roundWinningPlayerNumber] + 1);
            playerTotalPoints[roundWinningPlayerNumber]++;
        }

        UpdatePointColors();

        bool roundCompleted = roundWinningPlayerNumber >= 1
            && roundWinningPlayerNumber <= activePlayerCount
            && playerRoundPoints[roundWinningPlayerNumber] >= 3;
        bool matchCompleted = roundCompleted
            && completedRounds + 1 >= numberOfRounds;

        if (roundCompleted)
        {
            completedRounds++;
        }

        if (matchCompleted)
        {
            matchWinningPlayerNumber = FindMatchWinner(
                roundWinningPlayerNumber);
            ShowFinalMatchResults();
        }

        if (pointUIPanel != null)
        {
            pointUIPanel.SetActive(true);

            if (matchCompleted)
            {
                Debug.Log(
                    $"Player {matchWinningPlayerNumber} wins with "
                    + $"{playerTotalPoints[matchWinningPlayerNumber]} total points.",
                    this);
                yield break;
            }

            yield return new WaitForSecondsRealtime(5f);
            pointUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No UI Panel tagged Point UI was found.", this);
        }

        if (matchCompleted)
        {
            Debug.Log(
                $"Player {matchWinningPlayerNumber} wins with "
                + $"{playerTotalPoints[matchWinningPlayerNumber]} total points.",
                this);
            yield break;
        }

        if (roundCompleted)
        {
            ClearVisibleRoundPoints();
        }

        ReloadRound();
    }

    private static int FindMatchWinner(int tieBreakerPlayerNumber)
    {
        int winningPlayer = 1;
        int highestScore = playerTotalPoints[winningPlayer];

        for (int playerNumber = 2;
             playerNumber <= activePlayerCount;
             playerNumber++)
        {
            int score = playerTotalPoints[playerNumber];

            if (score > highestScore
                || (score == highestScore
                    && playerNumber == tieBreakerPlayerNumber))
            {
                winningPlayer = playerNumber;
                highestScore = score;
            }
        }

        return winningPlayer;
    }

    private void ShowFinalMatchResults()
    {
        if (pointUIPanel == null)
        {
            return;
        }

        for (int playerNumber = 1;
             playerNumber <= activePlayerCount;
             playerNumber++)
        {
            Transform labelRoot = pointUIPanel.transform.Find(
                $"PlayerP{playerNumber} Text");
            TMPro.TMP_Text label = labelRoot == null
                ? null
                : labelRoot.GetComponentInChildren<TMPro.TMP_Text>(true);

            if (label == null)
            {
                continue;
            }

            label.text = playerNumber == matchWinningPlayerNumber
                ? $"PLAYER {playerNumber} WINS!\nTotal: {playerTotalPoints[playerNumber]}"
                : $"Player {playerNumber}\nTotal: {playerTotalPoints[playerNumber]}";
        }
    }

    private static void ClearVisibleRoundPoints()
    {
        for (int playerNumber = 1; playerNumber <= 4; playerNumber++)
        {
            playerRoundPoints[playerNumber] = 0;
        }
    }

    private void UpdatePointColors()
    {
        for (int playerNumber = 1; playerNumber <= 4; playerNumber++)
        {
            for (int pointIndex = 0; pointIndex < 3; pointIndex++)
            {
                UnityEngine.UI.Graphic point =
                    pointIndicators[playerNumber, pointIndex];

                if (point != null)
                {
                    point.color = playerNumber <= activePlayerCount
                        && pointIndex < playerRoundPoints[playerNumber]
                        ? Color.green
                        : Color.red;
                }
            }
        }
    }

    private void ReloadRound()
    {
        if (upgradeUIPanel != null)
        {
            upgradeUIPanel.SetActive(false);
        }

        if (pointUIPanel != null)
        {
            pointUIPanel.SetActive(false);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public static void SetActivePlayerCount(int playerCount)
    {
        activePlayerCount = Mathf.Clamp(playerCount, 2, 4);
        PlayerPrefs.SetInt(ActivePlayerCountPreference, activePlayerCount);
        PlayerPrefs.Save();

        for (int playerNumber = activePlayerCount + 1;
             playerNumber <= 4;
             playerNumber++)
        {
            playerRoundPoints[playerNumber] = 0;
            playerTotalPoints[playerNumber] = 0;
        }

        ApplyActivePlayerCountToLoadedScenes();
    }

    public static void SetNumberOfRounds(int rounds)
    {
        numberOfRounds = Mathf.Clamp(rounds, 1, 3);
        PlayerPrefs.SetInt(NumberOfRoundsPreference, numberOfRounds);
        PlayerPrefs.Save();

        completedRounds = 0;
        matchWinningPlayerNumber = 0;
        ClearVisibleRoundPoints();

        for (int playerNumber = 1; playerNumber <= 4; playerNumber++)
        {
            playerTotalPoints[playerNumber] = 0;
        }
    }

    public static void SetRandomStartingUpgradesEnabled(bool enabled)
    {
        randomStartingUpgradesEnabled = enabled;
    }

    public static void ToggleRandomStartingUpgrades()
    {
        randomStartingUpgradesEnabled = !randomStartingUpgradesEnabled;
    }

    private static void GrantRandomStartingUpgrades()
    {
        if (!randomStartingUpgradesEnabled || randomStartingUpgradesGranted)
        {
            return;
        }

        randomStartingUpgradesGranted = true;

        for (int playerNumber = 1;
             playerNumber <= activePlayerCount;
             playerNumber++)
        {
            System.Collections.Generic.List<UpgradeType> eligibleUpgrades =
                new System.Collections.Generic.List<UpgradeType>();

            foreach (UpgradeType upgrade in allUpgrades)
            {
                if (!IsUpgradeAlreadyOwned(upgrade, playerNumber))
                {
                    eligibleUpgrades.Add(upgrade);
                }
            }

            if (eligibleUpgrades.Count == 0)
            {
                continue;
            }

            UpgradeType randomUpgrade = eligibleUpgrades[
                Random.Range(0, eligibleUpgrades.Count)];
            ActivateUpgrade(randomUpgrade, playerNumber);
        }
    }

    private void SetUpgradeRowVisible(int row, bool visible)
    {
        for (int column = 0; column < 3; column++)
        {
            UnityEngine.UI.Button button = upgradeButtons[row, column];

            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }

    public static void ApplyActivePlayerCountToLoadedScenes()
    {
        RemoveInactivePlayersAndShields();
    }

    private static void RemoveInactivePlayersAndShields()
    {
        for (int playerNumber = activePlayerCount + 1;
             playerNumber <= 4;
             playerNumber++)
        {
            DisableAndDestroyTaggedObjects($"Player{playerNumber}");
            DisableAndDestroyTaggedObjects($"shield{playerNumber}");
        }
    }

    private static void DisableAndDestroyTaggedObjects(string objectTag)
    {
        foreach (Transform candidate in FindObjectsOfType<Transform>(true))
        {
            GameObject target = candidate.gameObject;

            if (!target.scene.isLoaded || !target.CompareTag(objectTag))
            {
                continue;
            }

            target.SetActive(false);
            Destroy(target);
        }
    }

    private static void ActivateUpgrade(UpgradeType upgrade, int playerNumber)
    {
        switch (upgrade)
        {
            case UpgradeType.MiniGun: ActivateMiniGun(playerNumber); break;
            case UpgradeType.Dodger: ActivateDodger(playerNumber); break;
            case UpgradeType.BigOleTank: ActivateBigOleTank(playerNumber); break;
            case UpgradeType.Sniper: ActivateSniper(playerNumber); break;
            case UpgradeType.Homing: ActivateHoming(playerNumber); break;
            case UpgradeType.ChainWave: ActivateChainWave(playerNumber); break;
            case UpgradeType.Clone: ActivateClone(playerNumber); break;
            case UpgradeType.BoomerBoi: ActivateBoomerBoi(playerNumber); break;
            case UpgradeType.BlackHole: ActivateBlackHole(playerNumber); break;
            case UpgradeType.RocketShip: ActivateRocketShip(playerNumber); break;
        }
    }

    private static string GetUpgradeName(UpgradeType upgrade)
    {
        switch (upgrade)
        {
            case UpgradeType.MiniGun: return "MiniGun";
            case UpgradeType.Dodger: return "Dodger";
            case UpgradeType.BigOleTank: return "Big O'le Tank";
            case UpgradeType.Sniper: return "Sniper";
            case UpgradeType.Homing: return "Homing";
            case UpgradeType.ChainWave: return "Chain Wave";
            case UpgradeType.Clone: return "Clone";
            case UpgradeType.BoomerBoi: return "BoomerBoi";
            case UpgradeType.BlackHole: return "BlackHole";
            case UpgradeType.RocketShip: return "RocketShip";
            default: return upgrade.ToString();
        }
    }

    private static string GetUpgradeDescription(UpgradeType upgrade)
    {
        switch (upgrade)
        {
            case UpgradeType.Homing:
                return "Slowly shield moves toward closest non-user player\n"
                    + "-1 shield regeneration";
            case UpgradeType.MiniGun:
                return "+1 shield regeneration\n"
                    + "-1.5 shield size";
            case UpgradeType.BoomerBoi:
                return "Makes it so that the shield explodes when it gets near a player or shield\n"
                    + "-1 shield regeneration\n"
                    + "+1 shield size\n"
                    + "-5 shield speed";
            case UpgradeType.Sniper:
                return "-1 shield size\n"
                    + "+10 shield speed\n"
                    + "For every kill, you get\n"
                    + "-0.25 shield size\n"
                    + "+0.25 shield regeneration\n"
                    + "+2 shield speed";
            case UpgradeType.BlackHole:
                return "Figure it out, Sherlock.\n"
                    + "-2 player speed\n"
                    + "-2 shield regeneration\n"
                    + "+2 shield size";
            case UpgradeType.Dodger:
                return "-0.5 shield size\n"
                    + "+2.5 player speed\n"
                    + "+0.5 shield regeneration";
            case UpgradeType.Clone:
                return "Second Character under your control\n"
                    + "-2 player speed\n"
                    + "-2 shield size";
            case UpgradeType.BigOleTank:
                return "Cannot Move\n"
                    + "-2 shield regeneration\n"
                    + "-5 shield speed\n"
                    + "+10 shield size";
            case UpgradeType.RocketShip:
                return "Immune to all blackhole effects\n"
                    + "Sets all shield regeneration to 10 permanently\n"
                    + "Player Speed Increases by 1 every second you are constantly moving. Buff lost when you stop moving";
            case UpgradeType.ChainWave:
                return "(:";
            default:
                return string.Empty;
        }
    }

    private void SyncInspectorUpgradeFields()
    {
        player1MiniGun = miniGunLevels[1] > 0;
        player2MiniGun = miniGunLevels[2] > 0;
        player3MiniGun = miniGunLevels[3] > 0;
        player4MiniGun = miniGunLevels[4] > 0;
        player1MiniGunStacks = miniGunLevels[1];
        player2MiniGunStacks = miniGunLevels[2];
        player3MiniGunStacks = miniGunLevels[3];
        player4MiniGunStacks = miniGunLevels[4];

        player1Dodger = dodgerLevels[1] > 0;
        player2Dodger = dodgerLevels[2] > 0;
        player3Dodger = dodgerLevels[3] > 0;
        player4Dodger = dodgerLevels[4] > 0;
        player1DodgerStacks = dodgerLevels[1];
        player2DodgerStacks = dodgerLevels[2];
        player3DodgerStacks = dodgerLevels[3];
        player4DodgerStacks = dodgerLevels[4];

        player1BigOleTank = bigOleTankActive[1];
        player2BigOleTank = bigOleTankActive[2];
        player3BigOleTank = bigOleTankActive[3];
        player4BigOleTank = bigOleTankActive[4];

        player1Sniper = sniperActive[1];
        player2Sniper = sniperActive[2];
        player3Sniper = sniperActive[3];
        player4Sniper = sniperActive[4];

        player1Homing = homingActive[1];
        player2Homing = homingActive[2];
        player3Homing = homingActive[3];
        player4Homing = homingActive[4];

        player1ChainWave = chainWaveActive[1];
        player2ChainWave = chainWaveActive[2];
        player3ChainWave = chainWaveActive[3];
        player4ChainWave = chainWaveActive[4];

        player1Clone = cloneActive[1];
        player2Clone = cloneActive[2];
        player3Clone = cloneActive[3];
        player4Clone = cloneActive[4];

        player1BoomerBoi = boomerBoiActive[1];
        player2BoomerBoi = boomerBoiActive[2];
        player3BoomerBoi = boomerBoiActive[3];
        player4BoomerBoi = boomerBoiActive[4];

        player1BlackHole = blackHoleActive[1];
        player2BlackHole = blackHoleActive[2];
        player3BlackHole = blackHoleActive[3];
        player4BlackHole = blackHoleActive[4];

        player1RocketShip = rocketShipActive[1];
        player2RocketShip = rocketShipActive[2];
        player3RocketShip = rocketShipActive[3];
        player4RocketShip = rocketShipActive[4];
    }

    private void ActivateInspectorUpgrades()
    {
        if (player1MiniGun && GetMiniGunLevel(1) == 0)
        {
            ActivateMiniGun(1);
        }

        if (player2MiniGun && GetMiniGunLevel(2) == 0)
        {
            ActivateMiniGun(2);
        }

        if (player3MiniGun && GetMiniGunLevel(3) == 0)
        {
            ActivateMiniGun(3);
        }

        if (player4MiniGun && GetMiniGunLevel(4) == 0)
        {
            ActivateMiniGun(4);
        }

        if (player1Dodger && GetDodgerLevel(1) == 0)
        {
            ActivateDodger(1);
        }

        if (player2Dodger && GetDodgerLevel(2) == 0)
        {
            ActivateDodger(2);
        }

        if (player3Dodger && GetDodgerLevel(3) == 0)
        {
            ActivateDodger(3);
        }

        if (player4Dodger && GetDodgerLevel(4) == 0)
        {
            ActivateDodger(4);
        }

        if (player1BigOleTank)
        {
            ActivateBigOleTank(1);
        }

        if (player2BigOleTank)
        {
            ActivateBigOleTank(2);
        }

        if (player3BigOleTank)
        {
            ActivateBigOleTank(3);
        }

        if (player4BigOleTank)
        {
            ActivateBigOleTank(4);
        }

        if (player1Sniper)
        {
            ActivateSniper(1);
        }

        if (player2Sniper)
        {
            ActivateSniper(2);
        }

        if (player3Sniper)
        {
            ActivateSniper(3);
        }

        if (player4Sniper)
        {
            ActivateSniper(4);
        }

        if (player1Homing)
        {
            ActivateHoming(1);
        }

        if (player2Homing)
        {
            ActivateHoming(2);
        }

        if (player3Homing)
        {
            ActivateHoming(3);
        }

        if (player4Homing)
        {
            ActivateHoming(4);
        }

        if (player1ChainWave)
        {
            ActivateChainWave(1);
        }

        if (player2ChainWave)
        {
            ActivateChainWave(2);
        }

        if (player3ChainWave)
        {
            ActivateChainWave(3);
        }

        if (player4ChainWave)
        {
            ActivateChainWave(4);
        }

        if (player1Clone)
        {
            ActivateClone(1);
        }

        if (player2Clone)
        {
            ActivateClone(2);
        }

        if (player3Clone)
        {
            ActivateClone(3);
        }

        if (player4Clone)
        {
            ActivateClone(4);
        }

        if (player1BoomerBoi)
        {
            ActivateBoomerBoi(1);
        }

        if (player2BoomerBoi)
        {
            ActivateBoomerBoi(2);
        }

        if (player3BoomerBoi)
        {
            ActivateBoomerBoi(3);
        }

        if (player4BoomerBoi)
        {
            ActivateBoomerBoi(4);
        }

        if (player1BlackHole)
        {
            ActivateBlackHole(1);
        }

        if (player2BlackHole)
        {
            ActivateBlackHole(2);
        }

        if (player3BlackHole)
        {
            ActivateBlackHole(3);
        }

        if (player4BlackHole)
        {
            ActivateBlackHole(4);
        }

        if (player1RocketShip)
        {
            ActivateRocketShip(1);
        }

        if (player2RocketShip)
        {
            ActivateRocketShip(2);
        }

        if (player3RocketShip)
        {
            ActivateRocketShip(3);
        }

        if (player4RocketShip)
        {
            ActivateRocketShip(4);
        }
    }

    public static void ActivateMiniGun(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            miniGunLevels[playerNumber]++;
        }
    }

    public static bool IsMiniGunActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && miniGunLevels[playerNumber] > 0;
    }

    public static int GetMiniGunLevel(int playerNumber)
    {
        return playerNumber >= 1 && playerNumber <= 4
            ? miniGunLevels[playerNumber]
            : 0;
    }

    public static void ActivateDodger(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            dodgerLevels[playerNumber]++;
        }
    }

    public static bool IsDodgerActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && dodgerLevels[playerNumber] > 0;
    }

    public static int GetDodgerLevel(int playerNumber)
    {
        return playerNumber >= 1 && playerNumber <= 4
            ? dodgerLevels[playerNumber]
            : 0;
    }

    public static void ActivateBigOleTank(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            bigOleTankActive[playerNumber] = true;
        }
    }

    public static bool IsBigOleTankActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && bigOleTankActive[playerNumber];
    }

    public static void ActivateSniper(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            sniperActive[playerNumber] = true;
        }
    }

    public static bool IsSniperActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && sniperActive[playerNumber];
    }

    public static void RegisterSniperHit(int playerNumber)
    {
        if (IsSniperActive(playerNumber))
        {
            sniperHitCount[playerNumber]++;
        }
    }

    public static int GetSniperHitCount(int playerNumber)
    {
        return playerNumber >= 1 && playerNumber <= 4
            ? sniperHitCount[playerNumber]
            : 0;
    }

    public static void ActivateHoming(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            homingActive[playerNumber] = true;
        }
    }

    public static bool IsHomingActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && homingActive[playerNumber];
    }

    public static void SteerShieldTowardNearestPlayer(
        Rigidbody2D shieldBody,
        string ownerTag,
        float degreesPerSecond)
    {
        float speed = shieldBody.velocity.magnitude;

        if (speed <= 0.001f)
        {
            return;
        }

        Transform nearestPlayer = null;
        float nearestDistanceSquared = float.PositiveInfinity;

        for (int playerNumber = 1; playerNumber <= 4; playerNumber++)
        {
            string playerTag = $"Player{playerNumber}";

            if (playerTag == ownerTag)
            {
                continue;
            }

            foreach (GameObject player in GameObject.FindGameObjectsWithTag(playerTag))
            {
                float distanceSquared = ((Vector2)player.transform.position - shieldBody.position).sqrMagnitude;

                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestPlayer = player.transform;
                }
            }
        }

        if (nearestPlayer == null)
        {
            return;
        }

        Vector2 targetDirection = (Vector2)nearestPlayer.position - shieldBody.position;

        if (targetDirection.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector3 steeredDirection = Vector3.RotateTowards(
            shieldBody.velocity.normalized,
            targetDirection.normalized,
            degreesPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime,
            0f);

        shieldBody.velocity = ((Vector2)steeredDirection).normalized * speed;
    }

    public static void ActivateChainWave(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            chainWaveActive[playerNumber] = true;
        }
    }

    public static bool IsChainWaveActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && chainWaveActive[playerNumber];
    }

    public static void TriggerChainWave(int playerNumber)
    {
        if (!IsChainWaveActive(playerNumber))
        {
            return;
        }

        float lockEndTime = Time.time + 0.5f;

        for (int targetPlayer = 1; targetPlayer <= 4; targetPlayer++)
        {
            if (targetPlayer != playerNumber)
            {
                chainWaveMovementLockEndTime[targetPlayer] = Mathf.Max(
                    chainWaveMovementLockEndTime[targetPlayer],
                    lockEndTime);
            }
        }
    }

    public static bool IsPlayerMovementLockedByChainWave(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && Time.time < chainWaveMovementLockEndTime[playerNumber];
    }

    public static void ActivateClone(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            cloneActive[playerNumber] = true;
        }
    }

    public static bool IsCloneActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && cloneActive[playerNumber];
    }

    public static void ActivateBoomerBoi(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            boomerBoiActive[playerNumber] = true;
        }
    }

    public static bool IsBoomerBoiActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && boomerBoiActive[playerNumber];
    }

    public static void ActivateBlackHole(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            blackHoleActive[playerNumber] = true;
        }
    }

    public static bool IsBlackHoleActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && blackHoleActive[playerNumber];
    }

    public static void ActivateRocketShip(int playerNumber)
    {
        if (playerNumber >= 1 && playerNumber <= 4)
        {
            rocketShipActive[playerNumber] = true;
        }
    }

    public static bool IsRocketShipActive(int playerNumber)
    {
        return playerNumber >= 1
            && playerNumber <= 4
            && rocketShipActive[playerNumber];
    }

    public static bool TriggerBoomerBoiExpansionFromContact(GameObject shield)
    {
        if (shield.TryGetComponent(out FollowPlayer1 player1))
        {
            return player1.TriggerBoomerBoiExpansionFromContact();
        }

        if (shield.TryGetComponent(out FollowPlayer2 player2))
        {
            return player2.TriggerBoomerBoiExpansionFromContact();
        }

        if (shield.TryGetComponent(out FollowPlayer3 player3))
        {
            return player3.TriggerBoomerBoiExpansionFromContact();
        }

        if (shield.TryGetComponent(out FollowPlayer4 player4))
        {
            return player4.TriggerBoomerBoiExpansionFromContact();
        }

        return false;
    }

    public static bool HasBoomerBoiTargetNearby(
        GameObject shield,
        string ownerTag,
        Collider2D[] proximityResults)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            shield.transform.position,
            0.5f,
            proximityResults);

        for (int index = 0; index < hitCount; index++)
        {
            Collider2D hit = proximityResults[index];

            if (hit == null)
            {
                continue;
            }

            GameObject target = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (target == shield)
            {
                continue;
            }

            bool isShield = target.CompareTag("shield1")
                || target.CompareTag("shield2")
                || target.CompareTag("shield3")
                || target.CompareTag("shield4");

            bool isEnemyPlayer = !target.CompareTag(ownerTag)
                && (target.CompareTag("Player1")
                    || target.CompareTag("Player2")
                    || target.CompareTag("Player3")
                    || target.CompareTag("Player4"));

            if (isShield || isEnemyPlayer)
            {
                return true;
            }
        }

        return false;
    }

    public void ActivatePlayer1MiniGun()
    {
        player1MiniGun = true;
        ActivateMiniGun(1);
    }

    public void ActivatePlayer2MiniGun()
    {
        player2MiniGun = true;
        ActivateMiniGun(2);
    }

    public void ActivatePlayer3MiniGun()
    {
        player3MiniGun = true;
        ActivateMiniGun(3);
    }

    public void ActivatePlayer4MiniGun()
    {
        player4MiniGun = true;
        ActivateMiniGun(4);
    }

    public void ActivatePlayer1Dodger()
    {
        player1Dodger = true;
        ActivateDodger(1);
    }

    public void ActivatePlayer2Dodger()
    {
        player2Dodger = true;
        ActivateDodger(2);
    }

    public void ActivatePlayer3Dodger()
    {
        player3Dodger = true;
        ActivateDodger(3);
    }

    public void ActivatePlayer4Dodger()
    {
        player4Dodger = true;
        ActivateDodger(4);
    }

    public void ActivatePlayer1BigOleTank()
    {
        player1BigOleTank = true;
        ActivateBigOleTank(1);
    }

    public void ActivatePlayer2BigOleTank()
    {
        player2BigOleTank = true;
        ActivateBigOleTank(2);
    }

    public void ActivatePlayer3BigOleTank()
    {
        player3BigOleTank = true;
        ActivateBigOleTank(3);
    }

    public void ActivatePlayer4BigOleTank()
    {
        player4BigOleTank = true;
        ActivateBigOleTank(4);
    }

    public void ActivatePlayer1Sniper()
    {
        player1Sniper = true;
        ActivateSniper(1);
    }

    public void ActivatePlayer2Sniper()
    {
        player2Sniper = true;
        ActivateSniper(2);
    }

    public void ActivatePlayer3Sniper()
    {
        player3Sniper = true;
        ActivateSniper(3);
    }

    public void ActivatePlayer4Sniper()
    {
        player4Sniper = true;
        ActivateSniper(4);
    }

    public void ActivatePlayer1Homing()
    {
        player1Homing = true;
        ActivateHoming(1);
    }

    public void ActivatePlayer2Homing()
    {
        player2Homing = true;
        ActivateHoming(2);
    }

    public void ActivatePlayer3Homing()
    {
        player3Homing = true;
        ActivateHoming(3);
    }

    public void ActivatePlayer4Homing()
    {
        player4Homing = true;
        ActivateHoming(4);
    }

    public void ActivatePlayer1ChainWave()
    {
        player1ChainWave = true;
        ActivateChainWave(1);
    }

    public void ActivatePlayer2ChainWave()
    {
        player2ChainWave = true;
        ActivateChainWave(2);
    }

    public void ActivatePlayer3ChainWave()
    {
        player3ChainWave = true;
        ActivateChainWave(3);
    }

    public void ActivatePlayer4ChainWave()
    {
        player4ChainWave = true;
        ActivateChainWave(4);
    }

    public void ActivatePlayer1Clone()
    {
        player1Clone = true;
        ActivateClone(1);
    }

    public void ActivatePlayer2Clone()
    {
        player2Clone = true;
        ActivateClone(2);
    }

    public void ActivatePlayer3Clone()
    {
        player3Clone = true;
        ActivateClone(3);
    }

    public void ActivatePlayer4Clone()
    {
        player4Clone = true;
        ActivateClone(4);
    }

    public void ActivatePlayer1BoomerBoi()
    {
        player1BoomerBoi = true;
        ActivateBoomerBoi(1);
    }

    public void ActivatePlayer2BoomerBoi()
    {
        player2BoomerBoi = true;
        ActivateBoomerBoi(2);
    }

    public void ActivatePlayer3BoomerBoi()
    {
        player3BoomerBoi = true;
        ActivateBoomerBoi(3);
    }

    public void ActivatePlayer4BoomerBoi()
    {
        player4BoomerBoi = true;
        ActivateBoomerBoi(4);
    }

    public void ActivatePlayer1BlackHole()
    {
        player1BlackHole = true;
        ActivateBlackHole(1);
    }

    public void ActivatePlayer2BlackHole()
    {
        player2BlackHole = true;
        ActivateBlackHole(2);
    }

    public void ActivatePlayer3BlackHole()
    {
        player3BlackHole = true;
        ActivateBlackHole(3);
    }

    public void ActivatePlayer4BlackHole()
    {
        player4BlackHole = true;
        ActivateBlackHole(4);
    }

    public void ActivatePlayer1RocketShip()
    {
        player1RocketShip = true;
        ActivateRocketShip(1);
    }

    public void ActivatePlayer2RocketShip()
    {
        player2RocketShip = true;
        ActivateRocketShip(2);
    }

    public void ActivatePlayer3RocketShip()
    {
        player3RocketShip = true;
        ActivateRocketShip(3);
    }

    public void ActivatePlayer4RocketShip()
    {
        player4RocketShip = true;
        ActivateRocketShip(4);
    }
}
