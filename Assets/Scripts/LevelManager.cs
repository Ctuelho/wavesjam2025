using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Dependencies")]
    public WavesManager wavesManager;
    public ObserversManager observersManager;

    [Header("Grid Settings")]
    public int GridX = 10;
    public int GridY = 10;
    public float WaveSize = 1f;

    [Header("Observer Setup")]
    public List<GameObject> observerPrefabs = new List<GameObject>();

    private void Start()
    {
        InitializeLevel();
    }

    private void InitializeLevel()
    {
        if (wavesManager == null || observersManager == null)
        {
            Debug.LogError("WavesManager or ObserversManager reference is missing in LevelManager.");
            return;
        }

        // 1. Ditar o tamanho da Grid e criar
        wavesManager.WaveSize = WaveSize;
        wavesManager.CreateGrid(GridX, GridY);

        // 2. Passar a lista de observers para o ObserversManager
        observersManager.Initialize(WaveSize);
        observersManager.Observe(observerPrefabs);

        // 3. Posicionar o ObserversManager à direita da Grid
        // A Grid (WavesManager) agora está deslocada para a esquerda em 2 * WaveSize
        // Grid MinX = -(GridX/2 + 1) * WaveSize - 2 * WaveSize
        // Grid MaxX = (GridX/2 + 1) * WaveSize - 2 * WaveSize

        // O ponto de referência é o centro da Grid (Shifted Center: -2, 0).
        float gridFrameWidth = (GridX + 2) * WaveSize;
        float gridFrameHeight = (GridY + 2) * WaveSize;

        // MaxX da Grid (borda direita da frame, já com o shift de -2)
        // MaxX = CenterX + HalfWidth = -2 + gridFrameWidth/2
        float gridMaxX = -2 * WaveSize + (gridFrameWidth / 2f);

        // Posicionar o ObserversManager: gridMaxX + 1 (espaço) + 0.5 (metade do ObserversManager)
        float observersManagerCenterX = gridMaxX + 1.0f * WaveSize + 0.5f * WaveSize;

        // O ObserversManager deve estar alinhado verticalmente com o centro da Grid (0)
        observersManager.transform.position = new Vector3(observersManagerCenterX, 0f, 0f);

        // 4. Ajustar a Câmera
        AdjustCamera();
    }

    private void AdjustCamera()
    {
        // Largura total a ser visível: Grid + Espaço + ObserversManager
        float gridFrameWidth = (GridX + 2) * WaveSize;
        float totalWidth = gridFrameWidth + WaveSize + WaveSize; // Grid(X+2) + Espaço(1) + Observers(1)

        // Altura a ser visível: Máxima entre a Grid e o ObserversManager
        float gridVisibleHeight = (GridY + 2) * WaveSize;
        float observersVisibleHeight = observersManager.TotalHeight;
        float totalHeight = Mathf.Max(gridVisibleHeight, observersVisibleHeight);

        // O WavesManager lida com o ajuste real
        wavesManager.FitCameraToBounds(totalWidth, totalHeight);
    }
}