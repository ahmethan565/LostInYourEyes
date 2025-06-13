using UnityEngine;

public class Note : MonoBehaviour
{
    public KeyType assignedKey;
    public float speed = 400f;

    private float missThresholdY;

    public float missDetectFloat;

    void Start()
    {
        Transform column = transform.parent;
        Transform hitZone = column.Find("HitZone");

        if (hitZone != null)
        {
            float hitY = hitZone.position.y;
            missThresholdY = hitY - missDetectFloat;
        }

        else
        {
            Debug.LogWarning("HitZone not found" + column.name);
            missThresholdY = -100f;
        }
    }
    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y < missThresholdY)
        {
            Debug.Log("YOU MISSED!" + assignedKey);
            NoteSpawnerUI.Instance.AddPoints(-5);
            FeedbackUIController.Instance?.ShowFeedback(Color.red, assignedKey);
            Destroy(gameObject);
        }
    }
}