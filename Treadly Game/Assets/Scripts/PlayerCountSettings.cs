using UnityEngine;

public sealed class PlayerCountSettings : MonoBehaviour
{
    private void Start()
    {
        UnityEngine.UI.Slider roundsSlider =
            GetComponentInChildren<UnityEngine.UI.Slider>(true);

        if (roundsSlider != null)
        {
            roundsSlider.SetValueWithoutNotify(GameManager.NumberOfRounds);
        }
    }

    public void SelectTwoPlayers()
    {
        GameManager.SetActivePlayerCount(2);
    }

    public void SelectThreePlayers()
    {
        GameManager.SetActivePlayerCount(3);
    }

    public void SelectFourPlayers()
    {
        GameManager.SetActivePlayerCount(4);
    }

    public void SetNumberOfRounds(float rounds)
    {
        GameManager.SetNumberOfRounds(Mathf.RoundToInt(rounds));
    }
}
