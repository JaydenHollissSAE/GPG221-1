using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AIPathFindingSmartGround : AIPathFindingGround
{
    [SerializeField] protected List<Vector3> seenCells = new List<Vector3>();
    [SerializeField] protected List<Vector3> currentLookingAt = new List<Vector3>();

    [SerializeField] protected Vector2 viewDistance = new Vector2(5, 5);
    protected Vector2 scaledViewDistance;
    protected ItemData itemInFront = null;

    [SerializeField] protected List<LayerMask> viewableLayers = new List<LayerMask>();

    [SerializeField] protected List<ItemData> seenItems = new List<ItemData>();
    [SerializeField] protected List<Vector3> seenItemPositions = new List<Vector3>();

    protected AIGoals currentGoal = AIGoals.goToGoal;
    protected ItemData.ItemTypes fetchingItem = ItemData.ItemTypes.nothing;

    protected List<ItemData.ItemTypes> inventory = new List<ItemData.ItemTypes>();


    public enum AIGoals 
    {
        goToGoal,
        wander,
        fetch
    }

    protected override void Start()
    {
        bobHead = false;
        scaledViewDistance = new Vector2(Mathf.CeilToInt(viewDistance.x), Mathf.CeilToInt(viewDistance.y));
        StartCoroutine(View());
        base.Start();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    protected override void CheckGoal()
    {
        base.CheckGoal();
    }

    protected override void Update()
    {
        
        base.Update();
    }


    protected virtual void CheckItems()
    {
        if (itemInFront == null) return;
        else
        {
            if (itemInFront.type == ItemData.ItemTypes.key)
            {
                CollectItem();
            }
            else if (itemInFront.type == ItemData.ItemTypes.door)
            {
                FetchItem(ItemData.ItemTypes.key);
            }
        }
        return;
    }

    protected virtual void FetchItem(ItemData.ItemTypes item)
    {
        currentGoal = AIGoals.fetch;
        fetchingItem = item;
    }
    protected virtual void CollectItem()
    {
        inventory.Add(itemInFront.type);
        Destroy(itemInFront.gameObject);
        itemInFront = null;
        currentGoal = AIGoals.goToGoal;
    }


    protected virtual IEnumerator View()
    {
        while (true)
        {

            List<Vector3> checkPositions = new List<Vector3>();

            float angle = Mathf.Atan(scaledViewDistance.x / scaledViewDistance.y);

            for (int adjacent = 1; adjacent < scaledViewDistance.y; adjacent += 1)
            {
                int opposite = Mathf.CeilToInt(Mathf.Tan(angle) * adjacent);
                for (int x = -opposite; x <= opposite; x += 1)
                {
                    Vector3 pos = new Vector3(Mathf.FloorToInt(transform.position.x + (((opposite) * Mathf.Cos(transform.rotation.y)) + ((adjacent) * Mathf.Sin(transform.rotation.y)))), characterY, Mathf.FloorToInt(transform.position.z + ((adjacent) * Mathf.Cos(transform.rotation.y)) - ((opposite) * Mathf.Sin(transform.rotation.y))));
                    if (!checkPositions.Contains(pos) && (!seenCells.Contains(pos) || seenItemPositions.Contains(pos))) checkPositions.Add(pos);
                }
            }

            for (int i = 0; i < checkPositions.Count; i++)
            {
                if (!AIGrid.instance.unwalkableGrid.Contains(checkPositions[i]))
                {
                    foreach (LayerMask layer in viewableLayers)
                    {
                        RaycastHit hit2;
                        bool hitDetction2 = Physics.BoxCast(checkPositions[i], AIGrid.instance.scaledCellSize, Vector3.zero, out hit2, Quaternion.identity, Mathf.Infinity, layer);

                        if (hitDetction2)
                        {
                            ItemData itemData = hit2.collider.GetComponent<ItemData>();
                            if (itemData != null)
                            {
                                itemData.position = checkPositions[i];
                                seenItems.Add(itemData);
                                seenItemPositions.Add(itemData.position);
                            }
                            if (i == 0)
                            {
                                itemInFront = itemData;
                            }
                        }
                        else if (seenItemPositions.Contains(checkPositions[i])) // Removes item from list if it is no longer there
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
                seenCells.Add(checkPositions[i]);
            }
            currentLookingAt.Clear();
            currentLookingAt = checkPositions;
            yield return null;
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
                    Gizmos.color = new Color(1, 0, 1);
                    Gizmos.DrawCube(currentLookingAt[i], AIGrid.instance.scaledCellSize);
                }
            }

        }

    }
    



}
