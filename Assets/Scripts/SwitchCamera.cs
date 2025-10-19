using UnityEngine;
using System.Collections.Generic;

public class SwitchCamera : MonoBehaviour
{
    public static SwitchCamera instance = null;

    public List<Camera> cameras = new List<Camera>();

    public bool isFreecam = false;

    public Camera freeCam;

    public bool isAllCameras = false;



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
        else if (cameras.Count != 1)
        {
            Debug.Log(Camera.main.rect);
            int currentIndex = 0;
            if (isFreecam)
            {
                isFreecam = false;
                freeCam.enabled = false;
                currentIndex = 0;
            }
            else
            {
                if (isAllCameras) DisableAllCamerasMode();

                try
                {
                    currentIndex = cameras.IndexOf(Camera.main);
                } catch { currentIndex = 0; }
                try { Camera.main.enabled = false; } catch { }
                //cameras[currentIndex].enabled = false;
                currentIndex += switchBy;
            }
            while (currentIndex >= cameras.Count)
            {
                currentIndex = currentIndex - (cameras.Count);
            }
            while (currentIndex < 0)
            {
                currentIndex = (cameras.Count - 1) + currentIndex;
            }
            cameras[currentIndex].enabled = true;
        }
    }


    public void EnableFreeCam()
    {
        if (!isFreecam)
        {
            if (isAllCameras) DisableAllCamerasMode();

            isFreecam = true;
            try { Camera.main.enabled = false; } catch { }
            freeCam.enabled = true;
        }
    }


    public void DisableAllCamerasMode()
    {
        foreach (Camera cam in cameras)
        {

            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.enabled = false;
            
        }
        isAllCameras = false;
        return;
    }

    public void EnableAllCameras()
    {
        if (!isAllCameras)
        {
            isAllCameras = true;

            if (isFreecam)
            {
                isFreecam = false;
                freeCam.enabled = false;
            }

            float root = Mathf.Sqrt(cameras.Count);
            Debug.Log(root);
            for (int i = 0; i < root; i++)
            {
                for (int j = 0; j < root; j++)
                {
                    Camera cam = cameras[((int)root*i)+j];
                    float x;
                    float y;
                    x = (i) / root;
                    y = (j) / root;

                    cam.rect = new Rect(x, y, (1f / root), (1f / root));
                    cam.enabled = true;
                }
            }


        }
    }




}
