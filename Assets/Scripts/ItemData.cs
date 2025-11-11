using UnityEngine;

public class ItemData : MonoBehaviour
{
    public enum ItemTypes
    {
        nothing,
        key,
        door
    }

    public ItemTypes type;

    public Vector3 position;

}
