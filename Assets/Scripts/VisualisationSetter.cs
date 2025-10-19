using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class VisualisationSetter : MonoBehaviour
{
    public GameObject prefab;
    public Material baseMaterial;
    public static VisualisationSetter instance;
    public List<GameObject> unwalkableVisualisations = new List<GameObject>();
    public List<GameObject> stairsVisualisations = new List<GameObject>();
    public List<GameObject> walkableVisualisations = new List<GameObject>();
    public List<GameObject> airVisualisations = new List<GameObject>();
    public List<GameObject> calculatedPathVisualisations = new List<GameObject>();
    public List<GameObject> jumpVisualisations = new List<GameObject>();
    public List<GameObject> goalVisualisations = new List<GameObject>();
    public List<GameObject> pathToVisualisations = new List<GameObject>();
    public bool updateVisuals = true;
    private bool spawningQueueActive = false;
    public List<VisualisationSpawnData> spawningQueue = new List<VisualisationSpawnData>();


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }


    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }


    private void FixedUpdate()
    {
        if (updateVisuals) ToggleVisualisations();
    }


    public void ToggleVisualisations()
    {
        updateVisuals = false;
        foreach (GameObject controlObj in unwalkableVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationUnwalkable);
                else control.ChangeMyRenderer(false);
            } catch {}
        }

        foreach (GameObject controlObj in stairsVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationStairs);
                else control.ChangeMyRenderer(false);
            } catch {}
        }

        foreach (GameObject controlObj in walkableVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationWalkable);
                else control.ChangeMyRenderer(false);
            } catch {}
        }

        foreach (GameObject controlObj in airVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationAir);
                else control.ChangeMyRenderer(false);
            } catch {}
        }

        foreach (GameObject controlObj in calculatedPathVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationCalculatedPath);
                else control.ChangeMyRenderer(false);
            } catch {}
        }

        foreach (GameObject controlObj in jumpVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationJump);
                else control.ChangeMyRenderer(false);
            } catch {}
        }

        foreach (GameObject controlObj in goalVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationGoal);
                else control.ChangeMyRenderer(false);
            } catch {}
        }

        foreach (GameObject controlObj in pathToVisualisations)
        {
            try 
            {
                VisualisationControl control = controlObj.GetComponent<VisualisationControl>();
                if (AIGrid.instance.showVisualisations && control.imEnabled) control.ChangeMyRenderer(AIGrid.instance.showVisualisationPathTo);
                else control.ChangeMyRenderer(false);
            } catch {}
        }
       
    }



    public Material NewMaterial(UnityEngine.Color colour)
    {
        Material newMaterial = new Material(baseMaterial);
        colour.a = baseMaterial.color.a;
        newMaterial.color = colour;
        return newMaterial;
    }


    public void SpawnVisualisation(Vector3 position, Vector3 size, string state, GameObject agent = null)
    {
        VisualisationSpawnData data = new VisualisationSpawnData();
        data.position = position;
        data.size = size;
        data.state = state;
        data.agent = agent;
        spawningQueue.Add(data);
        if (!spawningQueueActive)
        {
            spawningQueueActive = true;
            StartCoroutine(SpawnQueue());
        }


        return;

    }


    IEnumerator SpawnQueue()
    {
        spawningQueueActive = true;
        int spawnCount = 0;

        while (spawningQueue.Count > 0)
        {
            GameObject spawned = Instantiate(prefab);
            spawned.transform.parent = transform;
            spawned.transform.position = spawningQueue[0].position;
            spawned.transform.localScale = spawningQueue[0].size;
            spawned.gameObject.name = spawningQueue[0].state + " Visualisation Cube";
            VisualisationControl control = spawned.GetComponent<VisualisationControl>();
            control.agent = spawningQueue[0].agent;
            control.state = spawningQueue[0].state;
            UnityEngine.Color colour = UnityEngine.Color.white;


            switch (spawningQueue[0].state)
            {
                case "walkable":
                    colour = UnityEngine.Color.green;
                    walkableVisualisations.Add(spawned);
                    break;
                case "stairs":
                    colour = UnityEngine.Color.yellow;
                    stairsVisualisations.Add(spawned);
                    break;
                case "unwalkable":
                    colour = UnityEngine.Color.red;
                    unwalkableVisualisations.Add(spawned);
                    break;
                case "air":
                    colour = UnityEngine.Color.magenta;
                    airVisualisations.Add(spawned);
                    break;
                case "calculatedPath":
                    colour = new UnityEngine.Color(1, 0.5f, 0);
                    calculatedPathVisualisations.Add(spawned);
                    break;
                case "goal":
                    colour = UnityEngine.Color.cyan;
                    goalVisualisations.Add(spawned);
                    break;
                case "pathTo":
                    colour = UnityEngine.Color.black;
                    pathToVisualisations.Add(spawned);
                    break;
                case "jump":
                    colour = new UnityEngine.Color(0.5f, 0.5f, 0.5f);
                    jumpVisualisations.Add(spawned);
                    break;
                default: //For showing errors
                    break;
            }
            spawned.GetComponent<Renderer>().material = NewMaterial(colour);
            spawningQueue.RemoveAt(0);
            spawnCount+=1;
            if (spawnCount >= 5)
            {
                spawnCount = 0;
                updateVisuals = true;
                yield return new WaitForSeconds(2f);
            }
            //yield return null;
        }
        updateVisuals = true;
        spawningQueueActive = false;
        yield return null;

    }




}

[Serializable]
public class VisualisationSpawnData
{
    public Vector3 position;
    public Vector3 size;
    public string state;
    public GameObject agent = null;
}
