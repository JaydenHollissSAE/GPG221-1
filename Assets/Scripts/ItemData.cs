using System.Collections.Generic;
using UnityEngine;

public class ItemData : MonoBehaviour
{
    /// <summary>
    /// Different types of items
    /// </summary>
    public enum ItemTypes
    {
        nothing,
        key,
        door
    }

    public ItemTypes type;

    public List<Vector3> position = new List<Vector3>(); // List to account for items larger than 1 cell

    void Start() // Sets the position in the position variable, accounting for scaling and rotation
    {
        Vector3 scaledScale = new Vector3(Mathf.CeilToInt(transform.lossyScale.x), Mathf.CeilToInt(transform.lossyScale.y), Mathf.CeilToInt(transform.lossyScale.z));

        for (int y = -(int)scaledScale.y/2; y <= scaledScale.y / 2; y+=1)
        {

            for (int x = -(int)scaledScale.x / 2; x <= scaledScale.x / 2; x += 1)
            {

                for (int z = -(int)scaledScale.z / 2; z <= scaledScale.z / 2; z += 1)
                {

                    Vector3 pos = new Vector3(x, y, z);
                    pos.x = Mathf.FloorToInt(pos.x * Mathf.Sin(transform.rotation.y));
                    pos.z = Mathf.FloorToInt(pos.z * Mathf.Cos(transform.rotation.y));
                    pos.x = Mathf.FloorToInt(pos.x * Mathf.Sin(transform.rotation.x));
                    pos.z = Mathf.FloorToInt(pos.z * Mathf.Cos(transform.rotation.x));
                    pos.x = Mathf.FloorToInt(pos.x * Mathf.Sin(transform.rotation.z));
                    pos.z = Mathf.FloorToInt(pos.z * Mathf.Cos(transform.rotation.z));
                    pos.x = Mathf.FloorToInt(pos.x + transform.position.x);
                    pos.y = Mathf.FloorToInt(pos.y + transform.position.y);
                    pos.z = Mathf.FloorToInt(pos.z + transform.position.z);
                    position.Add(pos);

                }
            }
        }
    }

}
