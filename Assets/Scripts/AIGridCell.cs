using UnityEngine;

public class AIGridCell
{
    public string state = "unwalkable";
    public Vector3 position;
    public float gCost = 100000f;
    public float hCost = 100000f;
    public float fCost = 100000f;
    public float eCost = 0f; //Extra Cost
}
