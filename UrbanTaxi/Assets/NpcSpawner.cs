using UnityEngine;
using WrightAngle.Waypoint; // WrightAngle Waypoint System

public class TaxiMissionManager : MonoBehaviour
{
    [Header("References")]
    public Transform taxi;

    [Header("Prefabs")]
    [Tooltip("NPC passeggero (deve avere WaypointTarget sul prefab)")]
    public GameObject passengerPrefab;

    [Tooltip("Oggetto destinazione (può essere un empty/marker world). Deve avere WaypointTarget sul prefab")]
    public GameObject destinationPrefab;

    [Header("Points (random)")]
    public Transform[] passengerSpawnPoints;
    public Transform[] destinationPoints;

    [Header("Distances")]
    public float pickupDistance = 2.5f;
    public float dropoffDistance = 3.0f;

    [Header("Options")]
    public bool avoidSamePassengerSpawnTwice = true;
    public bool avoidSameDestinationTwice = true;

    private GameObject passengerObj;
    private GameObject destinationObj;

    private WaypointTarget passengerWaypoint;
    private WaypointTarget destinationWaypoint;

    private enum State { GoPickup, GoDropoff }
    private State state;

    private int lastPassengerIndex = -1;
    private int lastDestinationIndex = -1;

    // Target “logico” (utile per RL/Agent)
    public Transform CurrentTarget
    {
        get
        {
            if (state == State.GoPickup && passengerObj != null && passengerObj.activeSelf)
                return passengerObj.transform;

            if (state == State.GoDropoff && destinationObj != null && destinationObj.activeSelf)
                return destinationObj.transform;

            return null;
        }
    }

    void Awake()
    {
        if (passengerPrefab != null)
        {
            passengerObj = Instantiate(passengerPrefab);
            passengerObj.SetActive(false);

            // PRIMA: GetComponent<WaypointTarget>()
            // ORA:
            passengerWaypoint = passengerObj.GetComponentInChildren<WaypointTarget>(true);
            if (passengerWaypoint == null)
                Debug.LogError("[TaxiMissionManager] Nessun WaypointTarget trovato nel passenger (root o children).");
        }

        if (destinationPrefab != null)
        {
            destinationObj = Instantiate(destinationPrefab);
            destinationObj.SetActive(false);

            destinationWaypoint = destinationObj.GetComponentInChildren<WaypointTarget>(true);
        }
    }

    void Start()
    {
        SpawnPassengerRandom();
    }

    void Update()
    {
        if (taxi == null) return;

        if (state == State.GoPickup && passengerObj != null && passengerObj.activeSelf)
        {
            if (Vector3.Distance(taxi.position, passengerObj.transform.position) <= pickupDistance)
            {
                // PICKUP
                DeactivatePassengerWaypoint();
                passengerObj.SetActive(false);

                SpawnDestinationRandom();
            }
        }
        else if (state == State.GoDropoff && destinationObj != null && destinationObj.activeSelf)
        {
            if (Vector3.Distance(taxi.position, destinationObj.transform.position) <= dropoffDistance)
            {
                // DROPOFF
                DeactivateDestinationWaypoint();
                destinationObj.SetActive(false);

                SpawnPassengerRandom();
            }
        }
    }

    void SpawnPassengerRandom()
    {
        if (passengerObj == null || passengerSpawnPoints == null || passengerSpawnPoints.Length == 0)
        {
            Debug.LogError("[TaxiMissionManager] passengerPrefab o passengerSpawnPoints non assegnati.");
            return;
        }

        // Quando riparti col passenger, spegni la destination
        if (destinationObj != null)
        {
            DeactivateDestinationWaypoint();
            destinationObj.SetActive(false);
        }

        int i = PickRandomIndex(passengerSpawnPoints.Length, lastPassengerIndex, avoidSamePassengerSpawnTwice);
        lastPassengerIndex = i;

        Transform sp = passengerSpawnPoints[i];
        passengerObj.transform.SetPositionAndRotation(sp.position, sp.rotation);
        passengerObj.SetActive(true);

        ActivatePassengerWaypoint();

        state = State.GoPickup;
    }

    void SpawnDestinationRandom()
    {
        if (destinationObj == null || destinationPoints == null || destinationPoints.Length == 0)
        {
            Debug.LogError("[TaxiMissionManager] destinationPrefab o destinationPoints non assegnati.");
            return;
        }

        int i = PickRandomIndex(destinationPoints.Length, lastDestinationIndex, avoidSameDestinationTwice);
        lastDestinationIndex = i;

        Transform dp = destinationPoints[i];
        destinationObj.transform.SetPositionAndRotation(dp.position, dp.rotation);
        destinationObj.SetActive(true);

        ActivateDestinationWaypoint();

        state = State.GoDropoff;
    }

    int PickRandomIndex(int length, int lastIndex, bool avoidSameTwice)
    {
        if (length <= 1 || !avoidSameTwice) return Random.Range(0, length);

        int idx = Random.Range(0, length);
        while (idx == lastIndex)
            idx = Random.Range(0, length);

        return idx;
    }

    void ActivatePassengerWaypoint()
    {
        if (passengerWaypoint != null) passengerWaypoint.ActivateWaypoint();
    }

    void DeactivatePassengerWaypoint()
    {
        if (passengerWaypoint != null) passengerWaypoint.DeactivateWaypoint();
    }

    void ActivateDestinationWaypoint()
    {
        if (destinationWaypoint != null) destinationWaypoint.ActivateWaypoint();
    }

    void DeactivateDestinationWaypoint()
    {
        if (destinationWaypoint != null) destinationWaypoint.DeactivateWaypoint();
    }

    // Chiamalo da OnEpisodeBegin() del tuo agente RL
    public void ResetMission()
    {
        if (passengerObj != null)
        {
            DeactivatePassengerWaypoint();
            passengerObj.SetActive(false);
        }

        if (destinationObj != null)
        {
            DeactivateDestinationWaypoint();
            destinationObj.SetActive(false);
        }

        lastPassengerIndex = -1;
        lastDestinationIndex = -1;

        SpawnPassengerRandom();
    }
}
