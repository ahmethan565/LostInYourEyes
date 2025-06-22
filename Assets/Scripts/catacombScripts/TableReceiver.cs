using System.Diagnostics;
using UnityEngine;

public class TableReceiver : MonoBehaviour
{
    public static TableReceiver Instance;

    public MeshRenderer tableRenderer;

    void Awake()
    {
        Instance = this;
    }

    public void ShowSelectedTable(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        catacombPuzzleChecker.Instance.SetCorrectSymbols(data.symbolTextures);
    }        
}
