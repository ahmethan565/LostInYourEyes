using TMPro;
using UnityEngine;
using System.Linq;

public class PauseMenuManager : MonoBehaviour
{
    // Singleton Instance
    private static PauseMenuManager _instance;
    public static PauseMenuManager Instance // Global eriþim noktasý
    {
        get
        {
            // Eðer instance yoksa bulmaya çalýþ
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PauseMenuManager>();

                // Hala yoksa hata ver (sahneye eklenmemiþ demektir)
                if (_instance == null)
                {
                    Debug.LogError("PauseMenuManager instance sahnede bulunamadý!");
                }
            }
            return _instance;
        }
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainEscPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    [SerializeField] private GameObject UiBackground;

    [Header("Tab Panels")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;
    private GameObject currentActivePanel; // SerializeField olmasýna gerek yok

    [Header("SystemInfos")]
    [SerializeField] private TextMeshProUGUI displayAdapterText;
    [SerializeField] private TextMeshProUGUI monitorText;

    // MENÜ DURUMU ARTIK PUBLIC
    public bool isMenuOpen = false;

    void Awake()
    {
        // Singleton kurulumu: Sadece bir instance olmasýna izin ver
        if (_instance != null && _instance != this)
        {
            // Zaten bir instance varsa bu yenisini yok et
            Destroy(this.gameObject);
        }
        else
        {
            // Bu ilk instance ise onu _instance olarak ayarla
            _instance = this;
            // Ýsteðe baðlý: Sahneler arasý geçiþte yok olmamasý için
            // DontDestroyOnLoad(gameObject);
        }
    }


    void Start()
    {
        // Baþlangýçta tüm menü panellerinin kapalý olduðundan emin olalým
        UiBackground.SetActive(false);
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(false);
        quitConfirmationPanel.SetActive(false);

        // Ekran kartý adý
        string gpuName = SystemInfo.graphicsDeviceName;
        if (displayAdapterText != null) displayAdapterText.text = gpuName;

        // Monitör bilgisi (çözünürlük + tazeleme oraný)
        Resolution res = Screen.currentResolution;
        // RefreshRateRatio kullanmak daha doðru, eski refreshRate int
        double hz = res.refreshRateRatio.value;

        string monitorInfo = $"{res.width}x{res.height} @ {(int)hz}Hz";  // örnek çýktý: 1920x1080 @ 144Hz, int'e çevirince küsuratý atarýz
        if (monitorText != null) monitorText.text = monitorInfo;
    }

    void Update()
    {
        // Sadece yerel oyuncu ESC menüsünü açabilmeli.
        // Bu script sahnede her zaman olduðu için IsMine kontrolünü burada yapamayýz.
        // ESC tuþuna basýldýðýnda menüyü aç/kapa logic'i her zaman çalýþmalý,
        // ancak karakterin hareketinin durdurulmasý FPSPlayerController içinde IsMine kontrolü ile yapýlacak.
        // Eðer menü açma/kapama sadece yerel oyuncu için olmalýysa, bu Update metodunun
        // FPSPlayerController'daki IsMine bloðuna taþýnmasý daha mantýklý olur.
        // Ama menü yönetimi UI scriptinde kalabilir, sadece ToggleMainEscMenu metodu çaðrýlýr.

        // ESC tuþuna basýldýðýnda menü navigasyonu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (quitConfirmationPanel.activeSelf) // Önce onay paneli açýk mý?
            {
                CancelQuit(); // Onay panelini kapat
            }
            else if (optionsMenuPanel.activeSelf) // Seçenekler paneli açýk mý?
            {
                HideOptionsPanelAndShowMainEsc(); // Seçenekleri kapat, ana menüye dön
            }
            else // Hiçbiri açýk deðilse ana menüyü aç/kapa
            {
                // ESC tuþuna basýldýðýnda ana menüyü sadece yerel oyuncu açmalý/kapamalý.
                // Bu kontrolün FPSPlayerController'da yapýlýp buradaki ToggleMainEscMenu() metodunun çaðrýlmasý daha temiz olurdu.
                // Ancak mevcut kod yapýna uyum saðlamak için buraya bir örnek ekleyelim,
                // fakat bu scriptin sahnede sadece 1 tane olduðundan ve
                // ESC basýldýðýnda sadece o bilgisayarda çalýþtýðýndan emin olunmalý.
                // Multiplayer'da bu, her oyuncunun kendi bilgisayarýnda kendi menüsünü açmasý için doðru yerdir.
                ToggleMainEscMenu();
            }
        }
    }

