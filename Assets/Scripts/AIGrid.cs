using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class AIGrid : MonoBehaviour
{

    public static AIGrid instance;
    public bool disableMove = false;

    public Vector3 checkDistance = new Vector3(60f, 60f, 60f);
    public Vector3 cellSize = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 scaledCheckDistance;
    public Vector3 scaledCellSize;


    public List<LayerMask> layers = new List<LayerMask>();
    public List<LayerMask> alwaysInvalidLayers = new List<LayerMask>();
    public List<ExtraCosts> extraCosts = new List<ExtraCosts>();
    public List<string> extraCostsNames = new List<string>();

    public AIGridCell[,,] grid = null;
    public List<AIGridCell> walkableGrid = new List<AIGridCell>();
    public List<AIGridCell> stairsGrid = new List<AIGridCell>();

    // Universal Gizmos showing toggles in the inspector

    [Tooltip("Required to be on for any Gizmos colour coding to show")]
    public bool showGizmos = false;

    [Tooltip("Red")]
    public bool showGizmosUnwalkable = true;
    [Tooltip("Yellow")]
    public bool showGizmosStairs = true;
    [Tooltip("Green")]
    public bool showGizmosWalkable = true;
    [Tooltip("Purple")]
    public bool showGizmosAir = false;
    [Tooltip("Orange")]
    public bool showGizmosCalculatedPath = false;
    [Tooltip("Grey")]
    public bool showGizmosJump = false;
    [Tooltip("Black")]
    public bool showGizmosPathTo = false;
    [Tooltip("Cyan")]
    public bool showGizmosGoal = false;
    [Tooltip("Blue")]
    public bool showGizmosError = false;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // List and array positions need to be ints not floats, so this forces them to be floats via rounding up to ensure they fit into the logic smoothly
        scaledCheckDistance = new Vector3(Mathf.CeilToInt(checkDistance.x), Mathf.CeilToInt(checkDistance.y), Mathf.CeilToInt(checkDistance.z));
        scaledCellSize = new Vector3(Mathf.CeilToInt(cellSize.x), Mathf.CeilToInt(cellSize.y), Mathf.CeilToInt(cellSize.z));
        for (int i = 0; i < extraCosts.Count; i++)
        {
            extraCostsNames.Add(extraCosts[i].name);
        }

        GenerateGrid();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;   // Allows the singleton to be reset via destruction other than scene loading
    }


    void GenerateGrid()
    {
        if (grid == null) grid = new AIGridCell[Mathf.CeilToInt(checkDistance.x), Mathf.CeilToInt(checkDistance.y), Mathf.CeilToInt(checkDistance.z)];
        walkableGrid.Clear();
        stairsGrid.Clear();
        
        // Order of operations for loops matter. y needs to be the highest so it does the logic for all whole floor before moving onto the next one

        for (int y = 0; y < scaledCheckDistance.y; y += Mathf.CeilToInt(cellSize.y))
        {
            for (int x = 0; x < scaledCheckDistance.x; x+=Mathf.CeilToInt(cellSize.x))
            {
                for (int z = 0; z < scaledCheckDistance.z; z += Mathf.CeilToInt(cellSize.z))
                {


                    foreach (LayerMask layer in alwaysInvalidLayers) // Check invalid layers first
                    {

                        grid[x, y, z] = new AIGridCell();



                        if (Physics.CheckBox(new Vector3(x, y, z), scaledCellSize, Quaternion.identity, layer))
                        {
                            // Sets cell at hit location as unwalkable
                            grid[x, y, z] = new AIGridCell();
                            grid[x, y, z].state = "unwalkable";
                            grid[x, y, z].position = new Vector3(x, y, z);

                            // Sets cells above at hit location as unwalkable
                            grid[x, y + (int)scaledCellSize.y, z] = new AIGridCell();
                            grid[x, y + (int)scaledCellSize.y, z].state = "unwalkable";
                            grid[x, y + (int)scaledCellSize.y, z].position = new Vector3(x, y + (scaledCellSize.y), z);
                            grid[x, y + ((int)scaledCellSize.y * 2), z] = new AIGridCell();
                            grid[x, y + ((int)scaledCellSize.y * 2), z].state = "unwalkable";
                            grid[x, y + ((int)scaledCellSize.y * 2), z].position = new Vector3(x, y + (scaledCellSize.y * 2), z);

                            // Sets cells below at hit location as unwalkable

                            if (y - (int)scaledCellSize.y > 0)
                            {
                                grid[x, y - (int)scaledCellSize.y, z] = new AIGridCell();
                                grid[x, y - (int)scaledCellSize.y, z].state = "unwalkable";
                                grid[x, y - (int)scaledCellSize.y, z].position = new Vector3(x, y + (scaledCellSize.y), z);
                            }
                        }
                        else grid[x, y, z] = null; // Sets the cell as null so it can be processed later


                    }



                    if (grid[x, y, z] == null) // Doesn't run if the cell has already been marked as something
                    {
                        foreach (LayerMask layer in layers)
                        {

                            grid[x, y, z] = new AIGridCell();
                            if (!Physics.CheckBox(new Vector3(x, y, z), scaledCellSize, Quaternion.identity, layer)) // Sets the cell as air if nothing is present in it
                            {
                                grid[x, y, z].state = "air";
                                grid[x, y, z].position = new Vector3(x, y, z);
                            }
                            else if (!Physics.CheckBox(new Vector3(x, y + scaledCellSize.y, z), scaledCellSize, Quaternion.identity, layer)) // Sets the cell as walkable if there is nothing in the above cell
                            {

                                grid[x, y, z].state = "walkable";
                                grid[x, y, z].position = new Vector3(x, y, z);
                                walkableGrid.Add(grid[x, y, z]);
                                RaycastHit hit;
                                bool hitDetction = Physics.BoxCast(new Vector3(x, y, z), scaledCellSize, Vector3.zero, out hit);
                                if (hitDetction)
                                {
                                    if (extraCostsNames.Contains(hit.transform.tag)) grid[x, y, z].eCost = extraCosts[extraCostsNames.IndexOf(hit.transform.tag)].cost;
                                }
                            }
                            else if (!Physics.CheckBox(new Vector3(x, y + (scaledCellSize.y * 2), z), scaledCellSize, Quaternion.identity, layer)) // Sets the cell as stairs if there is something in the cell above, but not in the one on top of it
                            {
                                grid[x, y, z].state = "stairs";
                                grid[x, y, z].position = new Vector3(x, y, z);
                                stairsGrid.Add(grid[x, y, z]);
                                RaycastHit hit;
                                bool hitDetction = Physics.BoxCast(new Vector3(x, y + scaledCellSize.y, z), scaledCellSize, Vector3.zero, out hit);
                                if (hitDetction)
                                {
                                    if (extraCostsNames.Contains(hit.transform.tag)) grid[x, y, z].eCost = extraCosts[extraCostsNames.IndexOf(hit.transform.tag)].cost;
                                }
                            }
                            

                            else // Runs if there is things detected in the two cells above, setting the cell as unwalkable
                            {
                                grid[x, y, z].state = "unwalkable";
                                grid[x, y, z].position = new Vector3(x, y, z);
                            }
                        }

                    }

                    


                }
            }
        }
        //Debug.Log(grid.Length);

    }

    private void OnDrawGizmos()
    {
        // Cell type visualisation through Gizmos

        if (showGizmos)
        {
            if (grid != null)
            {
                for (int x = 0; x < scaledCheckDistance.x; x += Mathf.CeilToInt(cellSize.x))
                {
                    for (int y = 0; y < scaledCheckDistance.y; y += Mathf.CeilToInt(cellSize.y))
                    {
                        for (int z = 0; z < scaledCheckDistance.z; z += Mathf.CeilToInt(cellSize.z))
                        {
                            switch (grid[x, y, z].state)
                            {
                                case "walkable":
                                    if (showGizmosWalkable)
                                    {
                                        Gizmos.color = Color.green;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                case "stairs":
                                    if (showGizmosStairs)
                                    {
                                        Gizmos.color = Color.yellow;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                case "unwalkable":
                                    if (showGizmosUnwalkable)
                                    {
                                        Gizmos.color = Color.red;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                case "air":
                                    if (showGizmosAir)
                                    {
                                        Gizmos.color = Color.magenta;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                default: //For showing errors
                                    if (showGizmosError)
                                    {
                                        Gizmos.color = Color.blue;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

        }
    }
}

[Serializable]
public class ExtraCosts
{
    public string name;
    public float cost = 0f;
}
