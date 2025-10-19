using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class AIPathFinding : MonoBehaviour
{

    public Vector3 goal = Vector3.zero;
    public Vector3 pathTo = Vector3.zero;
    Vector3 pastGoal = Vector3.zero;
    Vector3 pastPathTo = Vector3.zero;

    private bool settingPath = false;
    private bool settingGoal = false;
    private bool pathCalculated = false;
    private bool awaitCalculation = false;

    [SerializeField] int characterY = 10000000;

    List<Vector3> pathCellPositions = new List<Vector3>();

    int pathResetCounter = 0;

    Rigidbody rb;

     
    public UnityEvent clearPathToVisualisation;
    public UnityEvent clearGoalVisualisation;
    public UnityEvent clearCalculatedPathVisualisation;
    public UnityEvent clearJumpVisualisation;



    [Tooltip("Allow Gizmos to be shown for this AI object")]
    public bool enableMyGizmos = false;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        //Debug.Log(Vector3.Distance(transform.position, goal));
        //Debug.Log(settingGoal);
        if (!settingGoal && (goal == Vector3.zero || Input.GetKeyDown(KeyCode.Tab) || Vector3.Distance(transform.position, goal) < 1.0f || pathResetCounter >= 10)) // Tab used as debug control to force a change
        {
            settingGoal = true;
            //Debug.Log("Trigger Change");
            pathResetCounter = 0;
            SetGoal();
        }

        characterY = (int)Mathf.Floor(transform.position.y);

        if (!settingPath && (goal != pastGoal || ((goal.y >= characterY && characterY != pathTo.y) || (goal.y < characterY && characterY - (int)AIGrid.instance.scaledCellSize.y * 2 != pathTo.y)) || pathTo == Vector3.zero || characterY == 10000000 || Input.GetKeyDown(KeyCode.LeftShift))) // Left Shift used as debug control to force a change
        {
            settingPath = true;
            pastGoal = goal;
            SetPathTo();
        }

        if (pathTo != pastPathTo) // Resets check value to allow resets and failsafes to work properly
        {
            pastPathTo = pathTo;
            pathCalculated = false;
            //Debug.Log("Reset Path");
        }

        if (!pathCalculated) StartCoroutine(CalculatePath());

    }
    private void Update()
    {
        FollowPath(); // Done in update rather than FixedUpdate to make movement more smooth
    }

    void SetPathTo() 
    {

        clearPathToVisualisation.Invoke();
        



        int localCharacterY = characterY;
        if (localCharacterY == 10000000) localCharacterY = (int)Mathf.Floor(transform.position.y);
        //Debug.Log("Set Path");
        //if (Mathf.Abs(goal.y - transform.position.y) >= AIGrid.instance.scaledCellSize.y/2)
        
        // If the character's Y position is the same as the goal, the pathTo step value is set to the goal
        if (localCharacterY == goal.y)
        {
            pathTo = goal;
        }

        else // Sets a pathTo point based on where the goal is reliative to the character
        {

            if (goal.y < localCharacterY) // If the goal is below the character the pathTo value needs to be going downwards
            {
                localCharacterY -= (int)AIGrid.instance.scaledCellSize.y*2; // Needed or pathTo gets stuck in platforms and freaks out
                //localCharacterY -= (int)AIGrid.instance.scaledCellSize.y; // Has to be the size of 2 cells rather than 1 or the goal is stuck in the floor and can't be path finded to
            }

            bool validStairs = false;

            // Checks if there are stairs on the desired level which the character can path find to
            // This is just a check. The sequential nature causes selection issues if used
            foreach (AIGridCell pos in AIGrid.instance.stairsGrid)
            {
                if (pos.position.y == localCharacterY)
                {
                    validStairs = true;
                }
            }

            // Selects a random stair on the correct Y to path find to. Doesn't need to be exact as it is just for getting to a different layer, so randomness is the most effective way.
            while (validStairs)
            {
                int x = Random.Range(0, (int)AIGrid.instance.scaledCheckDistance.x);
                int z = Random.Range(0, (int)AIGrid.instance.scaledCheckDistance.z);
                if ((AIGrid.instance.grid[x, localCharacterY, z].state == "stairs"))
                {
                    //Debug.Log("Path found");
                    pathTo = AIGrid.instance.grid[x, localCharacterY, z].position;
                    break;
                }
            }
        }


        VisualisationSetter.instance.SpawnVisualisation(pathTo, AIGrid.instance.scaledCellSize, "pathTo", gameObject);
            

        settingPath = false;
        StartCoroutine(AutoPathReset()); // Starts failsafe

        return;
    }

    IEnumerator AutoPathReset()
    {
        // Failsafe that resets pathTo if it hasn't been met in too long
        Vector3 localPathTo = pathTo; // Stores the current location for a failsafe
        float timePassed = 0f;
        while (true)
        {
            if (localPathTo != pathTo) // Ends the failsafe if the actual pathTo is different from the local stored one, indicating it changed elsewhere
            {
                break;
            }
            else if (timePassed >= 60f)
            {
                SetPathTo();
                break;
            }
            timePassed += Time.deltaTime;
            yield return null;
        }
        yield return null;

    }
    
    IEnumerator AutoGoalReset()
    {
        // Failsafe that resets goal if it hasn't been met in too long
        Vector3 localGoal = goal; // Stores the current location for a failsafe
        float timePassed = 0f;
        while (true)
        {
            if (localGoal != pathTo) // Ends the failsafe if the actual goal is different from the local stored one, indicating it changed elsewhere
            {
                break;
            }
            else if (timePassed >= 300f)
            {
                SetGoal();
                break;
            }
            timePassed += Time.deltaTime;
            yield return null;
        }
        yield return null;

    }

    IEnumerator CalculatePath()
    {
        // Calculated as a Coroutine to prevent lag spikes via splitting it across ticks

        if (!awaitCalculation)
        {

            awaitCalculation = true;
            pathCalculated = true;
            yield return new WaitForSeconds(0.1f);


            clearCalculatedPathVisualisation.Invoke();

            pathCellPositions.Clear();
            if (pathTo == Vector3.zero) SetPathTo();

            List<AIGridCell> openCells = new List<AIGridCell>();
            List<AIGridCell> closedCells = new List<AIGridCell>();

            // Cells are required to be ints, so the position is scaled down to them
            Vector3 characterCellPos = new Vector3(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y), Mathf.FloorToInt(transform.position.z));
            int checkingX = (int)characterCellPos.x;
            int checkingY = (int)characterCellPos.y;
            int checkingZ = (int)characterCellPos.z;

            AIGridCell currentCell = null;
            AIGridCell startCell = null;
            try
            {
                currentCell = AIGrid.instance.grid[checkingX, checkingY, checkingZ];

            }
            catch { }


            if (currentCell == null) yield return new WaitForSeconds(2f); // Indicates that the grid is not made yet, so waits for 2 seconds as a failsafe
            try
            {
                currentCell = AIGrid.instance.grid[checkingX, checkingY, checkingZ];
                currentCell.gCost = 0f;
                currentCell.hCost = 0f;
                startCell = currentCell;
                openCells.Add(currentCell);

            }
            catch // Indicates a failure with fetching the grid, so halts to go ahead
            {
                pathCalculated = false;
            }
            bool localPathCalculated = false;
            while (pathCalculated && !localPathCalculated)
            {
                if (!pathCalculated) break; // Failsafe for changing target
                AIGridCell shortest = null;
                checkingX = Mathf.FloorToInt(currentCell.position.x);
                checkingZ = Mathf.FloorToInt(currentCell.position.z);
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

                                    if (!closedCells.Contains(checkingCell) && ((checkingCell.state == "walkable" || checkingCell.state == "stairs") && (AIGrid.instance.grid[checkingX + i, checkingY - (int)AIGrid.instance.scaledCellSize.y, checkingZ + j] == null || AIGrid.instance.grid[checkingX + i, checkingY - 1, checkingZ + j].state != "air")))
                                    {
                                        checkingCell.hCost = Vector3.Distance(checkingCell.position, pathTo);
                                        if (k == 0) checkingCell.gCost = currentCell.gCost + Vector3.Distance(currentCell.position, checkingCell.position);
                                        else checkingCell.gCost = currentCell.gCost + Vector3.Distance(AIGrid.instance.grid[Mathf.FloorToInt(currentCell.position.x), Mathf.FloorToInt(currentCell.position.y - (k * (int)AIGrid.instance.scaledCellSize.y)), Mathf.FloorToInt(currentCell.position.z)].position, checkingCell.position);
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
                                        checkingCell.gCost = 100000f;
                                        checkingCell.hCost = 100000f;
                                        checkingCell.fCost = 100000f;
                                        if (!closedCells.Contains(checkingCell)) closedCells.Add(checkingCell);
                                    }
                                }
                            }
                            catch { } // If error occurs, continue on like nothing happened. No need for extra check failsafe as if one isn't used, others are.

                            if (valuesPassed % 20 == 0) yield return null; // Ensures the checks update faster while not overloading, thus speeding things up without causing issues
                        }

                    }

                }
                try
                {
                    // Reorders the cells in the lists for calculations
                    if (shortest != null)
                    {
                        currentCell = shortest;
                        openCells.Add(currentCell);
                        pathCellPositions.Add(currentCell.position);
                    }
                    else if (openCells.Count > 1)
                    {
                        closedCells.Add(openCells[openCells.Count - 1]);
                        openCells.RemoveAt(openCells.Count - 1);
                        currentCell = openCells[openCells.Count - 1];
                    }

                }
                catch // Indicates a major failure with the calculations, so halts to go ahead
                {
                    awaitCalculation = false;
                }

                yield return null;

                // Ends when back to the starting cell
                if (currentCell == startCell)
                {
                    break;
                }


                foreach (Vector3 pos1 in pathCellPositions)
                {
                    VisualisationSetter.instance.SpawnVisualisation(pos1, AIGrid.instance.scaledCellSize, "calculatedPath", gameObject);
                }

            }
            awaitCalculation = false;
            if (!localPathCalculated) // If the flag for a calculated path is not met, tries to calculate again and increments the counter for how much it has tried.
            {
                pathResetCounter++;
                SetPathTo();
            }
        }
        yield return null;

    }


    void FollowPath()
    {
        if (!awaitCalculation && !AIGrid.instance.disableMove) // Checks if the logic is allowing the character to move
        {
            if (pathCellPositions.Count > 0) // Ensures a path exists to follow
            {
                // Gets the closest path position to the character and removes one ones before that to prevent the following trying to go backwards
                int closestIndex = 0;
                float closestDistance = 10000f;
                for (int i = 0; i < pathCellPositions.Count; i++)
                {
                    Vector3 tmpPos = pathCellPositions[i];
                    tmpPos.y = characterY;
                    pathCellPositions[i] = tmpPos;
                    float distance = Vector3.Distance(transform.position, pathCellPositions[i]);
                    if (distance < closestDistance)
                    {
                        closestIndex = i;
                        closestDistance = distance;
                    }
                }
                if (closestIndex > 0) pathCellPositions.RemoveRange(0, closestIndex);



                //Debug.Log("Move");


                // Looks towards the desired path position and moves towards it
                if (pathCellPositions[0].y < characterY)
                {
                    transform.LookAt(new Vector3(pathCellPositions[0].x, transform.forward.y, pathCellPositions[0].z), Vector3.up);
                }
                else transform.LookAt(pathCellPositions[0], Vector3.up);

                // I love this but it breaks when going down // I don't remember what this comment meant but I'll leave it because why not
                rb.linearVelocity = transform.forward * 10f;
                if (pathCellPositions.Count > 1) pathCellPositions[0] = CheckPathClarity(pathCellPositions[0], pathCellPositions[1]);

                // If the character is close enough to the position that position is removed from the path finding list
                if (Vector3.Distance(transform.position, pathCellPositions[0]) < 0.8f)
                {
                    pathCellPositions.RemoveAt(0);
                }

            }
            else StartCoroutine(CalculatePath()); // Calculates the path if the path is empty
            
        }

    }

    Vector3 CheckPathClarity(Vector3 inputPos, Vector3 nextInputPos)
    {
        // Checks if anything is in the way of the character
        Vector3 outputPos = inputPos;
        if (inputPos.y < characterY || AIGrid.instance.grid[(int)inputPos.x, (int)inputPos.y, (int)inputPos.z].state == "stairs") return inputPos; // Returns without doing anything if the target position is below the character or the target cell is stairs
        else
        {
            RaycastHit hit;
            bool hitDetction = Physics.BoxCast(new Vector3(inputPos.x, inputPos.y, inputPos.z), AIGrid.instance.scaledCellSize, Vector3.zero, out hit); // Checks if anything exists in the target cell
            if (hitDetction)
            {
                // Checks next to the cell on either the X or Z value depending on the next cell position
                for (int i = -1; i <= 1; i+=2)
                {
                    Vector3 checkPos;
                    if (nextInputPos.x != inputPos.x)
                    {
                        checkPos = new Vector3(inputPos.x + (AIGrid.instance.scaledCellSize.x * i), inputPos.y, inputPos.z);
                    }
                    else
                    {
                        checkPos = new Vector3(inputPos.x, inputPos.y, inputPos.z + (AIGrid.instance.scaledCellSize.z * i));
                    }

                    // Checks if something exists in the cell next to it
                    RaycastHit hit2;
                    bool hitDetction2 = Physics.BoxCast(checkPos, AIGrid.instance.scaledCellSize, Vector3.zero, out hit2);
                    if (!hitDetction2) // If nothing hit, checks if the cell is able to be traversed, sets new target if able to be
                    {
                        string state = AIGrid.instance.grid[(int)checkPos.x, (int)checkPos.y, (int)checkPos.z].state;
                        if (state == "stairs" || state == "walkable")
                        {
                            outputPos = checkPos;
                            break;
                        }
                    }

                }
            }

        }
        return outputPos;

    }

    private void OnDestroy()
    {
        try
        {
            clearGoalVisualisation.Invoke();
        } catch { }
        try
        {
            clearPathToVisualisation.Invoke();
        } catch { }
        try
        {
            clearCalculatedPathVisualisation.Invoke();
        } catch { }
        try
        {
            clearJumpVisualisation.Invoke();
        } catch { }
    }


    void SetGoal()
    {
        // Simple goal setting based on walkable locations
        if (AIGrid.instance.walkableGrid.Count > 0)
        {
            Vector3 tmpGoal = goal;
            clearGoalVisualisation.Invoke();




            goal = AIGrid.instance.walkableGrid[Random.Range(0, AIGrid.instance.walkableGrid.Count)].position;

            VisualisationSetter.instance.SpawnVisualisation(goal, AIGrid.instance.scaledCellSize, "goal", gameObject);
        }
        
        settingGoal = false;
        StartCoroutine(AutoGoalReset());
    }


    //private void OnDrawGizmos()
    //{
    //    // Gizmos control based on individual variable and global variable
    //    if (enableMyGizmos && AIGrid.instance != null && AIGrid.instance.showGizmos)
    //    {

    //        if (goal != Vector3.zero && AIGrid.instance.showVisualisationGoal)
    //        {
    //            Gizmos.color = Color.cyan;
    //            Gizmos.DrawCube(goal, AIGrid.instance.scaledCellSize);
    //        }
    //        if (pathTo != Vector3.zero && AIGrid.instance.showVisualisationPathTo)
    //        {
    //            //Debug.Log("Display PathTo");
    //            Gizmos.color = Color.black;
    //            Gizmos.DrawCube(pathTo, AIGrid.instance.scaledCellSize);
    //        }
    //        if (jumpPos != Vector3.zero && AIGrid.instance.showVisualisationJump)
    //        {
    //            //Debug.Log("Display PathTo");
    //            Gizmos.color = new Color(0.5f,0.5f,0.5f);
    //            Gizmos.DrawCube(jumpPos, AIGrid.instance.scaledCellSize);
    //        }
    //        if (pathCellPositions.Count > 0 && AIGrid.instance.showVisualisationCalculatedPath)
    //        {
    //            for (int i = 0; i < pathCellPositions.Count; i++)
    //            {
    //                Gizmos.color = new Color(1, 0.5f, 0);
    //                Gizmos.DrawCube(pathCellPositions[i], AIGrid.instance.scaledCellSize);

    //            }


    //        }
    //    }

    //}


    private void OnCollisionEnter(Collision collision)
    {
        ColliderJump(collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        ColliderJump(collision);
    }

    private void ColliderJump(Collision collision)
    {
        // Makes character jump up stairs
        if (collision.transform.position.y >= characterY)
        {
            Vector3 collidedWith = transform.position;
            Vector3 adjustBy = AIGrid.instance.scaledCellSize;
            if (collision.contacts[0].point.x < transform.position.x) adjustBy.x *= -1;
            adjustBy.y = 0f;
            if (collision.contacts[0].point.z < transform.position.z) adjustBy.z *= -1;
            collidedWith += adjustBy;

            clearJumpVisualisation.Invoke();

            jumpPos = new Vector3(Mathf.FloorToInt(collidedWith.x), Mathf.FloorToInt(collidedWith.y), Mathf.FloorToInt(collidedWith.z));

            VisualisationSetter.instance.SpawnVisualisation(jumpPos, AIGrid.instance.scaledCellSize, "jump", gameObject);


            string state = AIGrid.instance.grid[Mathf.FloorToInt(collidedWith.x), Mathf.FloorToInt(collidedWith.y), Mathf.FloorToInt(collidedWith.z)].state;
            //Debug.Log(state);
            if (state == "stairs")
            {
                rb.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);
                rb.linearVelocity = transform.forward * 10f;
            }
            else if (state == "unwalkable" || state == "air") // Stops being stuck in the air
            {
                //Debug.Log("Push Down");
                rb.AddForce(Vector3.down * 4f, ForceMode.VelocityChange);

            }
        }

    }

    Vector3 jumpPos = Vector3.zero;



}
