using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class TaxiAgent : Agent
{
    private TaxiController taxi;

    [Header("Progress Reward")]
    private float prevDistToGoal = 0f;
    private const float MaxMapDist = 320f;

    [Header("Episode by Steps")]
    public int maxDecisionSteps = 80;     // prova 80-120, in base alla mappa
    public float timeoutPenalty = -0.5f;  // penalità quando supera max step
    private int decisionSteps = 0;

    [Header("Episode Stats")]
    public float episodeReward = 0f; // reward totale dell'episodio corrente
    public int episodeCount = 0;     // numero episodi avviati

    public override void Initialize()
    {
        taxi = GetComponent<TaxiController>();
    }

    public override void OnEpisodeBegin()
    {
        // aggiorna contatori episodio
        episodeCount++;
        episodeReward = 0f;
        decisionSteps = 0;

        Debug.Log($"Inizio episodio #{episodeCount}");

        if (taxi == null) taxi = GetComponent<TaxiController>();
        if (taxi == null || taxi.currentNode == null || taxi.goalNode == null) return;

        prevDistToGoal = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (taxi.currentNode == null || taxi.goalNode == null)
            return;

        Vector3 toGoal = taxi.goalNode.transform.position - taxi.transform.position;

        // Direzione locale verso goal (x,z) già in [-1,1]
        Vector3 toGoalLocalDir = taxi.transform.InverseTransformDirection(toGoal.normalized);
        sensor.AddObservation(toGoalLocalDir.x);
        sensor.AddObservation(toGoalLocalDir.z);

        // Distanza normalizzata [0,1]
        float distToGoal = toGoal.magnitude;
        sensor.AddObservation(Mathf.Clamp01(distToGoal / MaxMapDist));

        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);

        float currToGoal = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );

        for (int i = 0; i < 4; i++)
        {
            if (i < neighbors.Count)
            {
                TaxiRoadNode n = neighbors[i];

                // direzione locale verso vicino
                Vector3 toN = n.transform.position - taxi.transform.position;
                Vector3 toNLocalDir = taxi.transform.InverseTransformDirection(toN.normalized);

                sensor.AddObservation(toNLocalDir.x);
                sensor.AddObservation(toNLocalDir.z);

                // improvement verso goal
                float nToGoal = Vector3.Distance(n.transform.position, taxi.goalNode.transform.position);
                float improvement = (currToGoal - nToGoal) / MaxMapDist;
                sensor.AddObservation(Mathf.Clamp(improvement, -1f, 1f));
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        decisionSteps++;

        // timeout a step
        if (decisionSteps > maxDecisionSteps)
        {
            Debug.Log("Termine episodio: troppi step");
            AddEpisodeReward(timeoutPenalty);
            EndEpisode();
            return;
        }

        if (taxi == null || taxi.currentNode == null) return;

        int action = actions.DiscreteActions[0]; // 0..3

        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);
        if (neighbors == null || neighbors.Count == 0) return;

        // azione non valida
        if (action < 0 || action >= neighbors.Count)
        {
            AddEpisodeReward(-0.02f);
            action = 0;
        }

        TaxiRoadNode chosen = neighbors[action];

        // evita oscillazione: tornare subito al nodo precedente
        if (chosen == taxi.PreviousNode)
        {
            AddEpisodeReward(-0.05f);

            for (int i = 0; i < neighbors.Count; i++)
            {
                if (neighbors[i] != taxi.PreviousNode)
                {
                    chosen = neighbors[i];
                    break;
                }
            }
        }

        taxi.SetTargetNode(chosen);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = Random.Range(0, 4);
    }

    private List<TaxiRoadNode> GetSortedNeighbors(TaxiRoadNode node)
    {
        var list = new List<TaxiRoadNode>(node.neighbors);

        list.Sort((a, b) =>
        {
            Vector3 dirA = (a.transform.position - node.transform.position).normalized;
            Vector3 dirB = (b.transform.position - node.transform.position).normalized;

            float angA = Vector3.SignedAngle(taxi.transform.forward, dirA, Vector3.up);
            float angB = Vector3.SignedAngle(taxi.transform.forward, dirB, Vector3.up);

            return angA.CompareTo(angB); // sinistra -> destra
        });

        return list;
    }

    // usa sempre questo metodo al posto di AddReward()
    private void AddEpisodeReward(float value)
    {
        AddReward(value);
        episodeReward += value;
    }

    public void OnReachedGoal()
    {
        AddEpisodeReward(10f);
        EndEpisode();
        Debug.Log("Goal raggiunto! Reward +10");
    }

    public void OnNodeReached()
    {
        if (taxi == null || taxi.currentNode == null || taxi.goalNode == null) return;

        float newDist = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );

        float delta = prevDistToGoal - newDist; // >0 se ti avvicini

        float shaped = Mathf.Clamp(delta / MaxMapDist, -1f, 1f);
        AddEpisodeReward(0.2f * shaped);

        prevDistToGoal = newDist;
    }
}
