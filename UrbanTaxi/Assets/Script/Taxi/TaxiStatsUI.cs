using UnityEngine;
using TMPro;

public class TaxiStatsUI : MonoBehaviour
{
    public TaxiAgent agent;

    public TextMeshProUGUI episodeText;
    public TextMeshProUGUI rewardText;

    void Update()
    {
        if (agent == null) return;

        // Episodio (mantiene la scritta "Episodio:")
        episodeText.text = $"Episodio: {agent.episodeCount}";

        // Reward con 2 decimali
        float r = agent.episodeReward;
        rewardText.text = $"Reward: {r:F2}";

        // Colore del testo in base al valore
        if (r < 0f)
            rewardText.color = Color.red;
        else
            rewardText.color = Color.green;
    }
}
