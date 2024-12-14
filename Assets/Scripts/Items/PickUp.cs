using System.Collections;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    public ItemDataBase ItemData {  get; private set; }
    [SerializeField] float rotationSpeed = 10;
    private bool isPlayerNearby = false;
    public void SetUp(ItemDataBase data)
    {
        ItemData = data;
        Instantiate(ItemData.Prefab, transform);
        gameObject.SetActive(true);
    }
    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            PickUpItem();
        }
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
    /// <summary>
    /// Picks up the item if possible
    /// </summary>
    private void PickUpItem()
    {
        if(InventoryManager.Instance.AddItem(ItemData))
        {
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// Sets the data for the pickup
    /// </summary>
    /// <param name="data"></param>
    public void SetData(ItemDataBase data)
    {
        ItemData = data;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            FloatingUIManager.Instance.ShowItemDetails(this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            FloatingUIManager.Instance.HideItemDetails();
        }
    }
}