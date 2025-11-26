// LevelManager.cs
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public class LevelData 
    {
        public int levelId;
        public string levelName;
        public int duration = 30;
        public List<GameObject> observers;
        public int gridSize = 3;
        public TextAsset target;
    }

    [Header("Rating Thresholds (0.0 to 1.0)")]
    public float OneStarThreshold = 0.60f;
    public float TwoStarsThreshold = 0.80f;
    public float ThreeStarsThreshold = 0.90f;

    public static LevelData CurrentLevelData;
    public LevelFeedbackUI levelFeedbackUI;
    [Header("Dependencies")]
    public WavesManager wavesManager;
    public ObserversManager observersManager;

    [Header("Grid Settings")]
    //public int GridSize = 9;
    public float WaveSize = 1f;

    public float GRID_OBSERVER_PADDING = 1.0f;

    [Header("Observer Setup")]
    public List<GameObject> observerPrefabs = new List<GameObject>();

    public GameObject leaveButton;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.F5))
        {
            Level.ClearSave();
        }
        else if(Input.GetKeyUp (KeyCode.F6))
        {
            //levelFeedbackUI.StartLevelEvaluation();
        }
    }

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
        wavesManager.CreateGrid(CurrentLevelData.gridSize);
        if(CurrentLevelData.target != null)
        {
            wavesManager.Drawer.DrawGridFromData(CurrentLevelData.target.text);
        }        
        observersManager.Observe(CurrentLevelData.observers);

        AdjustCameraAndPositions();

        Invoke("AdjustCameraAndPositions", 0.1f);
    }


    // LevelManager.cs (Modificação em AdjustCameraAndPositions)

    // LevelManager.cs (Modificado)

    // LevelManager.cs (Modificado)

    private void AdjustCameraAndPositions()
    {
        // A Grid (WavesManager) é a âncora central.
        wavesManager.transform.position = Vector3.zero;

        // 1. DIMENSÕES E PADDINGS X e Y
        float widthLS = wavesManager.LeftSpriteSize;
        float widthO = observersManager.TotalWidth;

        float deltaW = widthLS - widthO;

        float paddingX, paddingY;

        // Força a simetria (X e Y devem ser >= 1.0f)
        if (deltaW > 0) // Left Sprite é mais largo (Compensa em Y)
        {
            paddingX = 1.0f;
            paddingY = 1.0f + deltaW;
        }
        else // Observers é mais largo ou igual (Compensa em X)
        {
            paddingY = 1.0f;
            paddingX = 1.0f - deltaW;
        }

        float gridFrameWidth = (CurrentLevelData.gridSize + 2) * WaveSize;
        float gridVisibleHeight = (CurrentLevelData.gridSize + 2) * WaveSize;

        // Canto Esquerdo do Frame da Grid (relativo ao centro WavesManager/Grid em X=0)
        float gridFrameMinX_Local = -(gridFrameWidth / 2f);
        float gridFrameMaxX_Local = (gridFrameWidth / 2f);

        // 2. POSIÇÃO DO LEFT SPRITE (LS)
        // Chamando o novo método do WavesManager que posiciona o LS relativo ao centro da Grid (X=0)
        wavesManager.UpdateLeftSpritePosition(gridFrameMinX_Local, paddingX);

        // Posição central do LS:
        float leftSpriteCenterX = wavesManager.LeftSpriteRenderer.transform.position.x;

        // Borda Esquerda do LS (Para a câmera)
        float leftSpriteMinX = leftSpriteCenterX - (widthLS / 2f);

        // 3. POSIÇÃO DOS OBSERVERS

        // X Mínimo dos Observers (borda esquerda) = Borda Direita da Grid + Padding Y
        float observersMinX = gridFrameMaxX_Local + paddingY;

        // X Local da borda esquerda do Observers Manager 
        float observersMinX_Local = -observersManager.TotalWidth + observersManager.ObserverSize;

        // Posição Central do ObserversManager
        float observersManagerPosX = observersMinX - observersMinX_Local;
        observersManager.transform.position = new Vector3(observersManagerPosX, 0f, 0f);

        // 4. CÁLCULO DA ÁREA VISÍVEL TOTAL PARA A CÂMERA 🎥

        // O layout final é: [1.0] [LS] [X] [GRID] [Y] [O] [1.0]

        // O ponto de ancoragem original do Left Sprite tinha um padding de ScreenEdgePadding,
        // mas na nova lógica, ele está posicionado relativo à Grid.
        // Se o layout total deve ter um padding fixo de 1.0f na borda da tela:

        // Borda Esquerda da Área Total (Para a Câmera)
        // O LS está em leftSpriteMinX. Adiciona o padding fixo (1.0)
        float totalAreaMinX = leftSpriteMinX - 1.0f;

        // Borda Direita da Área Total (Para a Câmera)
        // Onde termina a última coluna de Observers:
        // Posição Central do Manager + Largura Total
        float observersMaxX_Mundo = observersManagerPosX + observersManager.TotalWidth;

        // Adiciona o padding fixo (1.0)
        float totalAreaMaxX = observersMaxX_Mundo + 1.0f;

        // A área total visível é calculada a partir dos extremos.
        float totalAreaWidth = totalAreaMaxX - totalAreaMinX;
        float totalAreaCenterX = (totalAreaMinX + totalAreaMaxX) / 2f;

        float observersVisibleHeight = observersManager.TotalHeight;
        float totalVisibleHeight = Mathf.Max(gridVisibleHeight, observersVisibleHeight);

        // 5. AJUSTE DA CÂMERA
        // Move a câmera para o centro da Área Total e ajusta o zoom (Ortho Size).
        wavesManager.FitCameraToBounds(totalAreaWidth, totalAreaCenterX, totalVisibleHeight);
    }

    internal static void SetLevelData(Level selectedLevel)
    {
        CurrentLevelData = new LevelData();
        CurrentLevelData.levelName = selectedLevel.name;
        CurrentLevelData.levelId = selectedLevel.levelId;
        CurrentLevelData.duration = selectedLevel.duration;
        CurrentLevelData.observers = selectedLevel.observers.ToList();
        CurrentLevelData.target = selectedLevel.target;
        CurrentLevelData.gridSize = selectedLevel.gridSize;
    }

    public int EvaluateLevelRating()
    {
        if (CurrentLevelData == null || CurrentLevelData.target == null)
        {
            Debug.LogWarning("Level data or target is missing. Returning 0 stars.");
            return 0;
        }

        // 1. Desserializar o JSON alvo para o objeto CollapsedGridData
        WavesManager.CollapsedGridData targetData;
        try
        {
            targetData = JsonUtility.FromJson<WavesManager.CollapsedGridData>(CurrentLevelData.target.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to deserialize target JSON for rating: {e.Message}");
            return 0;
        }

        if (wavesManager == null)
        {
            Debug.LogError("WavesManager is null.");
            return 0;
        }

        // 2. Calcular a similaridade (0.0 a 1.0)
        float similarity = wavesManager.GetGridSimilarity(targetData);

        Debug.Log($"Level Evaluation Complete: Similarity = {similarity * 100:F2}%");

        // 3. Converter a similaridade em estrelas
        int stars = ConvertSimilarityToStars(similarity);

        return stars;
    }

    public int ConvertSimilarityToStars(float similarity)
    {
        // A ordem é importante: Começamos pelo maior threshold.
        if (similarity >= ThreeStarsThreshold)
        {
            return 3;
        }
        else if (similarity >= TwoStarsThreshold)
        {
            return 2;
        }
        else if (similarity >= OneStarThreshold)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}