using TMPro;
using UnityEngine;
using System.Linq;

public class PauseMenuManager : MonoBehaviour
{
    // Singleton Instance
    private static PauseMenuManager _instance;
    public static PauseMenuManager Instance // Global eri�im noktas�
    {
        get
        {
            // E�er instance yoksa bulmaya �al��
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PauseMenuManager>();

                // Hala yoksa hata ver (sahneye eklenmemi� demektir)
                if (_instance == null)
                {
                    Debug.LogError("PauseMenuManager instance sahnede bulunamad�!");
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
    private GameObject currentActivePanel; // SerializeField olmas�na gerek yok

    [Header("SystemInfos")]
    [SerializeField] private TextMeshProUGUI displayAdapterText;
    [SerializeField] private TextMeshProUGUI monitorText;

    // MEN� DURUMU ARTIK PUBLIC
    public bool isMenuOpen = false;

    void Awake()
    {
        // Singleton kurulumu: Sadece bir instance olmas�na izin ver
        if (_instance != null && _instance != this)
        {
            // Zaten bir instance varsa bu yenisini yok et
            Destroy(this.gameObject);
        }
        else
        {
            // Bu ilk instance ise onu _instance olarak ayarla
            _instance = this;
            // �ste�e ba�l�: Sahneler aras� ge�i�te yok olmamas� i�in
            // DontDestroyOnLoad(gameObject);
        }
    }


    void Start()
    {
        // Ba�lang��ta t�m men� panellerinin kapal� oldu�undan emin olal�m
        UiBackground.SetActive(false);
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(false);
        quitConfirmationPanel.SetActive(false);

        // Ekran kart� ad�
        string gpuName = SystemInfo.graphicsDeviceName;
        if (displayAdapterText != null) displayAdapterText.text = gpuName;

        // Monit�r bilgisi (��z�n�rl�k + tazeleme oran�)
        Resolution res = Screen.currentResolution;
        // RefreshRateRatio kullanmak daha do�ru, eski refreshRate int
        double hz = res.refreshRateRatio.value;

        string monitorInfo = $"{res.width}x{res.height} @ {(int)hz}Hz";� // �rnek ��kt�: 1920x1080 @ 144Hz, int'e �evirince k�surat� atar�z
        if (monitorText != null) monitorText.text = monitorInfo;
    }

    void Update()
    {
        // Sadece yerel oyuncu ESC men�s�n� a�abilmeli.
        // Bu script sahnede her zaman oldu�u i�in IsMine kontrol�n� burada yapamay�z.
        // ESC tu�una bas�ld���nda men�y� a�/kapa logic'i her zaman �al��mal�,
        // ancak karakterin hareketinin durdurulmas� FPSPlayerController i�inde IsMine kontrol� ile yap�lacak.
        // E�er men� a�ma/kapama sadece yerel oyuncu i�in olmal�ysa, bu Update metodunun
        // FPSPlayerController'daki IsMine blo�una ta��nmas� daha mant�kl� olur.
        // Ama men� y�netimi UI scriptinde kalabilir, sadece ToggleMainEscMenu metodu �a�r�l�r.

        // ESC tu�una bas�ld���nda men� navigasyonu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (quitConfirmationPanel.activeSelf) // �nce onay paneli a��k m�?
            {
                CancelQuit(); // Onay panelini kapat
            }
            else if (optionsMenuPanel.activeSelf) // Se�enekler paneli a��k m�?
            {
                HideOptionsPanelAndShowMainEsc(); // Se�enekleri kapat, ana men�ye d�n
            }
            else // Hi�biri a��k de�ilse ana men�y� a�/kapa
            {
                // ESC tu�una bas�ld���nda ana men�y� sadece yerel oyuncu a�mal�/kapamal�.
                // Bu kontrol�n FPSPlayerController'da yap�l�p buradaki ToggleMainEscMenu() metodunun �a�r�lmas� daha temiz olurdu.
                // Ancak mevcut kod yap�na uyum sa�lamak i�in buraya bir �rnek ekleyelim,
                // fakat bu scriptin sahnede sadece 1 tane oldu�undan ve
                // ESC bas�ld���nda sadece o bilgisayarda �al��t���ndan emin olunmal�.
                // Multiplayer'da bu, her oyuncunun kendi bilgisayar�nda kendi men�s�n� a�mas� i�in do�ru yerdir.
                ToggleMainEscMenu();
            }
        }
    }

    public void ToggleMainEscMenu()
    {
        // isMenuOpen de�i�kenini de�i�tir
        isMenuOpen = !isMenuOpen;

        // Panelleri durumuna g�re aktif/deaktif et
        mainEscPanel.SetActive(isMenuOpen);
        UiBackground.SetActive(isMenuOpen);

        // Fare imlecini y�net
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None; // �mleci serbest b�rak
            Cursor.visible = true; // �mleci g�r�n�r yap
            Debug.Log("ESC Men� A��ld�");
            // Time.timeScale'i 0 yapmak genellikle singleplayer oyunlarda yap�l�r.
            // Multiplayer'da timeScale'i de�i�tirmek genellikle sorun yarat�r.
            // E�er multiplayer'da duraklatma (pause) yapmak istiyorsan�z,
            // bu durumu a� �zerinden senkronize etmeniz ve oyuncu inputunu/mant���n�
            // duraklatma durumuna g�re y�netmeniz gerekir.
            // �imdilik Time.timeScale dokunmuyoruz.
        }
        else
        {
            // Men� kapand���nda imleci tekrar kilitle (FPS oyunlar� i�in)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("ESC Men� Kapand�");
            // Time.timeScale = 1f; // E�er Time.timeScale 0 yap�ld�ysa geri al
        }
    }

    // --- MainEscPanel Buton Fonksiyonlar� ---
    public void ResumeGame()
    {
        // Oyunu devam ettirme logic'i (men�y� kapat�r)
        isMenuOpen = false; // Durumu g�ncelle
        UiBackground.SetActive(isMenuOpen);
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(false);������ // Her ihtimale kar�� di�er panelleri de kapat
        quitConfirmationPanel.SetActive(false);

        // �mleci tekrar kilitle (FPS oyunlar� i�in)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // E�er timeScale de�i�tirildiyse geri al
        // Time.timeScale = 1f;

        Debug.Log("Oyun Devam Ediyor");
    }

    public void ShowOptionsPanel()
    {
        mainEscPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
        ShowPanel(displayPanel); // Se�enekler a��ld���nda Display paneli a��k olsun
        //Debug.Log("Se�enekler A��ld�");
    }

    public void ShowQuitConfirmationPanel()
    {
        quitConfirmationPanel.SetActive(true);
        // �ste�e ba�l�: Ana men�y� arkada b�rakabilir veya kapatabilirsiniz.
        // mainEscPanel.SetActive(false);
        Debug.Log("��k�� Onay� A��ld�");
    }


    // --- OptionsMenuPanel Tab Fonksiyonlar� ---
    public void OnDisplayButton() => ShowPanel(displayPanel);
    public void OnGraphicsButton() => ShowPanel(graphicsPanel);
    public void OnAudioButton() => ShowPanel(audioPanel);
    public void OnControlsButton() => ShowPanel(controlsPanel);

    // Genel panel g�sterme fonksiyonu (tekrar� �nler)
    public void ShowPanel(GameObject panelToShow)
    {
        // Aktif paneli kapat
        if (currentActivePanel != null)
            currentActivePanel.SetActive(false);

        // Yeni paneli a�
        panelToShow.SetActive(true);
        currentActivePanel = panelToShow; // Aktif paneli g�ncelle
        //Debug.Log($"Panel De�i�ti: {panelToShow.name}");
    }

    // --- OptionsMenuPanel Geri Fonksiyonu ---
    public void HideOptionsPanelAndShowMainEsc()
    {
        optionsMenuPanel.SetActive(false); // Se�enekleri kapat
        // currentActivePanel = null; // �ste�e ba�l�: Se�eneklerden ��k�nca aktif panel referans�n� temizle
        if (mainEscPanel != null)
        {
            mainEscPanel.SetActive(true); // Ana men�y� g�ster
            Debug.Log("Se�eneklerden Geri D�n�ld�");
        }
        else
        {
            Debug.Log("Anamen�ye D�n�ld�");
        }
        // isMenuOpen durumu zaten true olmal� bu noktada
    }


    // --- QuitConfirmationPanel Buton Fonksiyonlar� ---
    public void ConfirmQuitGame()
    {
        Debug.Log("Oyundan ��k�l�yor...");

        // �NEML�: E�er online bir oyunsa, �nce sunucudan d�zg�n bir �ekilde ayr�l�n!
        // PhotonNetwork.LeaveRoom(); // veya uygun Photon metodu

        Application.Quit(); // Uygulamay� kapat�r

        // Edit�rde �al���rken oyunu durdurmak i�in (build al�nca bu k�s�m �al��maz)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CancelQuit()
    {
        quitConfirmationPanel.SetActive(false); // ��k�� onay panelini kapat
        // �ste�e ba�l�: Ana men�y� gizlediyseniz tekrar g�sterin
        // mainEscPanel.SetActive(true);
        Debug.Log("��k�� �ptal Edildi");
        // isMenuOpen durumu zaten true olmal� bu noktada
    }
}