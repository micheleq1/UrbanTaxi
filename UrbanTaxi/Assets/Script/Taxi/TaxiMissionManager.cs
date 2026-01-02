using UnityEngine;
using WrightAngle.Waypoint;
using System.Collections;

public class TaxiMissionManager : MonoBehaviour
{
    [Header("Taxi")]
    public TaxiController taxi;

    [Header("Prefabs")]
    public GameObject passengerPrefab;
    public GameObject destinationPrefab;

    [Header("Spawn Points")]
    public Transform[] passengerSpawnPoints;
    public Transform[] destinationPoints;

    [Header("Distances")]
    public float pickupDistance = 2.5f;
    public float dropoffDistance = 3f;

    [Header("Stop Settings")]
    public float stopTimeAtDestination = 5f;

    private GameObject passengerObj;
    private GameObject destinationObj;

    private WaypointTarget passengerWaypoint;
    private WaypointTarget destinationWaypoint;

    private enum State { WaitingPickup, GoingToPickup, WaitingDropoff, GoingToDropoff }
    private State state;

    private bool isHandlingTransition = false;

    // anti-spam: log solo quando cambia
    private bool lastReachedGoal = false;
    private State lastState;

    void Awake()
    {
        Debug.Log("[Mission] Awake");

        passengerObj = Instantiate(passengerPrefab);
        passengerObj.SetActive(false);
        passengerWaypoint = passengerObj.GetComponentInChildren<WaypointTarget>(true);

        destinationObj = Instantiate(destinationPrefab);
        destinationObj.SetActive(false);
        destinationWaypoint = destinationObj.GetComponentInChildren<WaypointTarget>(true);

        Debug.Log($"[Mission] passengerWaypoint={(passengerWaypoint != null)} destinationWaypoint={(destinationWaypoint != null)}");
    }

    void Start()
    {
        Debug.Log("[Mission] Start -> SpawnPassenger()");
        SpawnPassenger();
        lastState = state;
    }

    void Update()
    {
        if (taxi == null) return;

        bool reached = taxi.HasReachedGoal();

        // log solo al cambio (così non spamma)
        if (reached != lastReachedGoal)
        {
            Debug.Log($"[Mission] HasReachedGoal changed -> {reached} | state={state}");
            lastReachedGoal = reached;
        }

        if (state != lastState)
        {
            Debug.Log($"[Mission] State changed -> {lastState} => {state}");
            lastState = state;
        }

        if (isHandlingTransition) return;
        if (!reached) return;

        if (state == State.GoingToPickup)
        {
            Debug.Log("[Mission] Reached goal while GoingToPickup -> HandlePickup()");
            StartCoroutine(HandlePickup());
        }
        else if (state == State.GoingToDropoff)
        {
            Debug.Log("[Mission] Reached goal while GoingToDropoff -> HandleDropoff()");
            StartCoroutine(HandleDropoff());
        }
        else
        {
            Debug.Log($"[Mission] Reached goal but state is {state} (ignored)");
        }
    }

    IEnumerator HandlePickup()
    {
        isHandlingTransition = true;
        state = State.WaitingPickup;

        Debug.Log("[Mission] HandlePickup BEGIN -> passenger OFF");
        if (passengerWaypoint != null) passengerWaypoint.DeactivateWaypoint();
        passengerObj.SetActive(false);

        yield return null; // 1 frame

        Debug.Log("[Mission] HandlePickup -> SpawnDestination()");
        SpawnDestination();

        isHandlingTransition = false;
        Debug.Log("[Mission] HandlePickup END");
    }

    void SpawnDestination()
    {
        if (destinationPoints == null || destinationPoints.Length == 0)
        {
            Debug.LogError("[Mission] destinationPoints empty!");
            return;
        }

        Transform dp = destinationPoints[Random.Range(0, destinationPoints.Length)];
        destinationObj.transform.position = dp.position;

        destinationObj.SetActive(true);
        if (destinationWaypoint != null) destinationWaypoint.ActivateWaypoint();

        TaxiRoadNode dropNode = FindClosestNode(destinationObj.transform.position);
        Debug.Log($"[Mission] Destination spawned at {dp.position} -> closestNode={dropNode?.name}");

        taxi.SetGoalNode(dropNode);

        state = State.GoingToDropoff;
    }

    IEnumerator HandleDropoff()
    {
        isHandlingTransition = true;
        state = State.WaitingDropoff;

        Debug.Log("[Mission] HandleDropoff BEGIN -> destination OFF");
        if (destinationWaypoint != null) destinationWaypoint.DeactivateWaypoint();
        destinationObj.SetActive(false);

        yield return null; // 1 frame

        Debug.Log("[Mission] HandleDropoff -> SpawnPassenger()");
        SpawnPassenger();

        isHandlingTransition = false;
        Debug.Log("[Mission] HandleDropoff END");
    }

    void SpawnPassenger()
    {
        if (passengerSpawnPoints == null || passengerSpawnPoints.Length == 0)
        {
            Debug.LogError("[Mission] passengerSpawnPoints empty!");
            return;
        }

        Transform sp = passengerSpawnPoints[Random.Range(0, passengerSpawnPoints.Length)];
        passengerObj.transform.position = sp.position;
        passengerObj.SetActive(true);
        if (passengerWaypoint != null) passengerWaypoint.ActivateWaypoint();

        TaxiRoadNode pickupNode = FindClosestNode(passengerObj.transform.position);
        Debug.Log($"[Mission] Passenger spawned at {sp.position} -> closestNode={pickupNode?.name}");

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
