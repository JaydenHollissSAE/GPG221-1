using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualisationControl : MonoBehaviour
{
    public Renderer myRenderer;
    public bool imEnabled = true;
    public GameObject agent = null;
    public VisualisationSetter.VisualisationStates state = VisualisationSetter.VisualisationStates.nullVal;
    private bool setSubscription = false;
    int checkedTimes = 0;
    private void Start()
    {
        myRenderer = GetComponent<Renderer>();
    }


    private void OnDestroy()
    {
        DestroyMe();
    }

    public void DestroyMe()
    {
        if (VisualisationSetter.instance != null)
        {
            VisualisationSetter.instance.unwalkableVisualisations.Remove(this.gameObject);
            VisualisationSetter.instance.stairsVisualisations.Remove(this.gameObject);
            VisualisationSetter.instance.walkableVisualisations.Remove(this.gameObject);
            VisualisationSetter.instance.airVisualisations.Remove(this.gameObject);
            VisualisationSetter.instance.calculatedPathVisualisations.Remove(this.gameObject);
            VisualisationSetter.instance.jumpVisualisations.Remove(this.gameObject);
            VisualisationSetter.instance.goalVisualisations.Remove(this.gameObject);
            VisualisationSetter.instance.pathToVisualisations.Remove(this.gameObject);
        }

        if (agent != null)
        {
            AIPathFindingBase pathFinding = agent.GetComponent<AIPathFindingBase>();
            if (pathFinding != null)
            {
                switch (state)
                {
                    case VisualisationSetter.VisualisationStates.goal:
                        {
                            pathFinding.clearGoalVisualisation.RemoveListener(DestroyMe);
                            break;
                        }
                    case VisualisationSetter.VisualisationStates.pathTo:
                        {
                            pathFinding.clearPathToVisualisation.RemoveListener(DestroyMe);
                            break;
                        }
                    case VisualisationSetter.VisualisationStates.calculatedPath:
                        {
                            pathFinding.clearCalculatedPathVisualisation.RemoveListener(DestroyMe);
                            break;
                        }
                    case VisualisationSetter.VisualisationStates.jump:
                        {
                            pathFinding.clearJumpVisualisation.RemoveListener(DestroyMe);
                            break;
                        }
                    default:
                        break;

                }
            }
        }

        Destroy(this.gameObject);
    }


    public void ChangeMyRenderer(bool input)
    {
        if (myRenderer == null)
        {
            myRenderer = GetComponent<Renderer>();
        }

        if (myRenderer.enabled != input) myRenderer.enabled = input;
    }

    public void FixedUpdate()
    {
        if (!setSubscription) 
        {
            checkedTimes++;
            if (agent != null && state != VisualisationSetter.VisualisationStates.nullVal || checkedTimes > 100)
            {
                setSubscription = true;
                AIPathFindingBase pathFinding = agent.GetComponent<AIPathFindingBase>();
                if (pathFinding != null)
                {
                    switch (state)
                    {
                        case VisualisationSetter.VisualisationStates.goal:
                            {
                                pathFinding.clearGoalVisualisation.AddListener(DestroyMe);
                                break;
                            }
                        case VisualisationSetter.VisualisationStates.pathTo:
                            {
                                pathFinding.clearPathToVisualisation.AddListener(DestroyMe);
                                break;
                            }
                        case VisualisationSetter.VisualisationStates.calculatedPath:
                            {
                                pathFinding.clearCalculatedPathVisualisation.AddListener(DestroyMe);
                                break;
                            }
                        case VisualisationSetter.VisualisationStates.jump:
                            {
                                pathFinding.clearJumpVisualisation.AddListener(DestroyMe);
                                break;
                            }
                        default:
                            break;

                    }
                }
            }

        }
    }
    

    

}
