using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingUIManager : MonoBehaviour
{
    [Header("Floating UI")]
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemDescription;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject floatingUI;

    [Header("World placement")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2, 0); 
    [SerializeField] private float distanceOffset = 1f; 
    private Transform currentItem;

    [Header("Other")]
    [SerializeField] private Camera cam;
    [SerializeField] private float interactRange = 3f;

    public static FloatingUIManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        HideItemDetails();
    }
    private void Update()
    {
        UpdateFloatingUI();
    }
    /// <summary>
    /// Updates the floating UI
    /// </summary>
    private void UpdateFloatingUI()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            PickUp item = hit.collider.GetComponent<PickUp>();
            if (item != null)
            {
                ShowItemDetails(item);
            }
            else
            {
                HideItemDetails();
            }
        }
        else
        {
            HideItemDetails();
        }
    }
    /// <summary>
    /// Activates UI and shows details
    /// </summary>
    /// <param name="item"></param>
    public void ShowItemDetails(PickUp item)
    {
        itemName.text = item.ItemData.ItemName;
        itemDescription.text = item.ItemData.ItemDescription;
        floatingUI.SetActive(true);
        currentItem = item.transform;
        Vector3 directionToPlayer = (player.position - currentItem.position).normalized;
        Vector3 newPosition = currentItem.position + offset - (directionToPlayer * distanceOffset);
        floatingUI.transform.position = newPosition;
        floatingUI.transform.rotation = Quaternion.LookRotation(floatingUI.transform.position - cam.transform.position);
    }
    /// <summary>
    /// Deactivates UI
    /// </summary>
    public void HideItemDetails()
    {
        floatingUI.SetActive(false);
    }
}