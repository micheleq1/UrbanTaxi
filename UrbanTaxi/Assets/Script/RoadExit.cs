using UnityEngine;

public class RoadExit
{
    public TaxiRoadNode exitNode;
    public TaxiRoadNode startRoadNode;
    public TaxiRoadNode endRoadNode;

    private TaxiController taxi;

    public RoadExit(
        TaxiRoadNode exit,
        TaxiRoadNode start,
        TaxiRoadNode end,
        TaxiController controller)
    {
        exitNode = exit;
        startRoadNode = start;
        endRoadNode = end;
        taxi = controller;
    }

    // Distanza dal goal (stima geometrica)
    public float DistanzaDallGoal
    {
        get
        {
            if (taxi.goalNode == null) return 0f;

            return Vector3.Distance(
                endRoadNode.transform.position,
                taxi.goalNode.transform.position
            );
        }
    }

    // Torna indietro?
    public bool TornaIndietro
    {
        get
        {
            return taxi.PreviousNode == endRoadNode;
        }
    }

    // Lunghezza della strada reale
    public float LunghezzaStrada
    {
        get
        {
            return Vector3.Distance(
                startRoadNode.transform.position,
                endRoadNode.transform.position
            );
        }
    }

    // Rallentamenti percepiti 
    public float Rallentamenti
    {
        get
        {
            return taxi.TempoFermo;
        }
    }
}
