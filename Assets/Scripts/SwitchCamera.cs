using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class SwitchCamera : MonoBehaviour
{
    public static SwitchCamera instance = null;

    public List<Camera> cameras = new List<Camera>();

    public bool isFreecam = false;

    public Camera freeCam;


    private void Awake()
    {
        instance = this;
    }


    public void Switch(int switchBy = 0)
    {
        if (cameras.Count == 0)
        {
            EnableFreeCam();
        }
        else
        {
            int currentIndex;
            if (isFreecam)
            {
                isFreecam = false;
                freeCam.enabled = false;
                currentIndex = 0;
            }
            else
            {
                try
                {
                    currentIndex = cameras.IndexOf(Camera.main);
                } catch { currentIndex = 0; }
                Camera.main.enabled = false;
                //cameras[currentIndex].enabled = false;
            }
            currentIndex += switchBy;
            while (currentIndex > cameras.Count - 1)
            {
                currentIndex = cameras.Count - 1 - currentIndex;
            }
            while (currentIndex < 0)
            {
                currentIndex = cameras.Count - 1 + currentIndex;
            }
            cameras[currentIndex].enabled = true;
        }
    }


    public void EnableFreeCam()
    {
        if (!isFreecam)
        {
            isFreecam = true;
            Camera.main.enabled = false;
            freeCam.enabled = true;
        }
    }





}
