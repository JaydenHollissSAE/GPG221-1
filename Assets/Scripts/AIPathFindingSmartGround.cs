using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPathFindingSmartGround : AIPathFindingGround
{
    //[SerializeField] protected Vector3[,,] seenCells; // Allows me to map out a world with coordinates easily // Unused for stability and support
    [SerializeField] protected List<Vector3> seenCells = new List<Vector3>(); // Allows me to map out a world with coordinates easily
    [SerializeField] protected List<Vector3> currentLookingAt = new List<Vector3>();

    [SerializeField] protected Vector2 viewDistance = new Vector2(5, 5);
    protected Vector2 scaledViewDistance;
    [SerializeField] protected ItemData itemInFront = null;

    [SerializeField] protected List<LayerMask> viewableLayers = new List<LayerMask>();

    [SerializeField] protected List<ItemData> seenItems = new List<ItemData>();
    [SerializeField] protected List<Vector3> seenItemPositions = new List<Vector3>();

    [SerializeField] protected AIGoals currentTask = AIGoals.goToGoal;
    [SerializeField] protected ItemData.ItemTypes fetchingItem = ItemData.ItemTypes.nothing;

    [SerializeField] protected List<ItemData.ItemTypes> inventory = new List<ItemData.ItemTypes>();
    [SerializeField] protected List<ItemData.ItemTypes> neededItems = new List<ItemData.ItemTypes>();

    [SerializeField] protected List<Vector3> lockedPositions = new List<Vector3>();
    [SerializeField] protected List<ListBuffer> pathCellPositionsBuffer = new List<ListBuffer>();

    bool startWander = false;

    /// <summary>
    /// Different goals/tasks for the AI agent
    /// </summary>
    public enum AIGoals 
    {
        goToGoal,
        wander,
        fetch
    }

    protected override void Start()
    {
        scaledViewDistance = new Vector2(Mathf.CeilToInt(viewDistance.x), Mathf.CeilToInt(viewDistance.y));
        StartCoroutine(View()); // Starts the view that contantly runs
        base.Start();
    }


    /// <summary>
    /// Determines what to do based on the current goal/task
    /// </summary>
    protected override void FixedUpdate()
    {

        ItemBehaviour(); // Doesn't matter what task is happening, should run this at the start always, hence out of switch statement
        switch (currentTask)
        {
            default:
                base.FixedUpdate();
                break;
            case AIGoals.goToGoal:
                base.FixedUpdate();
                break;
            case AIGoals.fetch:
                ItemBehaviour();
                CheckForItems();
                FollowPath();
                break;
            case AIGoals.wander:
                CheckForItems();
                Wander();
                FollowPath();
                break;
        }

    }

    protected override void CheckGoal()
    {
        base.CheckGoal();
    }

    protected override void Update()
    {
        base.Update();
    }


    /// <summary>
    /// Checks if a needed item has been seen
    /// </summary>
    protected virtual void CheckForItems()
    {
        bool foundMatch = false;
        foreach (ItemData.ItemTypes item in neededItems)
        {
            ItemNeeded(item);
            foundMatch = true;
        }
        if (!foundMatch && neededItems.Count > 0)
        {
            currentTask = AIGoals.wander;
        }
    }






    /// <summary>
    /// Wanders around to fill in the unviewed areas
    /// </summary>
    protected virtual void Wander()
    {
        //if (Array.IndexOf(seenCells, pathTo) != -1)
        if (startWander || NewUnity.ContainsV3(seenCells, pathTo)) // Prevents a new wander spot from being selected when it doesn't need to be.
        {
            startWander = false;

            // Ensures the code doesn't try going outside of the supported area in the grid
            int[] xBounds = new int[2] { AIGrid.instances[gridNo].grid.GetLowerBound(0), AIGrid.instances[gridNo].grid.GetUpperBound(0) };
            int[] zBounds = new int[2] { AIGrid.instances[gridNo].grid.GetLowerBound(2), AIGrid.instances[gridNo].grid.GetUpperBound(2) };
            int[] yBounds = new int[2] { AIGrid.instances[gridNo].grid.GetLowerBound(1), AIGrid.instances[gridNo].grid.GetUpperBound(1) };
            
            List<Vector3> checkedPositions = new List<Vector3>();

            CalculatedPathData pathDataCore = null;

            // Stores variables locally to prevent issues from changing while function is running
            int localcharacterY = characterY;
            Vector3 localPosition = transform.position;

            // Goal is to fill out the viewed area so random works the most optimally

            while (checkedPositions.Count < (xBounds[0] + xBounds[1]) * (zBounds[0] + zBounds[1]))
            {
                if (localcharacterY > yBounds[0] && localcharacterY < yBounds[1])
                {
                    AIGridCell tmpCell = AIGrid.instances[gridNo].grid[UnityEngine.Random.Range(xBounds[0], xBounds[1]), localcharacterY, UnityEngine.Random.Range(zBounds[0], zBounds[1])];
                    if (!NewUnity.ContainsV3(checkedPositions, tmpCell.position))
                    {
                        checkedPositions.Add(tmpCell.position);
                        //if (Array.IndexOf(seenCells, tmpCell.position) != -1 && (tmpCell.state == AIGrid.GridStates.walkable || tmpCell.state == AIGrid.GridStates.stairs))
                        if (!NewUnity.ContainsV3(seenCells, tmpCell.position) && (tmpCell.state == AIGrid.GridStates.walkable || tmpCell.state == AIGrid.GridStates.stairs))
                        {

                            CalculatedPathData pathData = null;

                            // Tries multiple times to account for errors
                            for (int i = 0; i < 5; i++)
                            {
                                try
                                {
                                    pathData = AIPathFindingCore.CalculatePathCore(tmpCell.position, localPosition, 0, gridNo, lockedPositions);
                                }
                                catch { }
                                if (pathData != null && !pathData.failure) break;
                            }
                            if (pathData != null && !pathData.failure && pathData.pathFound)
                            {
                                pathDataCore = pathData;
                                pathDataCore.goal = tmpCell.position;
                                break;
                            }
                        }
                    }
                }
                else break;


            }

            if (pathDataCore != null) // Something has been found, variables should be set
            {
                pathTo = pathDataCore.goal;
                pathCellPositions = pathDataCore.pathCellPositions;
            }

            else // Failure to find somewhere, should go again
            {
                // Idk, maybe explode
                startWander = true;
            }

        }
        return;
    }


    /// <summary>
    /// Attempts to fetch a needed item if it has been seen before continuing or going around if faster
    /// </summary>
    /// <param name="fetchingItemType"></param>
    protected virtual void ItemNeeded(ItemData.ItemTypes fetchingItemType)
    {
        ItemData shortestKey = null;
        float shortestKeyCost = float.MaxValue;

        CalculatedPathData pathData1 = null;

        if (pathCellPositionsBuffer.Count == 0) // Only runs when there is nothing in the buffer used for fetching
        {

            // Checks if the needed item has been seen, path finds to it if it has been.
            foreach (ItemData item in seenItems)
            {
                if (item.type == fetchingItemType)
                {
                    CalculatedPathData pathData = null;
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            pathData = AIPathFindingCore.CalculatePathCore(item.position[0], transform.position, 0, gridNo);
                        }
                        catch { }
                        if (pathData != null && !pathData.failure) break;
                    }
                    if (pathData != null && pathData.pathFound)
                    {
                        if ((shortestKey == null || shortestKeyCost < pathData.cost)) // Gets the shortest path if multiple of the item has been seen
                        {
                            shortestKey = item;
                            pathData1 = pathData;
                        }
                    }

                }
            }

            // Gets the path for back to where it needs to go for the goal
            CalculatedPathData pathData2 = null;
            if (shortestKey != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        pathData2 = AIPathFindingCore.CalculatePathCore(pathTo, shortestKey.position[0], 0, gridNo);
                    }
                    catch { }
                    if (pathData2 != null && !pathData2.failure) break;
                }
            }
            else // If none of the needed item was found, agent should start wandering to find one
            {
                currentTask = AIGoals.wander;
                startWander = true;
                return;
            }

            // Adds the positions of the item in front of the agent to a list for blocked off areas
            List<Vector3> invalidPoints = lockedPositions;
            foreach (Vector3 pos in itemInFront.position)
            {
                invalidPoints.Add(pos);
            }


            // Checks if there is a faster way to get to the goal instead of getting the item and going back
            CalculatedPathData pathData3 = null;

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    pathData3 = AIPathFindingCore.CalculatePathCore(pathTo, transform.position, 0, gridNo, invalidPoints);
                }
                catch { }
                if (pathData3 != null && !pathData3.failure) break;
            }

            List<AIGridCell> gridPathData = new List<AIGridCell>();
            List<ListBuffer> listBuffer = new List<ListBuffer>();
            CalculatedPathData pathData4 = null;

            if (pathData3 == null || !pathData3.pathFound) // Sets the cost of the alternate path high if one isn't found so the previous path is chosen
            {
                pathData4 = new CalculatedPathData();
                pathData4.cost = float.MaxValue;
            }
            else 
            {
                pathData4 = pathData3;
            }


            // Checks the length of attempting each path
            if (pathData2 != null && pathData2.cost + shortestKeyCost < pathData4.cost) // If collecting the item is faster
            {
                ListBuffer listBuffer1 = new ListBuffer();
                listBuffer1.aiGridCellB = pathData1.pathCells;
                listBuffer.Add(listBuffer1);
                listBuffer1 = new ListBuffer();
                listBuffer1.aiGridCellB = pathData2.pathCells;
                listBuffer.Add(listBuffer1);
            }
            else if (pathData3 != null && pathData3.pathFound) // If the alternate path exists and is faster
            {
                ListBuffer listBuffer1 = new ListBuffer();
                listBuffer1.aiGridCellB = pathData3.pathCells;
                listBuffer.Add(listBuffer1);
            }
            else // If no alternate path is avalaible but collecting would take way too long
            {
                currentTask = AIGoals.wander;
                startWander = true;
                return;
            }


            // Sets variables and adds them to lists for following
            List<ListBuffer> listBuffer4 = new List<ListBuffer>();

            foreach (ListBuffer buffer in listBuffer)
            {
                List<Vector3> tmpList2 = new List<Vector3>();
                ListBuffer listBuffer3 = new ListBuffer();
                foreach (AIGridCell cell in buffer.aiGridCellB)
                {
                    tmpList2.Add(cell.position);
                }
                listBuffer3.vector3B = tmpList2;
                listBuffer4.Add(listBuffer3);

            }
            currentTask = AIGoals.fetch;
            pathCellPositionsBuffer.Clear();
            pathCellPositionsBuffer = listBuffer4;
            pathCellPositions.Clear();
        }


    }


    /// <summary>
    /// Item specific behaviour when interacting with them
    /// </summary>
    protected virtual void ItemBehaviour()
    {
        if (itemInFront == null) return;
        else
        {
            if (itemInFront.type == ItemData.ItemTypes.key) // Collects the item if it is a key
            {
                InteractItem(true);
            }
            else if (itemInFront.type == ItemData.ItemTypes.door)
            {
                if (inventory.Contains(ItemData.ItemTypes.key)) // Checks if the agent's inventory contains a key to use on the door, using it if avalaible
                {
                    foreach (Vector3 pos in itemInFront.position)
                    {
                        lockedPositions.Remove(pos);
                    }
                    InteractItem(false, AIGoals.fetch, ItemData.ItemTypes.key);
                }
                else // Marks a key as needed for the door if not in inventory
                {
                    if (!neededItems.Contains(ItemData.ItemTypes.key)) neededItems.Add(ItemData.ItemTypes.key);
                    if (currentTask != AIGoals.wander) currentTask = AIGoals.fetch;
                    pathCellPositions.Clear();
                }
            }
        }
        return;
    }


    /// <summary>
    /// Removes an item when collected. Removes item from inventory if one is needed
    /// </summary>
    /// <param name="collect"></param>
    /// <param name="goal"></param>
    /// <param name="useItem"></param>
    protected virtual void InteractItem(bool collect, AIGoals task = AIGoals.goToGoal, ItemData.ItemTypes useItem = ItemData.ItemTypes.nothing)
    {
        if (collect) inventory.Add(itemInFront.type);

        ActiveToggle.instance.ToggleActive(itemInFront.gameObject, false);
        currentTask = task;
        if (useItem != ItemData.ItemTypes.nothing)
        {
            inventory.Remove(useItem);
        }
        return;
    }


    /// <summary>
    /// Constantly checks a triangle in front and records what is seen
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator View()
    {
        while (true)
        {
            try
            {
                int adjacent1Count = 0;


                List<Vector3> checkPositions = new List<Vector3>();
                try
                {

                    // TRIGONOMETRY!!!!!
                    // Gets the view area in a triangle based on maths

                    float triAngle = Mathf.Atan(scaledViewDistance.x / scaledViewDistance.y); // "I should rename this angle varaible, I just used it in the wrong spot. Oh I know, it is for triangles so I'll put a tri prefix before it.........wait"


                    for (int adjacent = 1; adjacent < scaledViewDistance.y; adjacent += 1)
                    {

                        int opposite = Mathf.CeilToInt(Mathf.Tan(triAngle) * adjacent);
                        for (int oppositeX = -opposite; oppositeX <= opposite; oppositeX += 1)
                        {
                            Vector3 pos = new Vector3(Mathf.FloorToInt(transform.position.x + adjacent * Mathf.Sin(transform.rotation.y)), characterY, Mathf.FloorToInt(transform.position.z + oppositeX * Mathf.Cos(transform.rotation.y)));
                            if ((!NewUnity.ContainsV3(checkPositions, pos) && (!NewUnity.ContainsV3(seenCells, pos) || NewUnity.ContainsV3(seenItemPositions, pos))))
                            {
                                checkPositions.Add(pos);
                            }
                            else if (adjacent == 1) // Ensures the front row/right in front of the agent is always seen and checked
                            {
                                checkPositions.Add(pos);
                                adjacent1Count++;

                            }
                        }
                    }
                }
                catch (Exception ex) 
                { 
                    //Debug.LogError(ex);
                }



                bool itemFound = false;

                // Checks if any of the seen cells contained an item
                for (int i = 0; i < checkPositions.Count; i++)
                {
                    if (!NewUnity.ContainsV3(AIGrid.instances[gridNo].unwalkableGrid, checkPositions[i])) // Unwalkable can't contain anything or be interacted with, so no need to check
                    {
                        foreach (LayerMask layer in viewableLayers)
                        {
                            // Checks the cell on the layer
                            RaycastHit hit2;
                            bool hitDetction2 = Physics.BoxCast(checkPositions[i], AIGrid.instances[gridNo].scaledCellSize/2, checkPositions[i], out hit2, Quaternion.identity, Mathf.Infinity, layer);
                            NewUnity.DrawBoxCastBox(checkPositions[i], AIGrid.instances[gridNo].scaledCellSize/2, Quaternion.identity, checkPositions[i], Mathf.Infinity, Color.white);
                            if (hitDetction2)
                            {
                                ItemData itemData = hit2.collider.GetComponent<ItemData>(); 
                                if (itemData != null)
                                {
                                    if (!NewUnity.ContainsV3(itemData.position, checkPositions[i]))
                                    {
                                        itemData.position.Add(checkPositions[i]);
                                    }

                                    if (!seenItems.Contains(itemData))
                                    {
                                        seenItems.Add(itemData);
                                    }
                                    foreach (Vector3 pos in itemData.position)
                                    {
                                        if (!NewUnity.ContainsV3(seenItemPositions, pos)) seenItemPositions.Add(pos);
                                        if (!NewUnity.ContainsV3(lockedPositions, pos)) lockedPositions.Add(pos);
                                    }
                                    if (true)
                                    {
                                        if (!itemFound && itemInFront != itemData)
                                        {
                                            itemInFront = itemData;
                                        }
                                        if (itemData != null) itemFound = true;
                                    }

                                }
                            }
                            else if (NewUnity.ContainsV3(seenItemPositions, checkPositions[i])) // Removes item from list if it is no longer there
                            {
                                seenItemPositions.Remove(checkPositions[i]);
                                List<ItemData> tmpItems = seenItems;
                                for (int j = 0; j < seenItems.Count; j++)
                                {
                                    if (NewUnity.ContainsV3(seenItems[j].position,checkPositions[i])) tmpItems.Remove(seenItems[j]);
                                }
                                seenItems.Clear();
                                seenItems = tmpItems;


                            }
                        }

                    }
                    //seenCells[(int)checkPositions[i].x, (int)checkPositions[i].y, (int)checkPositions[i].z] = (checkPositions[i]);
                    if (!NewUnity.ContainsV3(seenCells, checkPositions[i])) seenCells.Add(checkPositions[i]);
                }
                currentLookingAt.Clear();
                currentLookingAt = checkPositions;
            }
            catch (Exception e) 
            { 
                //Debug.Log(e);
            }

            yield return null;
            //Debug.Log("Active");
        }

    }


    private void OnDrawGizmos() // Used for map visualisation for performance purposes
    {
        // Cell type visualisation through Gizmos

        if (AIGrid.instances[gridNo] != null)
        {
            if (AIGrid.instances[gridNo].showVisualisations)
            {
                for (int i = 0; i < currentLookingAt.Count; i += 1)
                {
                    if (i > 0) Gizmos.color = new Color(1, 0, 1);
                    else Gizmos.color = new Color(0, 0, 0); // First is black for debugging purposes
                    Gizmos.DrawCube(currentLookingAt[i], AIGrid.instances[gridNo].scaledCellSize);
                }
            }

        }

    }
    



    protected override void FollowPath()
    {
        // If the path finding is empty, the buffer is checked and used if avaliable
        if (pathCellPositions.Count < 1) 
        { 
            if (pathCellPositionsBuffer.Count > 0)
            {
                pathCellPositions = pathCellPositionsBuffer[0].vector3B;
                pathCellPositionsBuffer.RemoveAt(0);
            }
        }

        base.FollowPath();

        // Visualisations
        try
        {
            clearCalculatedPathVisualisation.Invoke();
        }
        catch { }
        try
        {
            foreach (Vector3 pos1 in pathCellPositions)
            {
                if (enableMyVisualisations) VisualisationSetter.instance.SpawnVisualisation(pos1, AIGrid.instances[gridNo].scaledCellSize, VisualisationSetter.VisualisationStates.calculatedPath, gameObject);
            }
        }
        catch { }
    }

    protected override void ColliderJump(Collision collision)
    {
        
        if (!NewUnity.ContainsLM(viewableLayers, collision.gameObject.layer)) // Prevents character from climibing doors and other items
        {
            Debug.Log(collision.gameObject.layer);
            base.ColliderJump(collision); 
        }
    }

    protected override IEnumerator CalculatePath() // Uses the core path finding functionality for more consistent behaviour with other functions and better functionality tie in
    {
        if (!awaitCalculation)
        {
            if (pathTo == Vector3.zero) SetPathTo();
            awaitCalculation = true;
            pathCalculated = true;
            CalculatedPathData data = new CalculatedPathData();
            //Debug.Log("Start Calculation");



            for (int i = 0; i < 10; i++)
            {
                try
                {
                    data = AIPathFindingCore.CalculatePathCore(pathTo, transform.position, 0, gridNo, lockedPositions);
                    Debug.Log($"{pathCellPositions.Count} {data.pathCellPositions.Count} {characterY}");
                }
                catch { }
                if (data != null && !data.failure)
                {
                    pathCellPositionsBuffer.Clear();
                    pathCellPositions.Clear();
                    pathCellPositions = data.pathCellPositions;
                    //Debug.Log($"{pathCellPositions.Count} {data.pathCellPositions.Count} {characterY}");
                    pathCalculated = true;
                    //Debug.Log("Calculation Success");
                    break;
                }
                yield return null;
            }
            if (data == null || data.failure)
            {
                pathCalculated = false;
                //Debug.Log("Calculation Failure");
            }

            yield return null;


            awaitCalculation = false;
        }
        yield return null;




    }

}

