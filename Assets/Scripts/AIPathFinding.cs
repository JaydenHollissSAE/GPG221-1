using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
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


    [Tooltip("Allow Gizmos to be shown for this AI object")]
    public bool enableMyGizmos = false;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Debug.Log(Vector3.Distance(transform.position, goal));
        Debug.Log(settingGoal);
        if (!settingGoal && (goal == Vector3.zero || Input.GetKeyDown(KeyCode.Tab) || Vector3.Distance(transform.position, goal) < 1.0f || pathResetCounter >= 10))
        {
            settingGoal = true;
            Debug.Log("Trigger Change");
            pathResetCounter = 0;
            SetGoal();
        }

        characterY = (int)Mathf.Floor(transform.position.y);

        if (!settingPath && (goal != pastGoal || ((goal.y >= characterY && characterY != pathTo.y) || (goal.y < characterY && characterY - (int)AIGrid.instance.scaledCellSize.y * 2 != pathTo.y)) || pathTo == Vector3.zero || characterY == 10000000 || Input.GetKeyDown(KeyCode.LeftShift)))
        {
            settingPath = true;
            pastGoal = goal;
            SetPathTo();
        }

        if (pathTo != pastPathTo)
        {
            pastPathTo = pathTo;
            pathCalculated = false;
            //Debug.Log("Reset Path");
        }

        if (!pathCalculated) StartCoroutine(CalculatePath());

    }
    private void Update()
    {
        FollowPath();
    }

    void SetPathTo() 
    {
        int localCharacterY = characterY;
        if (localCharacterY == 10000000) localCharacterY = (int)Mathf.Floor(transform.position.y);
        //Debug.Log("Set Path");
        //if (Mathf.Abs(goal.y - transform.position.y) >= AIGrid.instance.scaledCellSize.y/2)
        if (true)
        {
            if (localCharacterY == goal.y)
            {
                pathTo = goal;
            }

            else
            {

                if (goal.y < localCharacterY)
                {
                    localCharacterY -= (int)AIGrid.instance.scaledCellSize.y*2; //Needed or pathTo gets stuck in platforms and freaks out
                    //localCharacterY -= (int)AIGrid.instance.scaledCellSize.y;
                }

                bool validStairs = false;

                foreach (AIGridCell pos in AIGrid.instance.stairsGrid)
                {
                    if (pos.position.y == localCharacterY)
                    {
                        validStairs = true;
                    }
                }

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

           

            
        }
        settingPath = false;
        StartCoroutine(AutoPathReset());

        return;
    }

    IEnumerator AutoPathReset()
    {
        Vector3 localPathTo = pathTo;
        float timePassed = 0f;
        while (true)
        {
            if (localPathTo != pathTo)
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
        Vector3 localGoal = goal;
        float timePassed = 0f;
        while (true)
        {
            if (localGoal != pathTo)
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
        if (!awaitCalculation)
        {

            awaitCalculation = true;
            pathCalculated = true;
            yield return new WaitForSeconds(0.1f);
            pathCellPositions.Clear();
            if (pathTo == Vector3.zero) SetPathTo();

            List<AIGridCell> openCells = new List<AIGridCell>();
            List<AIGridCell> closedCells = new List<AIGridCell>();
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
            catch
            {

            }

            if (currentCell == null) yield return new WaitForSeconds(2f);
            try
            {
                currentCell = AIGrid.instance.grid[checkingX, checkingY, checkingZ];
                currentCell.gCost = 0f;
                currentCell.hCost = 0f;
                startCell = currentCell;
                openCells.Add(currentCell);

            }
            catch 
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

                if (pathTo.y < checkingY) yloop = 3;

                for (int k = 0; k < yloop; k++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        for (int i = -1; i < 2; i++)
                        {
                            valuesPassed++;
                            try
                            {
                                AIGridCell checkingCell = AIGrid.instance.grid[checkingX + i, checkingY - k, checkingZ + j];

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
                            catch { }

                            if (valuesPassed % 20 == 0) yield return null; // Ensures the checks update faster while not overloading, thus speeding things up without causing issues
                        }

                    }

                }
                try
                {
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
                catch
                {
                    awaitCalculation = false;
                }

                yield return null;

                if (currentCell == startCell)
                {
                    break;
                }
            }
            awaitCalculation = false;
            if (!localPathCalculated)
            {
                pathResetCounter++;
                SetPathTo();
            }
        }
        yield return null;

    }


    void FollowPath()
    {
        if (!awaitCalculation && !AIGrid.instance.disableMove)
        {
            if (pathCellPositions.Count > 0)
            {
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

                //transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, pathCellPositions[0], Time.fixedDeltaTime*10f, 2.0f * 10f));
                if (pathCellPositions[0].y < characterY)
                {
                    transform.LookAt(new Vector3(pathCellPositions[0].x, transform.forward.y, pathCellPositions[0].z), Vector3.up);
                }
                else transform.LookAt(pathCellPositions[0], Vector3.up);

                // I love this but it breaks when going down
                rb.linearVelocity = transform.forward * 10f;
                pathCellPositions[0] = CheckPathClarity(pathCellPositions[0], pathCellPositions[1]);

                if (Vector3.Distance(transform.position, pathCellPositions[0]) < 0.8f) pathCellPositions.RemoveAt(0);

            }
            else StartCoroutine(CalculatePath());
            
        }

    }

    Vector3 CheckPathClarity(Vector3 inputPos, Vector3 nextInputPos)
    {
        Vector3 outputPos = inputPos;
        if (inputPos.y < characterY || AIGrid.instance.grid[(int)inputPos.x, (int)inputPos.y, (int)inputPos.z].state == "stairs") return inputPos;
        else
        {
            RaycastHit hit;
            bool hitDetction = Physics.BoxCast(new Vector3(inputPos.x, inputPos.y, inputPos.z), AIGrid.instance.scaledCellSize, Vector3.zero, out hit);
            if (hitDetction)
            {
                if (nextInputPos.x != inputPos.x)
                {
                    for (int i = -1; i <= 1; i+=2)
                    {
                        RaycastHit hit2;
                        Vector3 checkPos = new Vector3(inputPos.x + (AIGrid.instance.scaledCellSize.x * i), inputPos.y, inputPos.z);
                        bool hitDetction2 = Physics.BoxCast(checkPos, AIGrid.instance.scaledCellSize, Vector3.zero, out hit2);
                        if (!hitDetction2)
                        {
                            string state = AIGrid.instance.grid[(int)checkPos.x, (int)checkPos.y, (int)checkPos.z].state;
                            if (state == "stairs" || state == "walkable")
                            {
                                outputPos = checkPos;
                            }
                        }

                    }
                }
                else
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        RaycastHit hit2;
                        Vector3 checkPos = new Vector3(inputPos.x, inputPos.y, inputPos.z + (AIGrid.instance.scaledCellSize.z * i));
                        bool hitDetction2 = Physics.BoxCast(checkPos, AIGrid.instance.scaledCellSize, Vector3.zero, out hit2);
                        if (!hitDetction2)
                        {
                            string state = AIGrid.instance.grid[(int)checkPos.x, (int)checkPos.y, (int)checkPos.z].state;
                            if (state == "stairs" || state == "walkable")
                            {
                                outputPos = checkPos;
                            }
                        }
                    }

                }


                
            }


        }
        return outputPos;

    }


    void SetGoal()
    {
        //AIGridCell[,,] grid = AIGrid.instance.grid;
        if (AIGrid.instance.walkableGrid.Count > 0)
        {

            goal = AIGrid.instance.walkableGrid[Random.Range(0, AIGrid.instance.walkableGrid.Count)].position;
        }
        //else //Debug.LogError("walkableGrid == 0");
        settingGoal = false;
        StartCoroutine(AutoGoalReset());
    }


    private void OnDrawGizmos()
    {
        if (enableMyGizmos && AIGrid.instance != null && AIGrid.instance.showGizmos)
        {

            if (goal != Vector3.zero && AIGrid.instance.showGizmosGoal)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(goal, AIGrid.instance.scaledCellSize);
            }
            if (pathTo != Vector3.zero && AIGrid.instance.showGizmosPathTo)
            {
                //Debug.Log("Display PathTo");
                Gizmos.color = Color.black;
                Gizmos.DrawCube(pathTo, AIGrid.instance.scaledCellSize);
            }
            if (jumpPos != Vector3.zero && AIGrid.instance.showGizmosJump)
            {
                //Debug.Log("Display PathTo");
                Gizmos.color = new Color(0.5f,0.5f,0.5f);
                Gizmos.DrawCube(jumpPos, AIGrid.instance.scaledCellSize);
            }
            if (pathCellPositions.Count > 0 && AIGrid.instance.showGizmosCalculatedPath)
            {
                for (int i = 0; i < pathCellPositions.Count; i++)
                {
                    Gizmos.color = new Color(1, 0.5f, 0);
                    Gizmos.DrawCube(pathCellPositions[i], AIGrid.instance.scaledCellSize);

                }


            }
        }

    }


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
        if (collision.transform.position.y >= characterY)
        {
            Vector3 collidedWith = transform.position;
            Vector3 adjustBy = AIGrid.instance.scaledCellSize;
            if (collision.contacts[0].point.x < transform.position.x) adjustBy.x *= -1;
            adjustBy.y = 0f;
            if (collision.contacts[0].point.z < transform.position.z) adjustBy.z *= -1;
            collidedWith += adjustBy;
            jumpPos = new Vector3(Mathf.FloorToInt(collidedWith.x), Mathf.FloorToInt(collidedWith.y), Mathf.FloorToInt(collidedWith.z));
            string state = AIGrid.instance.grid[Mathf.FloorToInt(collidedWith.x), Mathf.FloorToInt(collidedWith.y), Mathf.FloorToInt(collidedWith.z)].state;
            Debug.Log(state);
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
