using UnityEngine;

public class AIGridCell
{
    public AIGrid.GridStates state = AIGrid.GridStates.unwalkable;
    public Vector3 position;
    public float gCost = float.MaxValue;
    public float hCost = float.MaxValue;
    public float fCost = float.MaxValue;
    public float eCost = 0f; //Extra Cost
}
