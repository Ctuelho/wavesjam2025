using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
public class CollapsedGridDrawerUI : MonoBehaviour
{
    [Header("Mini Wave Configuration")]
    public Gradient CollapseColorGradient;
    public int ResolutionPerCell = 10;

    private Image _frameImage;
    private Texture2D _gridTexture;

    private static readonly Color BackgroundColor = Color.white;

    private void Awake()
    {
        _frameImage = GetComponent<Image>();
        if (_frameImage != null)
        {
            Color c = _frameImage.color;
            _frameImage.color = new Color(c.r, c.g, c.b, 0f);
            _frameImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    // Função auxiliar para destruição segura no Editor (CORRIGIDA)
    private void SafeDestroy(Object obj)
    {
        if (obj == null) return;

        // CORREÇÃO: Usamos a diretiva de compilação para garantir
        // que DestroyImmediate(obj, true) seja chamado SOMENTE no Unity Editor,
        // independentemente do estado de Application.isPlaying.
#if UNITY_EDITOR
        {
            // O Unity no Play Mode, mas rodando código de Assets, exige
            // que usemos 'DestroyImmediate(..., true)' para Assets dinâmicos.
            DestroyImmediate(obj, true);
        }
#else
        {
            // Em builds de jogo (runtime), o Destroy() normal funciona perfeitamente.
            Destroy(obj);
        }
#endif
    }

    public void DrawGridFromData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("CollapsedGridDrawerUI: JSON data is null or empty.");
            return;
        }

        WavesManager.CollapsedGridData data;
        try
        {
            data = JsonUtility.FromJson<WavesManager.CollapsedGridData>(jsonData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CollapsedGridDrawerUI: Failed to deserialize JSON data: {e.Message}");
            return;
        }

        if (data == null || _frameImage == null)
        {
            Debug.LogError("CollapsedGridDrawerUI: Deserialized data is null, or Image component is missing.");
            return;
        }

        int gridX = data.width;
        int gridY = data.height;

        if (gridX <= 0 || gridY <= 0)
        {
            Debug.LogWarning("CollapsedGridDrawerUI: Grid dimensions are zero or negative. Clearing drawings.");
            ClearDrawings();
            return;
        }

        int texWidth = gridX * ResolutionPerCell;
        int texHeight = gridY * ResolutionPerCell;

        // 2. Inicializar ou Redimensionar a Textura
        if (_gridTexture == null || _gridTexture.width != texWidth || _gridTexture.height != texHeight)
        {
            // Destruição segura da Textura anterior
            if (_gridTexture != null)
            {
                SafeDestroy(_gridTexture);
            }

            // Usar RGBA32 para Alpha e cores melhores
            _gridTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            _gridTexture.filterMode = FilterMode.Point;
            _gridTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // 3. Modificar os Pixels da Textura
        Color[] pixelArray = new Color[texWidth * texHeight];

        // Inicializar o array de pixels com a cor de fundo (Branco)
        for (int p = 0; p < pixelArray.Length; p++)
        {
            pixelArray[p] = BackgroundColor;
        }

        for (int i = 0; i < gridX; i++)
        {
            for (int j = 0; j < gridY; j++)
            {
                int flatIndex = i * gridY + j;

                if (flatIndex < data.flattenedCollapseValues.Length)
                {
                    float collapseValue = data.flattenedCollapseValues[flatIndex];
                    Color waveColor = CollapseColorGradient.Evaluate(Mathf.Clamp01(collapseValue));

                    // Desenha o quadrado da célula
                    for (int px = 0; px < ResolutionPerCell; px++)
                    {
                        for (int py = 0; py < ResolutionPerCell; py++)
                        {
                            int currentPixelX = i * ResolutionPerCell + px;
                            int currentPixelY = j * ResolutionPerCell + py;
                            int pixelIndex = currentPixelY * texWidth + currentPixelX;

                            pixelArray[pixelIndex] = waveColor;
                        }
                    }
                }
            }
        }

        // 4. Aplicar e Atualizar Textura
        _gridTexture.SetPixels(pixelArray);
        _gridTexture.Apply();

        // 5. Aplicar a Textura ao Componente Image

        // Destruição segura do Sprite anterior.
        if (_frameImage.sprite != null)
        {
            SafeDestroy(_frameImage.sprite);
        }

        Sprite newSprite = Sprite.Create(_gridTexture,
                                         new Rect(0, 0, texWidth, texHeight),
                                         Vector2.one * 0.5f,
                                         ResolutionPerCell);

        _frameImage.sprite = newSprite;
        _frameImage.color = Color.white;
        _frameImage.enabled = true;
    }

    public void ClearDrawings()
    {
        if (_frameImage != null)
        {
            _frameImage.enabled = false;

            if (_frameImage.sprite != null)
            {
                SafeDestroy(_frameImage.sprite);
                _frameImage.sprite = null;
            }
        }
    }

    private void OnDestroy()
    {
        // Libera a memória da GPU da textura.
        if (_gridTexture != null)
        {
            SafeDestroy(_gridTexture);
        }
    }
}