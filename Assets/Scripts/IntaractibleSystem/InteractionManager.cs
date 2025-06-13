using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class InteractionManager : MonoBehaviour
{

    private PhotonView photonView;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableLayerMask;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Vector2 defaultSize = new Vector2(5, 5);
    [SerializeField] private Vector2 highlightedSize = new Vector2(10, 10);

    [Header("UI")]
    [SerializeField] private InteractionUIController interactionUI;

    private IInteractable currentInteractable;
    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }
    private void Update()
    {
        if (!photonView.IsMine) return;

        HandleRaycast();
        HandleInput();
    }

    private void HandleRaycast()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayerMask))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

            if (hit.collider.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                if (currentInteractable != interactable)
                {
                    interactionUI.Show(interactable.GetInteractText());
                }

                currentInteractable = interactable;
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(crosshairImage.rectTransform.sizeDelta, highlightedSize, Time.deltaTime * 10f);
                return;
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);
        }

        if (currentInteractable != null)
        {
            interactionUI.Hide();
        }

        currentInteractable = null;
        crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(crosshairImage.rectTransform.sizeDelta, defaultSize, Time.deltaTime * 10f);
    }

    private void HandleInput()
    {
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
        }
    }
}
