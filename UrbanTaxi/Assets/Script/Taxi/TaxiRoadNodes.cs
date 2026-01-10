using System.Collections.Generic;
using UnityEngine;

public enum RoadNodeType
{
    StreetStart,
    StreetMiddle,
    StreetEnd,
    IntersectionCenter,
    IntersectionOut
}



public class TaxiRoadNode : MonoBehaviour
{
    public RoadNodeType nodeType;
    public List<TaxiRoadNode> neighbors = new List<TaxiRoadNode>();
}
