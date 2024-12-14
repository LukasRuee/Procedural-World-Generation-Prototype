using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool canCreateNewInstances;
    private Queue<GameObject> queue = new Queue<GameObject>();
    /// <summary>
    /// Fills queue with the set amount of gameobjects
    /// </summary>
    /// <param name="amount"></param>
    public void FillQueue(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            CreateAndEnqueueObject();
        }
    }
    /// <summary>
    /// Returns a gameobject from queue  
    /// </summary>
    public GameObject GetObject()
    {
        if(queue.Count <= 0 && canCreateNewInstances)
        {
            CreateAndEnqueueObject();
        }
        GameObject temp = queue.Dequeue();
        temp.SetActive(true);
        return temp;
    }
    /// <summary>
    /// Returns the Gameobject back to the Queue
    /// </summary>
    /// <param name="gameObject"></param>
    public void ReturnObject(GameObject gameObject)
    {
        gameObject.SetActive(false);
        queue.Enqueue(gameObject);
    }
    /// <summary>
    /// Creates a new Gameobject
    /// </summary>
    private void CreateAndEnqueueObject()
    {
        GameObject temp = Instantiate(prefab, transform);
        temp.transform.SetParent(transform);
        temp.SetActive(false);
        queue.Enqueue(temp);
    }
}
