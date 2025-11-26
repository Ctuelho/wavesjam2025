using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class CollapsedGridDrawer : MonoBehaviour
{
    [Header("Mini Wave Configuration")]
    public Sprite CellSprite;
    public Gradient CollapseColorGradient;

    private List<GameObject> _currentMiniWaves = new List<GameObject>();
    private SpriteRenderer _frameRenderer;
    private int _gridX, _gridY;

    private void Awake()
    {
        _frameRenderer = GetComponent<SpriteRenderer>();
    }

    public void DrawGridFromData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("CollapsedGridDrawer: JSON data is null or empty.");
            return;
        }

        WavesManager.CollapsedGridData data;
        try
        {
            data = JsonUtility.FromJson<WavesManager.CollapsedGridData>(jsonData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CollapsedGridDrawer: Failed to deserialize JSON data: {e.Message}");
            return;
        }

        if (data == null || CellSprite == null || _frameRenderer == null)
        {
            Debug.LogError("CollapsedGridDrawer: Deserialized data is null, CellSprite ou FrameRenderer está faltando.");
            return;
        }

        ClearPreviousDrawings();

        // 2. Configurar o Frame Principal e Obter Parâmetros
        float frameScale = _frameRenderer.transform.localScale.x;
        Vector3 frameLocalPosition = _frameRenderer.transform.localPosition;
        float frameZ = frameLocalPosition.z;

        _gridX = data.width;
        _gridY = data.height;

        // =========================================================================================
        // 3. CÁLCULO DE ESCALA E POSIÇÃO (IGNORANDO frameLocalPosition.x)
        // =========================================================================================

        float maxDim = Mathf.Max(_gridX, _gridY);
        float miniWaveScale = frameScale / maxDim;

        float totalGridWidth = _gridX * miniWaveScale;
        float totalGridHeight = _gridY * miniWaveScale;

        // Ponto de início (Canto Inferior Esquerdo):
        // ANULAÇÃO DO OFFSET X: Usamos 0 em X e mantemos o frameLocalPosition.y.
        // A grade será centralizada em X=0 do objeto pai.
        float startX = 0f - (totalGridWidth / 2f);
        float startY = frameLocalPosition.y - (totalGridHeight / 2f); // Mantemos Y

        // Fator de escala do objeto pai
        float parentScaleFactor = transform.localScale.x;

        // =========================================================================================
        // FIM DO CÁLCULO
        // =========================================================================================

        // 4. Desenhar Mini Waves
        for (int i = 0; i < _gridX; i++)
        {
            for (int j = 0; j < _gridY; j++)
            {
                int flatIndex = i * _gridY + j;

                if (flatIndex < data.flattenedCollapseValues.Length)
                {
                    float collapseValue = data.flattenedCollapseValues[flatIndex];
                    Color waveColor = CollapseColorGradient.Evaluate(Mathf.Clamp01(collapseValue));

                    // 5. Criar GameObject, Adicionar SpriteRenderer e Configurar
                    GameObject miniWaveObj = new GameObject($"MiniWave {i}-{j}");
                    miniWaveObj.transform.SetParent(transform, worldPositionStays: false);

                    SpriteRenderer sr = miniWaveObj.AddComponent<SpriteRenderer>();
                    sr.sprite = CellSprite;
                    sr.sortingOrder = 1;
                    sr.color = waveColor;

                    // Posicionamento no plano (i, j):
                    float centerOffsetX = (i * miniWaveScale) + (miniWaveScale / 2f);
                    float centerOffsetY = (j * miniWaveScale) + (miniWaveScale / 2f);

                    // posX é o deslocamento a partir do centro (0)
                    float posX = startX + centerOffsetX;
                    float posY = startY + centerOffsetY;

                    // 🎯 COMPENSAÇÃO DE POSIÇÃO FINAL
                    float finalLocalX = posX / parentScaleFactor;
                    float finalLocalY = posY / parentScaleFactor;

                    miniWaveObj.transform.localPosition = new Vector3(finalLocalX, finalLocalY, frameZ - 0.01f);

                    // COMPENSAÇÃO DE ESCALA
                    float finalLocalScale = miniWaveScale / parentScaleFactor;
                    miniWaveObj.transform.localScale = Vector3.one * finalLocalScale;

                    _currentMiniWaves.Add(miniWaveObj);
                }
            }
        }
    }

    public void ClearPreviousDrawings()
    {
        foreach (var wave in _currentMiniWaves)
        {
            if (wave != null)
            {
                Destroy(wave);
            }
        }
        _currentMiniWaves.Clear();
    }
}