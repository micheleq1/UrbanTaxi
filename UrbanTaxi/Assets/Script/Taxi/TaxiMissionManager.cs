using UnityEngine;
using WrightAngle.Waypoint;
using System.Collections;

public class TaxiMissionManager : MonoBehaviour
{
    
    public TaxiController taxi;

    
    public GameObject passengerPrefab;
    public GameObject destinationPrefab;

    
    public Transform[] passengerSpawnPoints;
    public Transform[] destinationPoints;

    
    public float pickupDistance = 2.5f;
    public float dropoffDistance = 3f;

    
    public float stopTimeAtDestination = 5f;

    private GameObject passengerObj;
    private GameObject destinationObj;

    private WaypointTarget passengerWaypoint;
    private WaypointTarget destinationWaypoint;

    private enum State { WaitingPickup, GoingToPickup, WaitingDropoff, GoingToDropoff }
    private State state;

    private bool isHandlingTransition = false;

    
    private bool lastReachedGoal = false;
    private State lastState;

    void Awake()
    {

        passengerObj = Instantiate(passengerPrefab);
        passengerObj.SetActive(false);
        passengerWaypoint = passengerObj.GetComponentInChildren<WaypointTarget>(true);

        destinationObj = Instantiate(destinationPrefab);
        destinationObj.SetActive(false);
        destinationWaypoint = destinationObj.GetComponentInChildren<WaypointTarget>(true);

    }

    void Start()
    {
        SpawnPassenger();
        lastState = state;
        
    }


    IEnumerator SetTimeScaleDelayed()
    {
        yield return null; // aspetta 1 frame
        Time.timeScale = 10f;
    }

    void Update()
    {
        if (taxi == null) return;

        bool reached = taxi.HasReachedGoal();

        
        if (reached != lastReachedGoal)
        {
            lastReachedGoal = reached;
        }

        if (state != lastState)
        {
            lastState = state;
        }

        if (isHandlingTransition) return;
        if (!reached) return;

        if (state == State.GoingToPickup)
        {
            StartCoroutine(HandlePickup());
        }
        else if (state == State.GoingToDropoff)
        {
            StartCoroutine(HandleDropoff());
        }
    }

    IEnumerator HandlePickup()
    {
        isHandlingTransition = true;
        state = State.WaitingPickup;

        if (passengerWaypoint != null) passengerWaypoint.DeactivateWaypoint();
        passengerObj.SetActive(false);

        yield return null; 

        SpawnDestination();

        isHandlingTransition = false;
    }

    void SpawnDestination()
    {
        if (destinationPoints == null || destinationPoints.Length == 0)
        {
            return;
        }

        Transform dp = destinationPoints[Random.Range(0, destinationPoints.Length)];
        destinationObj.transform.position = dp.position;

        destinationObj.SetActive(true);
        if (destinationWaypoint != null) destinationWaypoint.ActivateWaypoint();

        TaxiRoadNode dropNode = FindClosestNode(destinationObj.transform.position);

        taxi.SetGoalNode(dropNode);

        state = State.GoingToDropoff;
    }

    IEnumerator HandleDropoff()
    {
        isHandlingTransition = true;
        state = State.WaitingDropoff;

        if (destinationWaypoint != null) destinationWaypoint.DeactivateWaypoint();
        destinationObj.SetActive(false);

        yield return null; 

        SpawnPassenger();

        isHandlingTransition = false;
    }

    void SpawnPassenger()
    {
        if (passengerSpawnPoints == null || passengerSpawnPoints.Length == 0)
        {
            return;
        }

        Transform sp = passengerSpawnPoints[Random.Range(0, passengerSpawnPoints.Length)];
        passengerObj.transform.position = sp.position;
        passengerObj.SetActive(true);
        if (passengerWaypoint != null) passengerWaypoint.ActivateWaypoint();

        TaxiRoadNode pickupNode = FindClosestNode(passengerObj.transform.position);

        taxi.SetGoalNode(pickupNode);

        state = State.GoingToPickup;
    }

    TaxiRoadNode FindClosestNode(Vector3 pos)
    {
        TaxiRoadNode[] nodes = FindObjectsOfType<TaxiRoadNode>();

        TaxiRoadNode best = null;
        float bestDist = float.MaxValue;

        foreach (var n in nodes)
        {
            float d = Vector3.Distance(pos, n.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }

        return best;
    }
}
