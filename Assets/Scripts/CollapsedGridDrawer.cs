using UnityEngine;
using System.Collections.Generic;

// Requer um SpriteRenderer no GameObject para o frame principal (LeftSpriteRenderer)
[RequireComponent(typeof(SpriteRenderer))]
public class CollapsedGridDrawer : MonoBehaviour
{
    [Header("Mini Wave Configuration")]
    // O Sprite a ser usado para desenhar cada célula da mini-wave
    public Sprite CellSprite;
    // Gradient para mapear o valor Collapse (0.0f a 1.0f) para uma cor
    public Gradient CollapseColorGradient;

    // Lista para manter o controle dos objetos de mini-wave criados para limpeza
    private List<GameObject> _currentMiniWaves = new List<GameObject>();
    private SpriteRenderer _frameRenderer;

    // Variáveis para cache do tamanho da grid
    private int _gridX, _gridY;

    private void Awake()
    {
        _frameRenderer = GetComponent<SpriteRenderer>();
        // Garante que o frame principal comece invisível, ou tenha um Alpha de 0
        if (_frameRenderer != null)
        {
            Color c = _frameRenderer.color;
            _frameRenderer.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    /// <summary>
    /// Desenha a mini-grid lendo os dados de colapso de uma string JSON.
    /// Assume que este script está no LeftSpriteRenderer, obtendo a posição e escala dele.
    /// </summary>
    /// <param name="jsonData">String JSON contendo os dados da WavesManager.CollapsedGridData.</param>
    public void DrawGridFromData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("CollapsedGridDrawer: JSON data is null or empty.");
            return;
        }

        // 1. Deserializar o JSON
        WavesManager.CollapsedGridData data;
        try
        {
            // Nota: Requer que WavesManager.CollapsedGridData seja acessível
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
        Vector3 framePosition = _frameRenderer.transform.position;
        // Obtemos a escala para determinar o tamanho total do "frame"
        float frameScale = _frameRenderer.transform.localScale.x;

        Color c = _frameRenderer.color;
        _frameRenderer.color = new Color(c.r, c.g, c.b, 1f); // Torna o frame visível

        // Obtemos as dimensões da grid
        _gridX = data.width;
        _gridY = data.height;

        // Escala da Mini-Wave: O lado do frame dividido pelo maior número de células (para caber perfeitamente)
        float miniWaveScale = frameScale / Mathf.Max(_gridX, _gridY);
        float halfFrameScale = frameScale / 2f;

        // 3. Calcular Posições de Offset
        // Posição inicial (canto inferior esquerdo)
        float startX = framePosition.x - halfFrameScale + miniWaveScale / 2f;
        float startY = framePosition.y - halfFrameScale + miniWaveScale / 2f;

        // 4. Desenhar Mini Waves
        for (int i = 0; i < _gridX; i++)
        {
            for (int j = 0; j < _gridY; j++)
            {
                // Acesso à grid de colapso
                int flatIndex = i * _gridY + j;

                if (flatIndex < data.flattenedCollapseValues.Length)
                {
                    float collapseValue = data.flattenedCollapseValues[flatIndex];

                    // Obter a cor do Gradiente
                    Color waveColor = CollapseColorGradient.Evaluate(Mathf.Clamp01(collapseValue));

                    // 5. Criar GameObject, Adicionar SpriteRenderer e Configurar
                    GameObject miniWaveObj = new GameObject($"MiniWave {i}-{j}");
                    miniWaveObj.transform.SetParent(transform);

                    // Adicionar o SpriteRenderer
                    SpriteRenderer sr = miniWaveObj.AddComponent<SpriteRenderer>();
                    sr.sprite = CellSprite; // Aplica o sprite de referência
                    sr.sortingOrder = 1; // Garante que seja desenhado acima do frame (Background)
                    sr.color = waveColor;

                    // Posicionamento no plano (i, j)
                    float posX = startX + i * miniWaveScale;
                    float posY = startY + j * miniWaveScale;
                    // Posicionamento Z ligeiramente à frente do frame (se o frame Z for 0, isto é -0.01)
                    miniWaveObj.transform.position = new Vector3(posX, posY, framePosition.z - 0.01f);

                    // A escala do objeto é o tamanho da célula calculado
                    miniWaveObj.transform.localScale = Vector3.one * miniWaveScale;

                    _currentMiniWaves.Add(miniWaveObj);
                }
            }
        }
    }

    /// <summary>
    /// Destrói todos os objetos de mini-wave criados.
    /// </summary>
    public void ClearPreviousDrawings()
    {
        foreach (var wave in _currentMiniWaves)
        {
            if (wave != null)
            {
                // Usamos DestroyImmediate no editor, mas Destroy em tempo de execução
                // Para simplificar, vou manter o Destroy padrão, mas esteja ciente
                // de usar DestroyImmediate se você estiver chamando isso dentro de OnValidate/Editor Scripting.
                Destroy(wave);
            }
        }
        _currentMiniWaves.Clear();

        if (_frameRenderer != null)
        {
            // Oculta o frame principal ao limpar
            Color c = _frameRenderer.color;
            _frameRenderer.color = new Color(c.r, c.g, c.b, 0f);
        }
    }
}