    public void ToggleMainEscMenu()
    {
        // isMenuOpen deðiþkenini deðiþtir
        isMenuOpen = !isMenuOpen;

        // Panelleri durumuna göre aktif/deaktif et
        mainEscPanel.SetActive(isMenuOpen);
        UiBackground.SetActive(isMenuOpen);

        // Fare imlecini yönet
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None; // Ýmleci serbest býrak
            Cursor.visible = true; // Ýmleci görünür yap
            Debug.Log("ESC Menü Açýldý");
            // Time.timeScale'i 0 yapmak genellikle singleplayer oyunlarda yapýlýr.
            // Multiplayer'da timeScale'i deðiþtirmek genellikle sorun yaratýr.
            // Eðer multiplayer'da duraklatma (pause) yapmak istiyorsanýz,
            // bu durumu að üzerinden senkronize etmeniz ve oyuncu inputunu/mantýðýný
            // duraklatma durumuna göre yönetmeniz gerekir.
            // Þimdilik Time.timeScale dokunmuyoruz.
        }
        else
        {
            // Menü kapandýðýnda imleci tekrar kilitle (FPS oyunlarý için)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("ESC Menü Kapandý");
            // Time.timeScale = 1f; // Eðer Time.timeScale 0 yapýldýysa geri al
        }
    }

    // --- MainEscPanel Buton Fonksiyonlarý ---
    public void ResumeGame()
    {
        // Oyunu devam ettirme logic'i (menüyü kapatýr)
        isMenuOpen = false; // Durumu güncelle
        UiBackground.SetActive(isMenuOpen);
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(false);       // Her ihtimale karþý diðer panelleri de kapat
        quitConfirmationPanel.SetActive(false);

        // Ýmleci tekrar kilitle (FPS oyunlarý için)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Eðer timeScale deðiþtirildiyse geri al
        // Time.timeScale = 1f;

        Debug.Log("Oyun Devam Ediyor");
    }

    public void ShowOptionsPanel()
    {
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
        ShowPanel(displayPanel); // Seçenekler açýldýðýnda Display paneli açýk olsun
        //Debug.Log("Seçenekler Açýldý");
    }

    public void ShowQuitConfirmationPanel()
    {
        quitConfirmationPanel.SetActive(true);
        // Ýsteðe baðlý: Ana menüyü arkada býrakabilir veya kapatabilirsiniz.
        // mainEscPanel.SetActive(false);
        Debug.Log("Çýkýþ Onayý Açýldý");
    }


    // --- OptionsMenuPanel Tab Fonksiyonlarý ---
    public void OnDisplayButton() => ShowPanel(displayPanel);
    public void OnGraphicsButton() => ShowPanel(graphicsPanel);
    public void OnAudioButton() => ShowPanel(audioPanel);
    public void OnControlsButton() => ShowPanel(controlsPanel);

    // Genel panel gösterme fonksiyonu (tekrarý önler)
    public void ShowPanel(GameObject panelToShow)
    {
        // Aktif paneli kapat
        if (currentActivePanel != null)
            currentActivePanel.SetActive(false);

        // Yeni paneli aç
        panelToShow.SetActive(true);
        currentActivePanel = panelToShow; // Aktif paneli güncelle
        //Debug.Log($"Panel Deðiþti: {panelToShow.name}");
    }

    // --- OptionsMenuPanel Geri Fonksiyonu ---
    public void HideOptionsPanelAndShowMainEsc()
    {
        optionsMenuPanel.SetActive(false); // Seçenekleri kapat
        // currentActivePanel = null; // Ýsteðe baðlý: Seçeneklerden çýkýnca aktif panel referansýný temizle
        if (mainEscPanel != null)
        {
            mainEscPanel.SetActive(true); // Ana menüyü göster
            Debug.Log("Seçeneklerden Geri Dönüldü");
        }
        else
        {
            Debug.Log("Anamenüye Dönüldü");
        }
        // isMenuOpen durumu zaten true olmalý bu noktada
    }


    // --- QuitConfirmationPanel Buton Fonksiyonlarý ---
    public void ConfirmQuitGame()
    {
        Debug.Log("Oyundan çýkýlýyor...");

        // ÖNEMLÝ: Eðer online bir oyunsa, önce sunucudan düzgün bir þekilde ayrýlýn!
        // PhotonNetwork.LeaveRoom(); // veya uygun Photon metodu

        Application.Quit(); // Uygulamayý kapatýr

        // Editörde çalýþýrken oyunu durdurmak için (build alýnca bu kýsým çalýþmaz)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CancelQuit()
    {
        quitConfirmationPanel.SetActive(false); // Çýkýþ onay panelini kapat
        // Ýsteðe baðlý: Ana menüyü gizlediyseniz tekrar gösterin
        // mainEscPanel.SetActive(true);
        Debug.Log("Çýkýþ Ýptal Edildi");
        // isMenuOpen durumu zaten true olmalý bu noktada
    }
}