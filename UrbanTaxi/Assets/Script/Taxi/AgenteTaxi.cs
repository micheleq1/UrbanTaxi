using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class TaxiAgent : Agent
{
    private TaxiController taxi;
    public float maxEpisodeTime = 300f;
    private float episodeTimer = 0f;
    [Header("Progress Reward")]
    
    private float prevDistToGoal = 0f;
    private const float MaxMapDist = 320f;

    [Header("Episode by Steps")]
    public int maxDecisionSteps = 80;    // prova 80-120, in base alla mappa
    public float timeoutPenalty = -0.5f; // penalità quando supera max step

    private int decisionSteps = 0;
    public override void Initialize()
    {
        taxi = GetComponent<TaxiController>();
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Inizio episodio");
        //episodeTimer = 0f;
        decisionSteps = 0;

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

        // Direzione locale (solo direzione, già in [-1,1])
        Vector3 toGoalLocalDir = taxi.transform.InverseTransformDirection(toGoal.normalized);
        sensor.AddObservation(toGoalLocalDir.x);
        sensor.AddObservation(toGoalLocalDir.z);
        
        // Distanza normalizzata in [0,1] 
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

                // 1) DOVE PORTA: direzione locale verso il vicino (x,z) in [-1,1]
                Vector3 toN = n.transform.position - taxi.transform.position;
                Vector3 toNLocalDir = taxi.transform.InverseTransformDirection(toN.normalized);

                sensor.AddObservation(toNLocalDir.x);
                sensor.AddObservation(toNLocalDir.z);

                // 2) AVVICINA AL GOAL: improvement in [-1,1]
                float nToGoal = Vector3.Distance(n.transform.position, taxi.goalNode.transform.position);
                float improvement = (currToGoal - nToGoal) / MaxMapDist; // positivo = bene
                sensor.AddObservation(Mathf.Clamp(improvement, -1f, 1f));
            }
            else
            {
                // Padding: (dirX, dirZ, improvement)
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }

        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        decisionSteps++;

        // 2) timeout a step
        if (decisionSteps > maxDecisionSteps)
        {
            Debug.Log("Termine episodio, troppi step");
            AddReward(timeoutPenalty);
            EndEpisode();
            return;
        }
        if (taxi == null || taxi.currentNode == null) return;

        int action = actions.DiscreteActions[0]; // 0..3
        

        // stessi neighbors ordinati usati in CollectObservations
        List<TaxiRoadNode> neighbors = GetSortedNeighbors(taxi.currentNode);
        if (neighbors == null || neighbors.Count == 0) return;

        // se azione punta a un vicino che non esiste -> fallback (oppure penalità)
        if (action < 0 || action >= neighbors.Count)
        {
            // penalizza leggermente azione non valida
            AddReward(-0.02f);
            action = 0;
        }

        TaxiRoadNode chosen = neighbors[action];

        // evita oscillazione: tornare subito al nodo precedente
        if (chosen == taxi.PreviousNode)
        {
            AddReward(-0.05f);
            // fallback: scegli un altro vicino se esiste
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
   /* private void FixedUpdate()
    {
        // timeout
        episodeTimer += Time.fixedDeltaTime;
        if (episodeTimer >= maxEpisodeTime)
        {
            
            AddReward(-0.5f);   // penalità timeout (regola: piccola ma chiara)
            EndEpisode();
        }

        // (facoltativo) penalità per il tempo che passa
        AddReward(-0.001f);
    }*/


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

    public void OnReachedGoal()
    {
        
        AddReward(10f);
        EndEpisode();
        Debug.Log("Ricompensa +5");
    }

    public void OnNodeReached()
    {
        if (taxi == null || taxi.currentNode == null || taxi.goalNode == null) return;

        float newDist = Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        );

        float delta = prevDistToGoal - newDist; // >0 se ti avvicini

        // reward proporzionale al progresso (normalizzato)
        float shaped = Mathf.Clamp(delta / MaxMapDist, -1f, 1f);
        AddReward(0.2f * shaped);

        prevDistToGoal = newDist;
    }

    


}
