using System;
using System.Collections.Generic;
using UnityEngine;

public class AIPathFindingCore : MonoBehaviour
{


    public static CalculatedPathData CalculatePathCore(Vector3 pathTo, Vector3 startPos, int pathResetCounter, List<Vector3> blockedPositions = null)
    {
        // Calculated as a Coroutine to prevent lag spikes via splitting it across ticks

        if (blockedPositions == null) blockedPositions = new List<Vector3>();

        CalculatedPathData calculatedPathData = new CalculatedPathData();



        calculatedPathData.pathCells.Clear();
        if (pathTo == Vector3.zero)
        {
            calculatedPathData.failure = true;
            return calculatedPathData;
        }

        List<AIGridCell> openCells = new List<AIGridCell>();
        List<AIGridCell> closedCells = new List<AIGridCell>();

        // Cells are required to be ints, so the position is scaled down to them
        Vector3 characterCellPos = new Vector3(Mathf.FloorToInt(startPos.x), Mathf.FloorToInt(startPos.y), Mathf.FloorToInt(startPos.z));
        int checkingX = (int)(characterCellPos.x - AIGrid.instance.gameObject.transform.position.x);
        int checkingY = (int)(characterCellPos.y - AIGrid.instance.gameObject.transform.position.y);
        int checkingZ = (int)(characterCellPos.z - AIGrid.instance.gameObject.transform.position.z);

        AIGridCell currentCell = null;
        AIGridCell startCell = null;

        try
        {
            currentCell = AIGrid.instance.grid[checkingX, checkingY, checkingZ];
            currentCell.gCost = 0f;
            currentCell.hCost = 0f;
            startCell = currentCell;
            openCells.Add(currentCell);

        }
        catch (Exception ex) // Indicates a failure with fetching the grid, so halts to go ahead
        {
            Debug.LogError(ex);
            calculatedPathData.failure = true;
            return calculatedPathData;
        }
        bool localPathCalculated = false;
        while (!localPathCalculated)
        {
            //Debug.Log("Start While");
            AIGridCell shortest = null;
            checkingX = Mathf.FloorToInt(currentCell.position.x - AIGrid.instance.gameObject.transform.position.x);
            checkingZ = Mathf.FloorToInt(currentCell.position.z - AIGrid.instance.gameObject.transform.position.z);
            int yloop = 1;
            //int k = 0;
            int valuesPassed = 0;

            if (pathTo.y < checkingY) yloop = 3; // Adds extra loops on the y level when going down

            // A-Star Algorithm

            for (int k = 0; k < yloop; k++)
            {
                for (int j = -1; j < 2; j++)
                {
                    for (int i = -1; i < 2; i++)
                    {
                        valuesPassed++;
                        try
                        {
                            AIGridCell checkingCell = AIGrid.instance.grid[checkingX + i, checkingY - k, checkingZ + j]; // Gets cells around the character

                            if (!openCells.Contains(checkingCell) && checkingCell != currentCell)
                            {

                                if (!closedCells.Contains(checkingCell) && ((checkingCell.state == AIGrid.GridStates.walkable || checkingCell.state == AIGrid.GridStates.stairs) && (AIGrid.instance.grid[checkingX + i, checkingY - (int)AIGrid.instance.scaledCellSize.y, checkingZ + j] == null || AIGrid.instance.grid[checkingX + i, checkingY - 1, checkingZ + j].state != AIGrid.GridStates.air)) && !(NewUnity.ContainsV3(blockedPositions, checkingCell.position)))
                                {
                                    checkingCell.hCost = Vector3.Distance(checkingCell.position, pathTo);
                                    if (k == 0) checkingCell.gCost = currentCell.gCost + Vector3.Distance(currentCell.position, checkingCell.position);
                                    else checkingCell.gCost = currentCell.gCost + Vector3.Distance(AIGrid.instance.grid[Mathf.FloorToInt(currentCell.position.x - AIGrid.instance.gameObject.transform.position.x), Mathf.FloorToInt(currentCell.position.y - (k * (int)AIGrid.instance.scaledCellSize.y) -  AIGrid.instance.gameObject.transform.position.y), Mathf.FloorToInt(currentCell.position.z - AIGrid.instance.gameObject.transform.position.z)].position, checkingCell.position);
                                    checkingCell.fCost = checkingCell.gCost + checkingCell.hCost + checkingCell.eCost;
                                    if (checkingCell.position == pathTo)
                                    {
                                        localPathCalculated = true;
                                        i = 3;
                                        j = 3;
                                    }
                                    if (shortest == null) shortest = checkingCell;
                                    else if (shortest.fCost > checkingCell.fCost)
                                    {
                                        shortest = checkingCell;
                                    }
                                }
                                else
                                {
                                    checkingCell.gCost = float.MaxValue;
                                    checkingCell.hCost = float.MaxValue;
                                    checkingCell.fCost = float.MaxValue;
                                    if (!closedCells.Contains(checkingCell)) closedCells.Add(checkingCell);
                                }
                            }
                        }
                        catch (Exception ex) { Debug.LogError(ex); } // If error occurs, continue on like nothing happened. No need for extra check failsafe as if one isn't used, others are.

                    }

                }

            }
            try
            {
                // Reorders the cells in the lists for calculations
                //Debug.Log(shortest);
                if (shortest != null)
                {
                    currentCell = shortest;
                    openCells.Add(currentCell);
                    calculatedPathData.pathCells.Add(currentCell);
                    calculatedPathData.cost = shortest.fCost;
                    calculatedPathData.pathCellPositions.Add(currentCell.position);
                    //Debug.Log("Shortest Run");
                }
                else if (openCells.Count > 1)
                {
                    closedCells.Add(openCells[openCells.Count - 1]);
                    openCells.RemoveAt(openCells.Count - 1);
                    currentCell = openCells[openCells.Count - 1];
                    //Debug.Log("Open Run");
                }

            }
            catch // Indicates a major failure with the calculations, so halts to go ahead
            {
                calculatedPathData.failure = true;
                return calculatedPathData;
            }


            // Ends when back to the starting cell
            if (currentCell == startCell)
            {
                break;
            }
        }

        if (!localPathCalculated) // If the flag for a calculated path is not met, tries to calculate again and increments the counter for how much it has tried.
        {
            pathResetCounter++;
            calculatedPathData.pathFound = false;
        }

        calculatedPathData.pathResetCounter = pathResetCounter;
        return calculatedPathData;
    }




}

[Serializable]
public class CalculatedPathData
{
    public List<AIGridCell> pathCells = new List<AIGridCell>();
    public List<Vector3> pathCellPositions = new List<Vector3>();
    public Vector3 goal;
    public bool failure = false;
    public int pathResetCounter;
    public float cost = float.MaxValue;
    public bool pathFound = true;
}



[Serializable]
public class ListBuffer
{
    public List<AIGridCell> aiGridCellB;
    public List<Vector3> vector3B;
    public List<int> intB;
    public List<float> floatB;
    public List<string> stringB;
    public List<Vector2> vector2B;
    //public List<ListBuffer> listBufferB; // Nesting in itself causes an overflow, preventing it from showing in the inspector
}