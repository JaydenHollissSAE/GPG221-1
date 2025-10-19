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
    public bool showVisualisations = false;
    private bool oldShowVisualisations = false;

    [Tooltip("Red")]
    public bool showVisualisationUnwalkable = true;
    private bool oldShowVisualisationUnwalkable = true;
    [Tooltip("Yellow")]
    public bool showVisualisationStairs = true;
    private bool oldShowVisualisationStairs = false;
    [Tooltip("Green")]
    public bool showVisualisationWalkable = true;
    private bool oldShowVisualisationWalkable = false;
    [Tooltip("Purple")]
    public bool showVisualisationAir = false;
    private bool oldShowVisualisationAir = false;
    [Tooltip("Orange")]
    public bool showVisualisationCalculatedPath = false;
    private bool oldShowVisualisationPath = false;
    [Tooltip("Grey")]
    public bool showVisualisationJump = false;
    private bool oldShowVisualisationJump = false;
    [Tooltip("Black")]
    public bool showVisualisationPathTo = false;
    private bool oldShowVisualisationPathTo = false;
    [Tooltip("Cyan")]
    public bool showVisualisationGoal = false;
    private bool oldShowVisualisationGoal = false;
    [Tooltip("Blue")]
    public bool showVisualisationError = false;
    private bool oldShowVisualisationError = false;


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


    private void FixedUpdate()
    {
        if (showVisualisations != oldShowVisualisations)
        {
            oldShowVisualisations = showVisualisations;
            VisualisationSetter.instance.updateVisuals = true;
        }
        if (showVisualisationUnwalkable != oldShowVisualisationUnwalkable)
        {
            oldShowVisualisationUnwalkable = showVisualisationUnwalkable;
            VisualisationSetter.instance.updateVisuals = true;
        }

        if (showVisualisationStairs != oldShowVisualisationStairs)
        {
            oldShowVisualisationStairs = showVisualisationStairs;
            VisualisationSetter.instance.updateVisuals = true;
        }

        if (showVisualisationWalkable != oldShowVisualisationWalkable)
        {
            oldShowVisualisationWalkable = showVisualisationWalkable;
            VisualisationSetter.instance.updateVisuals = true;
        }

        if (showVisualisationAir != oldShowVisualisationAir)
        {
            oldShowVisualisationAir = showVisualisationAir;
            VisualisationSetter.instance.updateVisuals = true;
        }

        if (showVisualisationCalculatedPath != oldShowVisualisationPath)
        {
            oldShowVisualisationPath = showVisualisationCalculatedPath;
            VisualisationSetter.instance.updateVisuals = true;
        }

        if (showVisualisationJump != oldShowVisualisationJump)
        {
            oldShowVisualisationJump = showVisualisationJump;
            VisualisationSetter.instance.updateVisuals = true;
        }

        if (showVisualisationPathTo != oldShowVisualisationPathTo)
        {
            oldShowVisualisationPathTo = showVisualisationPathTo;
            VisualisationSetter.instance.updateVisuals = true;
        }

        if (showVisualisationGoal != oldShowVisualisationGoal)
        {
            oldShowVisualisationGoal = showVisualisationGoal;
            VisualisationSetter.instance.updateVisuals = true;
        }
        if (showVisualisationError != oldShowVisualisationError)
        {
            oldShowVisualisationError = showVisualisationError;
            VisualisationSetter.instance.updateVisuals = true;
        }

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

                            //VisualisationSetter.instance.SpawnVisualisation(grid[x, y, z].position, scaledCellSize, "unwalkable"); // Disabled for build optimisation

                            // Sets cells above at hit location as unwalkable
                            grid[x, y + (int)scaledCellSize.y, z] = new AIGridCell();
                            grid[x, y + (int)scaledCellSize.y, z].state = "unwalkable";
                            grid[x, y + (int)scaledCellSize.y, z].position = new Vector3(x, y + (scaledCellSize.y), z);

                            //VisualisationSetter.instance.SpawnVisualisation(grid[x, y + (int)scaledCellSize.y, z].position, scaledCellSize, "unwalkable"); // Disabled for build optimisation

                            grid[x, y + ((int)scaledCellSize.y * 2), z] = new AIGridCell();
                            grid[x, y + ((int)scaledCellSize.y * 2), z].state = "unwalkable";
                            grid[x, y + ((int)scaledCellSize.y * 2), z].position = new Vector3(x, y + (scaledCellSize.y * 2), z);

                            //VisualisationSetter.instance.SpawnVisualisation(grid[x, y + ((int)scaledCellSize.y * 2), z].position, scaledCellSize, "unwalkable"); // Disabled for build optimisation

                            // Sets cells below at hit location as unwalkable

                            if (y - (int)scaledCellSize.y > 0)
                            {
                                grid[x, y - (int)scaledCellSize.y, z] = new AIGridCell();
                                grid[x, y - (int)scaledCellSize.y, z].state = "unwalkable";
                                grid[x, y - (int)scaledCellSize.y, z].position = new Vector3(x, y + (scaledCellSize.y), z);

                                //VisualisationSetter.instance.SpawnVisualisation(grid[x, y - (int)scaledCellSize.y, z].position, scaledCellSize, "unwalkable"); // Disabled for build optimisation
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

                                //VisualisationSetter.instance.SpawnVisualisation(grid[x, y, z].position, scaledCellSize, "air"); // Disabled for build optimisation

                            }
                            else if (!Physics.CheckBox(new Vector3(x, y + scaledCellSize.y, z), scaledCellSize, Quaternion.identity, layer)) // Sets the cell as walkable if there is nothing in the above cell
                            {

                                grid[x, y, z].state = "walkable";
                                grid[x, y, z].position = new Vector3(x, y, z);
                                walkableGrid.Add(grid[x, y, z]);

                                //VisualisationSetter.instance.SpawnVisualisation(grid[x, y, z].position, scaledCellSize, "walkable"); // Disabled for build optimisation

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

                                //VisualisationSetter.instance.SpawnVisualisation(grid[x, y, z].position, scaledCellSize, "stairs"); // Disabled for build optimisation

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

                                //VisualisationSetter.instance.SpawnVisualisation(grid[x, y, z].position, scaledCellSize, "unwalkable"); // Disabled for build optimisation
                            }
                        }

                    }

                    


                }
            }
        }
        //Debug.Log(grid.Length);

    }

    private void OnDrawGizmos() // Used for map visualisation for performance purposes
    {
        // Cell type visualisation through Gizmos

        if (showVisualisations)
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
                                    if (showVisualisationWalkable)
                                    {
                                        Gizmos.color = Color.green;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                case "stairs":
                                    if (showVisualisationStairs)
                                    {
                                        Gizmos.color = Color.yellow;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                case "unwalkable":
                                    if (showVisualisationUnwalkable)
                                    {
                                        Gizmos.color = Color.red;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                case "air":
                                    if (showVisualisationAir)
                                    {
                                        Gizmos.color = Color.magenta;
                                        Gizmos.DrawCube(new Vector3(x, y, z), scaledCellSize);
                                    }
                                    break;
                                default: //For showing errors
                                    if (showVisualisationError)
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

    public void ToggleVisualisation(string state)
    {
        switch (state)
        {
            case "goal":
                showVisualisationGoal = !showVisualisationGoal;
                break;
            case "pathTo":
                showVisualisationPathTo = !showVisualisationPathTo;
                break;
            case "calculatedPath":
                showVisualisationCalculatedPath = !showVisualisationCalculatedPath;
                break;
            case "jump":
                showVisualisationJump = !showVisualisationJump;
                break;
            default: 
                break;
        }
    }

}

[Serializable]
public class ExtraCosts
{
    public string name;
    public float cost = 0f;
}
