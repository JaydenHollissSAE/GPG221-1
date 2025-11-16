using System.Collections;
using UnityEngine;

public class ActiveToggle : MonoBehaviour
{
    public static ActiveToggle instance;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void ToggleActive(GameObject item, bool state)
    {
        item.SetActive(state);
        if (!state) StartCoroutine(ReactiveTimer(item, Random.Range(15f, 200f)));
    }

    public IEnumerator ReactiveTimer(GameObject item, float time)
    {
        yield return new WaitForSeconds(time);
        ToggleActive(item, true);
    }

}
