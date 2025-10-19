using UnityEngine;

public class AddCamera : MonoBehaviour
{

    bool addedCam = false;

    private void FixedUpdate()
    {
        if (!addedCam)
        {
            if (SwitchCamera.instance != null)
            {
                addedCam = true;
                SwitchCamera.instance.cameras.Add(GetComponent<Camera>());
            }

        }
    }

    private void OnDestroy()
    {
        if (SwitchCamera.instance != null)
        {
            SwitchCamera.instance.cameras.Remove(GetComponent<Camera>());
            SwitchCamera.instance.Switch(1);
        }

    }

}
