using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEditor.Progress;

public class AIPathFindingSmartGround : AIPathFindingGround
{
    //[SerializeField] protected Vector3[,,] seenCells; // Allows me to map out a world with coordinates easily
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

    public enum AIGoals 
    {
        goToGoal,
        wander,
        fetch
    }

    protected override void Start()
    {
        //List<Vector3> testList = new List<Vector3>() { new Vector3(10,2,4), new Vector3(12, 1, -4), new Vector3(230, 52, 99) };
        //Debug.Log(testList.Contains(Vector3.zero));
        //Debug.Log(testList.Contains(new Vector3(10, 2, 4)));
        //Debug.Log(testList.Contains(new Vector3(12, 1, -4)));
        //Debug.Log(testList.Contains(Vector3.one));



        scaledViewDistance = new Vector2(Mathf.CeilToInt(viewDistance.x), Mathf.CeilToInt(viewDistance.y));
        StartCoroutine(View());
        base.Start();
    }


    /// <summary>
    /// Determines what to do based on the current goal/task
    /// </summary>
    protected override void FixedUpdate()
    {

        ItemBehaviour();
        //Debug.Log("Passed");
        switch (currentTask)
        {
            default:
                base.FixedUpdate();
                break;
            case AIGoals.goToGoal:
                base.FixedUpdate();
                break;
            case AIGoals.fetch:
                //CheckForItems();
                //ItemNeeded(itemInFront)
                ItemBehaviour();
                CheckForItems();
                //FollowPath();
                base.FixedUpdate();
                break;
            case AIGoals.wander:
                CheckForItems();
                Wander();
                FollowPath();
                break;
        }
        //SetPathTo(myGoal);

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
        if (NewUnity.ContainsV3(seenCells, pathTo))
        {
            int xLength = AIGrid.instance.grid.GetLength(0);
            int yLength = AIGrid.instance.grid.GetLength(2);
            List<Vector3> checkedPositions = new List<Vector3>();

            CalculatedPathData pathDataCore = null;

            // Goal is to fill out the viewed area so random works the most optimally

            while (checkedPositions.Count < xLength * yLength)
            {
                AIGridCell tmpCell = AIGrid.instance.grid[UnityEngine.Random.Range(0, xLength), (int)pathTo.y, UnityEngine.Random.Range(0, yLength)];
                if (!NewUnity.ContainsV3(checkedPositions,tmpCell.position))
                {
                    checkedPositions.Add(tmpCell.position);
                    //if (Array.IndexOf(seenCells, tmpCell.position) != -1 && (tmpCell.state == AIGrid.GridStates.walkable || tmpCell.state == AIGrid.GridStates.stairs))
                    if (NewUnity.ContainsV3(seenCells,tmpCell.position) && (tmpCell.state == AIGrid.GridStates.walkable || tmpCell.state == AIGrid.GridStates.stairs))
                    {

                        CalculatedPathData pathData = null;
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                pathData = AIPathFindingCore.CalculatePathCore(tmpCell.position, transform.position, 0, lockedPositions);
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

            if (pathDataCore != null)
            {
                pathTo = pathDataCore.goal;
                pathCellPositions = pathDataCore.pathCellPositions;
            }

            else
            {
                // Idk, maybe explode
            }





                
        }
    }


    /// <summary>
    /// Attempts to fetch a needed item if it has been seen before continuing or going around if faster
    /// </summary>
    /// <param name="fetchingItemType"></param>
    protected virtual void ItemNeeded(ItemData.ItemTypes fetchingItemType)
    {
        Debug.Log("Running ItemNeeded");
        ItemData shortestKey = null;
        float shortestKeyCost = float.MaxValue;

        CalculatedPathData pathData1 = null;

        if (pathCellPositionsBuffer.Count == 0) 
        {

            foreach (ItemData item in seenItems)
            {
                if (item.type == fetchingItemType)
                {
                    CalculatedPathData pathData = null;
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            pathData = AIPathFindingCore.CalculatePathCore(item.position, transform.position, 0);
                        }
                        catch { }
                        if (pathData != null && !pathData.failure) break;
                    }
                    if (pathData != null && pathData.pathFound)
                    {
                        if ((shortestKey == null || shortestKeyCost < pathData.cost))
                        {
                            shortestKey = item;
                            pathData1 = pathData;
                        }
                    }

                }
            }

            CalculatedPathData pathData2 = null;
            if (shortestKey != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        pathData2 = AIPathFindingCore.CalculatePathCore(pathTo, transform.position, 0);
                    }
                    catch { }
                    if (pathData2 != null && !pathData2.failure) break;
                }
            }
            CalculatedPathData pathData3 = null;

            List<Vector3> invalidPoints = new List<Vector3>
        {
            itemInFront.position
        };

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    pathData3 = AIPathFindingCore.CalculatePathCore(pathTo, transform.position, 0, invalidPoints);
                }
                catch { }
                if (pathData3 != null && !pathData3.failure) break;
            }

            List<AIGridCell> gridPathData = new List<AIGridCell>();
            List<ListBuffer> listBuffer = new List<ListBuffer>();

            if (pathData3 == null || !pathData3.pathFound)
            {
                currentTask = AIGoals.wander;
                return;
            }
            else if (pathData2 != null && pathData2.cost + shortestKeyCost < pathData3.cost)
            {
                ListBuffer listBuffer1 = new ListBuffer();
                listBuffer1.aiGridCellB = pathData1.pathCells;
                listBuffer.Add(listBuffer1);
                listBuffer1 = new ListBuffer();
                listBuffer1.aiGridCellB = pathData2.pathCells;
                listBuffer.Add(listBuffer1);
            }
            else
            {
                ListBuffer listBuffer1 = new ListBuffer();
                listBuffer1.aiGridCellB = pathData3.pathCells;
                listBuffer.Add(listBuffer1);
            }


            foreach (ListBuffer buffer in listBuffer)
            {
                List<Vector3> tmpList2 = new List<Vector3>();
                ListBuffer listBuffer3 = new ListBuffer();
                foreach (AIGridCell cell in buffer.aiGridCellB)
                {
                    tmpList2.Add(cell.position);
                }
                listBuffer3.vector3B = tmpList2;
                listBuffer.Add(listBuffer3);

            }
            currentTask = AIGoals.fetch;
            pathCellPositionsBuffer.Clear();
            pathCellPositionsBuffer = listBuffer;
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
            if (itemInFront.type == ItemData.ItemTypes.key)
            {
                InteractItem(true);
            }
            else if (itemInFront.type == ItemData.ItemTypes.door)
            {
                if (inventory.Contains(ItemData.ItemTypes.key))
                {
                    lockedPositions.Remove(itemInFront.position);
                    InteractItem(false, AIGoals.fetch, ItemData.ItemTypes.key);
                }
                else
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
        inventory.Add(itemInFront.type);
        Destroy(itemInFront.gameObject);
        //itemInFront = null;
        currentTask = task;
        if (useItem != ItemData.ItemTypes.nothing)
        {
            inventory.Remove(useItem);
        }
        return;
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
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

                    int facingAdjustmentX = 1;
                    if (transform.forward.x - transform.position.x < 0) facingAdjustmentX = -1;
                    int facingAdjustmentZ = 1;
                    //if (transform.forward.z - transform.position.z < 0) facingAdjustmentZ = -1;

                    float degree = transform.rotation.y * (Mathf.PI / 180f);
                    //Debug.Log(transform.rotation.y);


                    float triAngle = Mathf.Atan(scaledViewDistance.x / scaledViewDistance.y);

                    //Debug.Log(scaledViewDistance);

                    for (int adjacent = 1; adjacent < scaledViewDistance.y; adjacent += 1)
                    {

                        int opposite = Mathf.CeilToInt(Mathf.Tan(triAngle) * adjacent);
                        for (int oppositeX = -opposite; oppositeX <= opposite; oppositeX += 1)
                        {
                            //Debug.Log(oppositeX);
                            Vector3 pos = new Vector3(Mathf.FloorToInt(transform.position.x + adjacent * Mathf.Sin(transform.rotation.y)), characterY, Mathf.FloorToInt(transform.position.z + oppositeX * Mathf.Cos(transform.rotation.y)));
                            //Vector3 pos = new Vector3(Mathf.FloorToInt(transform.position.x + (((oppositeX) * Mathf.Cos(-transform.rotation.y)) + ((adjacent) * Mathf.Sin(-transform.rotation.y)))), characterY, Mathf.FloorToInt(transform.position.z + ((adjacent) * Mathf.Cos(-transform.rotation.y)) - ((oppositeX) * Mathf.Sin(-transform.rotation.y))));
                            //Vector3 pos = new Vector3(Mathf.FloorToInt((transform.position.x) + (facingAdjustmentX*((oppositeX) * Mathf.Cos(transform.rotation.y)) + ((adjacent) * Mathf.Sin(transform.rotation.y)))), transform.position.y, Mathf.FloorToInt((transform.position.z) + (facingAdjustmentZ*((adjacent) * Mathf.Cos(transform.rotation.y)) - ((oppositeX) * Mathf.Sin(transform.rotation.y)))));
                            //Vector3 pos = Quaternion.AngleAxis(transform.rotation.y, transform.position) * (new Vector3(adjacent, 0, oppositeX) + transform.position); 
                            //Vector3 pos = (RotatePointAroundPivot((new Vector3(Mathf.FloorToInt(adjacent + transform.position.x), characterY, Mathf.FloorToInt(oppositeX + transform.position.z))), transform.position, new Vector3(0, transform.rotation.y,0)));
                            //Debug.Log(pos);
                            //Debug.Log(seenCells.Contains(pos));
                            //if (!checkPositions.Contains(pos) && (Array.IndexOf(seenCells, pos) == -1 || seenItemPositions.Contains(pos))) checkPositions.Add(pos);
                            if ((!NewUnity.ContainsV3(checkPositions, pos) && (!NewUnity.ContainsV3(seenCells, pos) || NewUnity.ContainsV3(seenItemPositions, pos))))
                            {
                                checkPositions.Add(pos);
                            }
                            else if (adjacent == 1)
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

                /*
                Vector3 newPos = new Vector3(Mathf.FloorToInt(transform.forward.x), Mathf.FloorToInt(transform.forward.y), Mathf.FloorToInt(transform.forward.z));

                if (!NewUnity.ContainsV3(checkPositions, newPos))
                {
                    List<Vector3> tmpList = (new List<Vector3> { newPos });
                    tmpList.AddRange(checkPositions);
                    checkPositions = tmpList;
                }
                */

                bool itemFound = false;

                for (int i = 0; i < checkPositions.Count; i++)
                {


                    if (!NewUnity.ContainsV3(AIGrid.instance.unwalkableGrid, checkPositions[i]))
                    {
                        foreach (LayerMask layer in viewableLayers)
                        {
                            RaycastHit hit2;
                            bool hitDetction2 = Physics.BoxCast(checkPositions[i], AIGrid.instance.scaledCellSize/2, checkPositions[i], out hit2, Quaternion.identity, Mathf.Infinity, layer);
                            NewUnity.DrawBoxCastBox(checkPositions[i], AIGrid.instance.scaledCellSize/2, Quaternion.identity, checkPositions[i], Mathf.Infinity, Color.white);
                            //if (i==0) Debug.Log($"{checkPositions[i]} hitDetction2 {hitDetction2} {(int)(Mathf.Log(layer.value, 2))} {LayerMask.LayerToName((int)(Mathf.Log(layer.value, 2)))}");
                            //Debug.Log(checkPositions[0]);
                            if (hitDetction2)
                            {
                                //Debug.Log("Hit Detected: " + hit2.collider.gameObject.name);
                                ItemData itemData = hit2.collider.GetComponent<ItemData>(); 
                                if (itemData != null)
                                {
                                    if (!seenItems.Contains(itemData))
                                    {
                                        itemData.position = checkPositions[i];
                                        seenItems.Add(itemData);
                                        seenItemPositions.Add(itemData.position);
                                        lockedPositions.Add(itemData.position);
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
                                    if (seenItems[j].position == checkPositions[i]) tmpItems.Remove(seenItems[j]);
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
                //if (currentLookingAt.Count == 0) 
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

        if (AIGrid.instance != null)
        {
            if (AIGrid.instance.showVisualisations)
            {
                for (int i = 0; i < currentLookingAt.Count; i += 1)
                {
                    if (i > 0) Gizmos.color = new Color(1, 0, 1);
                    else Gizmos.color = new Color(0, 0, 0);
                    Gizmos.DrawCube(currentLookingAt[i], AIGrid.instance.scaledCellSize);
                }
            }

        }

    }
    



    protected override void FollowPath()
    {
        if (pathCellPositions.Count < 1) 
        { 
            if (pathCellPositionsBuffer.Count > 0)
            {
                pathCellPositions = pathCellPositionsBuffer[0].vector3B;
                pathCellPositionsBuffer.RemoveAt(0);
            }
        }
        try
        {
            clearCalculatedPathVisualisation.Invoke();
        }
        catch { }
        try
        {
            foreach (Vector3 pos1 in pathCellPositions)
            {
                VisualisationSetter.instance.SpawnVisualisation(pos1, AIGrid.instance.scaledCellSize, VisualisationSetter.VisualisationStates.calculatedPath, gameObject);
            }
        }
        catch { }

        base.FollowPath();
    }

    protected override void ColliderJump(Collision collision)
    {
        
        if (!NewUnity.ContainsLM(viewableLayers, collision.gameObject.layer)) // Prevents character from climibing doors and other items
        {
            Debug.Log(collision.gameObject.layer);
            base.ColliderJump(collision); 
        }
    }

    protected override IEnumerator CalculatePath() // Idk why but this is refusing to return a properly working value, breaking everything
    {
        if (!awaitCalculation)
        {
            if (pathTo == Vector3.zero) SetPathTo();
            awaitCalculation = true;
            pathCalculated = true;
            CalculatedPathData data = new CalculatedPathData();
            //Debug.Log("Start Calculation");



            for (int i = 0; i < 15; i++)
            {
                try
                {
                    data = AIPathFindingCore.CalculatePathCore(pathTo, transform.position, 0);
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

