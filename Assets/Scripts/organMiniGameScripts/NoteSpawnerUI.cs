using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class NoteSpawnerUI : MonoBehaviourPunCallbacks
{
    public static NoteSpawnerUI Instance;

    public GameObject notePrefab;
    public Transform[] columns;

    public string[] keysTexts = { "W", "A", "S", "D", "\u2190", "\u2191", "\u2192", "\u2193" };

    public float spawnInterval;
    public bool isInvoking;

    private float points;

    public Note noteScript;

    public TMP_Text pointsText;

    // public Note Note { get => note; set => note = value; };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        noteScript = GetComponent<Note>();
        StartSpawn();
        points = 0;
    }

    public void AddPoints(float amount)
    {
        points += amount;
        if (points <= 0)
        {
            points = 0;
        }
        UpdateScoreUI();
        Debug.Log(points);

        if (points >= 400 && !PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Reached400"))
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable() { { "Reached400", true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        StartCoroutine(UpdateSpawnInterval());
    }

    IEnumerator UpdateSpawnInterval()
    {

        if (points == 50)
        {
            spawnInterval = 10f;
            DestroyAllWithTag();
            RestartSpawn();
            yield return new WaitForSeconds(10);
            spawnInterval = 0.8f;
            RestartSpawn();
        }
        else if (points == 100)
        {
            spawnInterval = 10f;
            DestroyAllWithTag();
            RestartSpawn();
            yield return new WaitForSeconds(10);
            spawnInterval = 0.7f;
            RestartSpawn();
        }

        else if (points == 200)
        {
            spawnInterval = 10f;
            DestroyAllWithTag();
            RestartSpawn();
            yield return new WaitForSeconds(10);
            spawnInterval = 0.6f;
            RestartSpawn();
        }
    }

    void UpdateScoreUI()
    {
        if (pointsText != null)
        {
            pointsText.text = "Score: " + points;
        }
    }

    void SpawnNote()
    {
        int columnIndex = Random.Range(0, columns.Length);
        int keyIndex = Random.Range(0, keysTexts.Length);

        GameObject newNote = Instantiate(notePrefab, columns[columnIndex]);

        newNote.GetComponentInChildren<TMP_Text>().text = keysTexts[keyIndex];
        newNote.GetComponent<Note>().assignedKey = (KeyType)keyIndex;

        newNote.transform.localPosition = new Vector3(0, 400f, 0);
    }

    void StartSpawn()
    {
        InvokeRepeating(nameof(SpawnNote), spawnInterval, spawnInterval);

        isInvoking = true;
    }
    void RestartSpawn()
    {
        if (isInvoking)
        {
            CancelInvoke(nameof(SpawnNote));
        }

        InvokeRepeating(nameof(SpawnNote), spawnInterval, spawnInterval);

        isInvoking = true;
    }

    void DestroyAllWithTag()
    {
        GameObject[] objectsToDestroy = GameObject.FindGameObjectsWithTag("Note");

        foreach (GameObject obj in objectsToDestroy)
        {
            Destroy(obj);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Reached400"))
        {
            CheckIfBothPlayersReached400();
        }
    }

    private void CheckIfBothPlayersReached400()
    {
        bool allReached = true;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue("Reached400", out object value) || !(bool)value)
            {
                allReached = false;
                break;
            }
        }

        if (allReached)
        {
            photonView.RPC("PuzzleSolved", RpcTarget.All);
        }
    }

    void PuzzleSolved()
    {
        Debug.Log("Both two players reached 400 points. pzulle solved.");
    }
}
