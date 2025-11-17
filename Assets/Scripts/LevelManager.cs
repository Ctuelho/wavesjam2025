using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Dependencies")]
    public WavesManager wavesManager;
    public ObserversManager observersManager;

    [Header("Grid Settings")]
    public int GridSize = 9;
    public float WaveSize = 1f;

    [Header("Observer Setup")]
    public List<GameObject> observerPrefabs = new List<GameObject>();

    private void Start()
    {
        //InitializeLevel();
    }

    public void InitializeLevel()
    {
        if (wavesManager == null || observersManager == null)
        {
            Debug.LogError("WavesManager or ObserversManager reference is missing in LevelManager.");
            return;
        }

        wavesManager.WaveSize = WaveSize;
        wavesManager.CreateGrid(GridSize);

        observersManager.Initialize(WaveSize);
        observersManager.Observe(observerPrefabs);

        float gridFrameWidth = (GridSize + 2) * WaveSize;
        float gridFrameHeight = (GridSize + 2) * WaveSize;

        float gridMaxX = -2 * WaveSize + (gridFrameWidth / 2f);

        float observersManagerCenterX = gridMaxX + 1.0f * WaveSize + 0.5f * WaveSize;

        observersManager.transform.position = new Vector3(observersManagerCenterX, 0f, 0f);

        AdjustCamera();
    }

    private void AdjustCamera()
    {
        float gridFrameWidth = (GridSize + 2) * WaveSize;
        float totalWidth = gridFrameWidth + WaveSize + WaveSize;

        float gridVisibleHeight = (GridSize + 2) * WaveSize;
        float observersVisibleHeight = observersManager.TotalHeight;
        float totalHeight = Mathf.Max(gridVisibleHeight, observersVisibleHeight);

        wavesManager.FitCameraToBounds(totalWidth, totalHeight);
    }
}