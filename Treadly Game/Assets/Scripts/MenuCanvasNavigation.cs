using System;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuCanvasNavigation
{
    private static readonly System.Collections.Generic.HashSet<int>
        configuredSceneHandles = new System.Collections.Generic.HashSet<int>();

    private const string MainUITag = "Main UI";
    private const string ControlsUITag = "Controls UI";
    private const string ControlsButtonName = "Controls Button";
    private const string BackButtonName = "Back Button Controls";
    private const string SettingsMenuName = "Settings Menu";
    private const string SettingsButtonName = "Settings Button";
    private const string BackSettingsButtonName = "Back Button Settings";
    private const string RandomUpgradeToggleName = "Toggle";
    private const string StartButtonName = "Start Button";
    private const string QuitButtonName = "Quit Button";
    private const string ArenaSceneName = "Areana";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CloseGameTab();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSceneListener()
    {
        SceneManager.sceneLoaded -= ConfigureScene;
        configuredSceneHandles.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneListener()
    {
        SceneManager.sceneLoaded -= ConfigureScene;
        SceneManager.sceneLoaded += ConfigureScene;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureInitialScene()
    {
        ConfigureScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void ConfigureScene(Scene scene, LoadSceneMode loadMode)
    {
        GameManager.ApplyActivePlayerCountToLoadedScenes();

        if (!configuredSceneHandles.Add(scene.handle))
        {
            return;
        }

        GameObject mainUI = null;
        GameObject controlsUI = null;
        GameObject settingsUI = null;
        UnityEngine.UI.Button controlsButton = null;
        UnityEngine.UI.Button backButton = null;
        UnityEngine.UI.Button settingsButton = null;
        UnityEngine.UI.Button backSettingsButton = null;
        UnityEngine.UI.Button randomUpgradeButton = null;
        UnityEngine.UI.Toggle randomUpgradeToggle = null;
        UnityEngine.UI.Button startButton = null;
        UnityEngine.UI.Button quitButton = null;

        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.gameObject.scene != scene)
            {
                continue;
            }

            if (candidate.CompareTag(MainUITag))
            {
                mainUI = candidate.gameObject;
            }
            else if (candidate.CompareTag(ControlsUITag))
            {
                controlsUI = candidate.gameObject;
            }

            if (candidate.name.Equals(
                    SettingsMenuName,
                    StringComparison.OrdinalIgnoreCase))
            {
                settingsUI = candidate.gameObject;
            }

            if (candidate.name.Equals(
                    ControlsButtonName,
                    StringComparison.OrdinalIgnoreCase))
            {
                controlsButton = candidate.GetComponent<UnityEngine.UI.Button>();
            }
            else if (candidate.name.Equals(
                    BackButtonName,
                    StringComparison.OrdinalIgnoreCase))
            {
                backButton = candidate.GetComponent<UnityEngine.UI.Button>();
            }
            else if (candidate.name.Equals(
                    SettingsButtonName,
                    StringComparison.OrdinalIgnoreCase))
            {
                settingsButton = candidate.GetComponent<UnityEngine.UI.Button>();
            }
            else if (candidate.name.Equals(
                    BackSettingsButtonName,
                    StringComparison.OrdinalIgnoreCase))
            {
                backSettingsButton = candidate.GetComponent<UnityEngine.UI.Button>();
            }
            else if (candidate.name.Equals(
                    RandomUpgradeToggleName,
                    StringComparison.OrdinalIgnoreCase))
            {
                randomUpgradeButton = candidate.GetComponent<UnityEngine.UI.Button>();
                randomUpgradeToggle = candidate.GetComponent<UnityEngine.UI.Toggle>();
            }
            else if (candidate.name.Equals(
                    StartButtonName,
                    StringComparison.OrdinalIgnoreCase))
            {
                startButton = candidate.GetComponent<UnityEngine.UI.Button>();
            }
            else if (candidate.name.Equals(
                    QuitButtonName,
                    StringComparison.OrdinalIgnoreCase))
            {
                quitButton = candidate.GetComponent<UnityEngine.UI.Button>();
            }
        }

        if (mainUI == null || controlsUI == null)
        {
            return;
        }

        mainUI.SetActive(true);
        controlsUI.SetActive(false);

        if (settingsUI != null)
        {
            settingsUI.SetActive(false);
        }

        if (controlsButton != null)
        {
            controlsButton.onClick.AddListener(
                () => ShowCanvas(mainUI, controlsUI));
        }
        else
        {
            Debug.LogWarning(
                $"No Button component named {ControlsButtonName} was found in {scene.name}.");
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(
                () => ShowCanvas(controlsUI, mainUI));
        }
        else
        {
            Debug.LogWarning(
                $"No Button component named {BackButtonName} was found in {scene.name}.");
        }

        if (settingsUI != null && settingsButton != null)
        {
            settingsButton.onClick.AddListener(
                () => ShowCanvas(mainUI, settingsUI));
        }
        else
        {
            Debug.LogWarning(
                $"{SettingsMenuName} or {SettingsButtonName} was not found in {scene.name}.");
        }

        if (settingsUI != null && backSettingsButton != null)
        {
            backSettingsButton.onClick.AddListener(
                () => ShowCanvas(settingsUI, mainUI));
        }
        else
        {
            Debug.LogWarning(
                $"{SettingsMenuName} or {BackSettingsButtonName} was not found in {scene.name}.");
        }

        ConfigureRandomUpgradeToggle(
            randomUpgradeButton,
            randomUpgradeToggle,
            scene);

        if (startButton != null)
        {
            startButton.onClick.AddListener(
                () => SceneManager.LoadScene(ArenaSceneName));
        }
        else
        {
            Debug.LogWarning(
                $"No Button component named {StartButtonName} was found in {scene.name}.");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogWarning(
                $"No Button component named {QuitButtonName} was found in {scene.name}.");
        }
    }

    private static void QuitGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CloseGameTab();
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void ConfigureRandomUpgradeToggle(
        UnityEngine.UI.Button button,
        UnityEngine.UI.Toggle toggle,
        Scene scene)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(
                GameManager.RandomStartingUpgradesEnabled);
            toggle.onValueChanged.AddListener(
                GameManager.SetRandomStartingUpgradesEnabled);
            return;
        }

        if (button != null)
        {
            button.onClick.AddListener(
                GameManager.ToggleRandomStartingUpgrades);
            return;
        }

        Debug.LogWarning(
            $"No Button or Toggle component named {RandomUpgradeToggleName} was found in {scene.name}.");
    }

    private static void ShowCanvas(GameObject canvasToHide, GameObject canvasToShow)
    {
        canvasToHide.SetActive(false);
        canvasToShow.SetActive(true);
    }
}
