using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsTextMesh;
    [SerializeField] private Image fuelImage;

    private void Update()
    {
        UpdateStatsTextMesh();
    }
    private void UpdateStatsTextMesh()
    {
        fuelImage.fillAmount = Lander.Instance.GetFuelNormalized();

        statsTextMesh.text =
            GameManager.Instance.GetScore() + "\n" +
            Mathf.Round(GameManager.Instance.GetTime()) + "\n" +
            Mathf.Round(Lander.Instance.GetSpeedX()) * 10f + "\n" +
            Mathf.Round(Lander.Instance.GetSpeedY()) * 10f;
    }
}
