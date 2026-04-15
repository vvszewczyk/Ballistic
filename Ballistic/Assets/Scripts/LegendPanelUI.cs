using UnityEngine;

public class LegendPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject legendPanel;

    private void Start()
    {
        if (legendPanel != null)
            legendPanel.SetActive(false);
    }

    public void ToggleLegend()
    {
        if (legendPanel == null) return;

        legendPanel.SetActive(!legendPanel.activeSelf);
    }

    public void ShowLegend()
    {
        if (legendPanel == null) return;
        legendPanel.SetActive(true);
    }

    public void HideLegend()
    {
        if (legendPanel == null) return;
        legendPanel.SetActive(false);
    }
}