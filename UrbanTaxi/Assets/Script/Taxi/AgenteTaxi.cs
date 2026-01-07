using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class TaxiAgent : Agent
{
    private TaxiController taxi;

    public override void Initialize()
    {
        taxi = GetComponent<TaxiController>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (taxi.currentNode == null || taxi.goalNode == null)
            return;

        sensor.AddObservation(Vector3.Distance(
            taxi.currentNode.transform.position,
            taxi.goalNode.transform.position
        ) / 50f);

        List<TaxiRoadNode> neighbors = taxi.currentNode.neighbors;

        for (int i = 0; i < 4; i++)
        {
            if (i < neighbors.Count)
            {
                float d = Vector3.Distance(
                    neighbors[i].transform.position,
                    taxi.goalNode.transform.position
                );
                sensor.AddObservation(d / 50f);
            }
            else
            {
                sensor.AddObservation(1f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (taxi.currentNode == null) return;

        int choice = actions.DiscreteActions[0];
        List<TaxiRoadNode> neighbors = taxi.currentNode.neighbors;

        if (choice < 0 || choice >= neighbors.Count)
            return;

        TaxiRoadNode chosen = neighbors[choice];

        if (chosen == taxi.PreviousNode)
            return;

        taxi.SetTargetNode(chosen);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = Random.Range(0, 4);
    }

}
