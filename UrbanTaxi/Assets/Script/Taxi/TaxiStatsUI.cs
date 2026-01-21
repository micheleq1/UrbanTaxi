using UnityEngine;
using TMPro;

public class TaxiStatsUI : MonoBehaviour
{
    public TaxiAgent agent;

    
    public TextMeshProUGUI episodeText;
    public TextMeshProUGUI rewardCurrentText;
    public TextMeshProUGUI rewardPrevText;
    public TextMeshProUGUI timeCurrentText;

    
    private int lastSeenEpisode = -1;
    private float episodeStartTime = 0f;

    void Start()
    {
        if (agent != null)
        {
            lastSeenEpisode = agent.episodeCount;
            episodeStartTime = Time.time;
        }
    }

    void Update()
    {
        if (agent == null) return;

        
        if (agent.episodeCount != lastSeenEpisode)
        {
            lastSeenEpisode = agent.episodeCount;
            episodeStartTime = Time.time;
        }

        
        if (episodeText != null)
            episodeText.text = $"Episodio: {agent.episodeCount}";

       
        float currR = agent.episodeReward;
        if (rewardCurrentText != null)
        {
            rewardCurrentText.text = $"Reward (corrente): {currR:F2}";
            rewardCurrentText.color = (currR < 0f) ? Color.red : Color.green;
        }

        
        float prevR = agent.previousEpisodeReward;
        if (rewardPrevText != null)
        {
            rewardPrevText.text = $"Reward (precedente): {prevR:F2}";
            rewardPrevText.color = (prevR < 0f) ? Color.red : Color.green;
        }

        
        float elapsed = Time.time - episodeStartTime;
        if (timeCurrentText != null)
            timeCurrentText.text = $"Tempo (corrente): {elapsed:F2}s";
    }
}
