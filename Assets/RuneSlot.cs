using UnityEngine;
using Photon.Pun;

public class RuneSlot : MonoBehaviourPun, IInteractable
{
    public string requiredRuneID;
    public Transform runePlacementPoint; // Rün’ün yerleştirileceği pozisyon
    public bool isCompleted { get; private set; } = false;

    public RunePuzzleController puzzleController;

    public void Interact()
    {
        Debug.Log("Bu rün slotuna bir rün yerleştirilebilir.");
        if (puzzleController != null)
            puzzleController.NotifyRunePlaced();
    }

    public void InteractWithItem(GameObject heldItemGO)
    {
        if (isCompleted || heldItemGO == null) return;

        ItemPickup pickup = heldItemGO.GetComponent<ItemPickup>();
        if (pickup == null || pickup.itemID != requiredRuneID) return;

        isCompleted = true;

        // Elimizdeki item’ı envanterden bırak ve konumlandır
        InventorySystem.Instance.ForceDropItem(); // Yere düşürmeden bırakmak için özel fonksiyon

        // Sabitle: pozisyon + rotasyon + parent
        heldItemGO.transform.SetParent(null); // Önce parent'tan ayır
        heldItemGO.transform.position = runePlacementPoint.position;
        heldItemGO.transform.rotation = runePlacementPoint.rotation;
        heldItemGO.transform.localScale = Vector3.one; // Orijinal scale'e sıfırla
        heldItemGO.transform.SetParent(runePlacementPoint, true); // worldPositionStays = true


        // İsteğe bağlı: collider kapat, rigidbody varsa kinematik yap
        Collider col = heldItemGO.GetComponent<Collider>();
        if (col) col.enabled = false;

        Rigidbody rb = heldItemGO.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Puzzle sistemine bildir
        if (puzzleController != null)
            puzzleController.NotifyRunePlaced();
    }

    public string GetInteractText()
    {
        return isCompleted ? "Rune placed" : $"Place {requiredRuneID}";
    }
}
