using UnityEngine;
using Photon.Pun;

public class FPSPlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravityValue = -20f;

    [Header("Kamera Ayarları")]
    public float mouseSensitivity = 100f;
    public Transform cameraRoot;
    public bool clampVerticalRotation = true;
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    [Header("Ağ Ayarları")]
    public float remoteSmoothFactor = 15f;

    private CharacterController controller;
    private Vector3 playerVelocity; // Hem input kaynaklı hem yer çekimi kaynaklı dikey hızı içerir
    private float xRotation = 0f;

    // Ağ üzerinden gelen veriler
    private float network_xRotation;
    private bool network_isGrounded;
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (!photonView.IsMine)
        {
            if (controller != null)
                controller.enabled = false;
            SetupRemotePlayerCamera();
        }
    }

    void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 20;

        if (photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SetupLocalPlayerCamera();
        }
    }

    void Update()
    {
        // Sadece yerel oyuncunun logic'ini çalıştır
        if (photonView.IsMine)
        {
            // Menü açık mı kontrol et (Singleton veya Statik değişkene göre)
            bool menuIsOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isMenuOpen;
            // Eğer Statik kullandıysanız: bool menuIsOpen = PauseMenuManager.isMenuOpen;

            // Yere değme durumu ve yer çekimi/dikey hız uygulaması her zaman çalışmalı (inputtan bağımsız)
            bool isGrounded = controller.isGrounded;
            if (isGrounded && playerVelocity.y < 0)
            {
                playerVelocity.y = -0.5f; // Yere değer değmez dikey hızı sıfırla
            }

            // Yer çekimini uygula (inputtan bağımsız)
            playerVelocity.y += gravityValue * Time.deltaTime;

            // Yatay hareket vektörünü inputtan al. Menü açıkken sıfır olacak.
            Vector3 horizontalMove = Vector3.zero;

            // Eğer menü açık değilse input'u işle
            if (!menuIsOpen)
            {
                // Yatay/Dikey inputu al
                horizontalMove = GetInputMoveVector();

                // Zıplama inputunu işle
                HandleJumpInput(isGrounded);

                // Fare ile bakışı işle
                HandleLocalPlayerMouseLook();
            }
            // else: Menü açıksa, horizontalMove sıfır kalır, zıplama inputu alınmaz, bakış işlenmez.

            // Toplam hareket vektörü: Yatay input (veya sıfır) + Dikey velocity (yer çekimi + zıplama)
            // playerVelocity.y zaten yer çekimi tarafından güncellendi
            Vector3 totalMove = horizontalMove + new Vector3(0, playerVelocity.y, 0);


            // Karakteri hareket ettir
            if (controller != null && controller.enabled) // Controller'ın aktif olduğundan emin ol
            {
                controller.Move(totalMove * Time.deltaTime);
            }


        }
        else // Remote oyuncu ise
        {
            SmoothRemotePlayerData();
        }
    }

    // Sadece yatay/dikey input vektörünü döndüren yardımcı metot
    private Vector3 GetInputMoveVector()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v);
        if (move.magnitude > 1f) move.Normalize(); // Çapraz harekette hızı aşmamak için
        return move * moveSpeed; // Hız ile çarpılmış input vektörünü döndür
    }

    // Sadece zıplama inputunu işleyen yardımcı metot
    private void HandleJumpInput(bool isGrounded)
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    // Eski HandleLocalPlayerMovement metodu artık kullanılmıyor

    // ... (Diğer metotlar aynı kalır)

    void SetupLocalPlayerCamera()
    {
        if (cameraRoot != null)
        {
            Camera cam = cameraRoot.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
                AudioListener al = cam.GetComponent<AudioListener>();
                if (al == null) al = cam.gameObject.AddComponent<AudioListener>();
                al.enabled = true;
            }
            else Debug.LogWarning("cameraRoot altında kamera bulunamadı! Yerel kamera ayarlanamadı.");
        }
        else Debug.LogWarning("cameraRoot atanmamış! Yerel kamera ayarlanamadı.");
    }

    void SetupRemotePlayerCamera()
    {
        if (cameraRoot != null)
        {
            Camera cam = cameraRoot.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.gameObject.SetActive(false);
            AudioListener al = cameraRoot.GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;
        }
        else Debug.LogWarning("cameraRoot atanmamış! Remote kamera ayarlanamadı.");
    }

    void HandleLocalPlayerMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;

        if (clampVerticalRotation)
            xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        else Debug.LogWarning("cameraRoot atanmamış! Dikey bakış yapılamadı.");
    }


    void SmoothRemotePlayerData()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * remoteSmoothFactor);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * remoteSmoothFactor);

        if (cameraRoot != null)
        {
            Quaternion currentRot = cameraRoot.localRotation;
            Quaternion targetRot = Quaternion.Euler(network_xRotation, 0f, 0f);
            cameraRoot.localRotation = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * remoteSmoothFactor);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(xRotation);
            // İsteğe bağlı: Dikey hızı da gönderebilirsiniz.
            // stream.SendNext(playerVelocity.y);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            network_xRotation = (float)stream.ReceiveNext();
            // Eğer gönderiyorsanız:
            // playerVelocity.y = (float)stream.ReceiveNext(); // Remote için playerVelocity.y'yi günceller (opsiyonel)
        }
    }
}