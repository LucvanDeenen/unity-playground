using UnityEngine;

public class Toggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerControllerScript;
    public GameObject inventory;
    public GameObject ui;
    private bool toggle;


    void Update()
    {
        toggle = playerControllerScript.InventoryOpen;
        if (toggle)
        {
            inventory.SetActive(true);
            ui.SetActive(false);
        } 
        else
        {
            inventory.SetActive(false);
            ui.SetActive(true);
        }
    }
}
