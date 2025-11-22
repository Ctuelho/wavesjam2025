using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class LevelFeedbackUI : MonoBehaviour
{
    // Dependências
    [Header("Dependencies")]
    public LevelManager levelManager;
    public WavesManager wavesManager;
    // Alteramos para a referência do Image, que contém a propriedade fillAmount
    public Image progressBarFillImage;
    public RectTransform progressBarContainer;

    // NOVAS REFERÊNCIAS DE ESTRELA (DIRETAS)... (Inalterado)
    [Header("Stars UI References")]
    [Tooltip("Estrela 1 (Threshold mais baixo). Deve ter pivot (0.5, 0.5) para escala correta.")]
    public RectTransform star1Rect;
    [Tooltip("Estrela 2 (Threshold médio). Deve ter pivot (0.5, 0.5) para escala correta.")]
    public RectTransform star2Rect;
    [Tooltip("Estrela 3 (Threshold mais alto). Deve ter pivot (0.5, 0.5) para escala correta.")]
    public RectTransform star3Rect;
    // ... (Parâmetros de Animação e Gradiente inalterados) ...

    // Parâmetros do Left Sprite (Alvo)
    [Header("1. Target Sprite Animation")]
    public float spriteMoveDuration = 1.0f;
    public Ease spriteMoveEase = Ease.OutBack;
    public float spriteScaleDuration = 1.0f;
    public Ease spriteScaleEase = Ease.InOutQuad;

    // Parâmetros da Barra de Progresso (Punch Scale)
    [Header("2. Progress Bar Punch")]
    public float punchDuration = 0.5f;
    public float punchScale = 1.1f;
    public int punchVibrato = 10;
    public float punchElasticity = 0.5f;

    // Parâmetros da Animação da Barra de Progresso (Preenchimento)
    [Header("3. Progress Bar Fill Animation")]
    public float fillDuration = 2.5f;
    public float fillStartDelay = 0.5f;
    public Ease fillEase = Ease.OutCubic;
    public float starPunchScale = 1.5f;

    // CAMPO DE GRADIENTE
    [Header("4. Progress Bar Color Gradient")]
    public Gradient ColorGradient;


    // Referências privadas... (Inalterado)
    private SpriteRenderer _leftSprite;
    private float _initialLeftSpriteScale;
    private bool _star1Animated = false;
    private bool _star2Animated = false;
    private bool _star3Animated = false;
    private List<(float threshold, RectTransform starRect)> _starReferences = new List<(float, RectTransform)>();


    private void Awake()
    {
        // Garante que a barra de progresso comece vazia (fillAmount = 0)
        if (progressBarFillImage != null)
        {
            // O fillAmount é a forma correta de preencher quando Image Type é 'Filled'
            progressBarFillImage.fillAmount = 0f;

            // Define a cor inicial
            if (ColorGradient != null)
            {
                progressBarFillImage.color = ColorGradient.Evaluate(0f);
            }
        }
        // A lista _starReferences é inicializada em StartLevelEvaluation.
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Inicia o processo de feedback e animação do nível.
    /// </summary>
    public void StartLevelEvaluation()
    {
        if (levelManager == null || wavesManager == null || ColorGradient == null || star1Rect == null)
        {
            Debug.LogError("Dependencies or Star Rects not fully set in LevelFeedbackUI.");
            return;
        }

        // 1. Inicializa a lista de estrelas AQUI para usar os thresholds carregados
        _starReferences.Clear();
        _starReferences.Add((levelManager.OneStarThreshold, star1Rect));
        _starReferences.Add((levelManager.TwoStarsThreshold, star2Rect));
        _starReferences.Add((levelManager.ThreeStarsThreshold, star3Rect));

        gameObject.SetActive(true);
        _leftSprite = wavesManager.LeftSpriteRenderer;

        // Resetar o estado das estrelas
        _star1Animated = _star2Animated = _star3Animated = false;

        // 2. Reposiciona e reseta a escala das estrelas.
        ResetAndPositionStars();

        // Resetar a cor inicial e o preenchimento
        if (progressBarFillImage != null)
        {
            progressBarFillImage.color = ColorGradient.Evaluate(0f);
            progressBarFillImage.fillAmount = 0f;
        }

        // ... (Resto da inicialização inalterado) ...
        if (_leftSprite == null)
        {
            Debug.LogError("Left Sprite Renderer not found in WavesManager.");
            return;
        }
        _initialLeftSpriteScale = _leftSprite.transform.localScale.x;
        AnimateTargetSpriteToGrid();
    }

    /// <summary>
    /// Reposiciona as estrelas na barra de acordo com os thresholds e reseta suas escalas.
    /// </summary>
    private void ResetAndPositionStars()
    {
        if (progressBarContainer == null)
        {
            Debug.LogWarning("ProgressBarContainer is null. Skipping star positioning.");
            return;
        }

        // Tenta obter a largura correta calculada pelo sistema de layout
        float containerWidth = progressBarContainer.rect.width;
        float containerHeight = progressBarContainer.sizeDelta.y;

        // Fallback robusto para problemas de timing (MANTIDO)
        if (containerWidth <= 0)
        {
            containerWidth = progressBarContainer.sizeDelta.x;
            if (containerWidth <= 0)
            {
                Debug.LogError("Não foi possível determinar a largura da barra para posicionar as estrelas corretamente. Verifique se a UI está ativa e configurada.");
                return;
            }
            Debug.LogWarning($"ProgressBarContainer rect.width is zero. Using sizeDelta.x ({containerWidth}) for star positioning. Check UI configuration.");
        }

        foreach (var starRef in _starReferences)
        {
            RectTransform starRect = starRef.starRect;
            float normalizedPosition = starRef.threshold;

            if (starRect != null)
            {
                // Calcula a posição local no eixo X
                float xPos = normalizedPosition * (containerWidth/2f);
                float starHeight = starRect.sizeDelta.y;
                float yPos = (containerHeight / 2f) + (starHeight / 2f);
                //starRect.anchoredPosition = new Vector2(xPos, yPos);

                // Garante que a estrela comece invisível/pequena
                starRect.localScale = Vector3.zero;

                // Define o parent (Garantia)
                //starRect.SetParent(progressBarContainer.transform, false);
            }
        }
    }

    private void AnimateTargetSpriteToGrid()
    {
        // ... (Código inalterado) ...
        float waveSize = wavesManager.WaveSize;
        float gridSize = LevelManager.CurrentLevelData.gridSize;
        float actualGridWidth = gridSize * waveSize;
        float actualGridHeight = gridSize * waveSize;
        Vector3 targetPosition = Vector3.zero;
        Vector3 targetScale = Vector3.one * Mathf.Max(actualGridWidth, actualGridHeight);
        Sequence spriteSequence = DOTween.Sequence();
        spriteSequence.Append(_leftSprite.transform.DOMove(targetPosition, spriteMoveDuration).SetEase(spriteMoveEase));
        spriteSequence.Join(_leftSprite.transform.DOScale(targetScale, spriteScaleDuration).SetEase(spriteScaleEase));
        spriteSequence.OnComplete(AnimateProgressBar);
    }

    private void AnimateProgressBar()
    {
        // 2. Punch na barra de progresso
        progressBarContainer.DOPunchScale(Vector3.one * punchScale, punchDuration, punchVibrato, punchElasticity)
            .OnComplete(() =>
            {
                // 3. Animação de preenchimento após o Punch
                StartFillAnimation();
            });
    }


    private void StartFillAnimation()
    {
        if (progressBarFillImage == null)
        {
            Debug.LogError("ProgressBarFillImage não está definido. Certifique-se de que a barra tem um componente Image e está configurada como Filled.");
            return;
        }

        // 1. Obter o resultado final da similaridade
        WavesManager.CollapsedGridData targetData = JsonUtility.FromJson<WavesManager.CollapsedGridData>(LevelManager.CurrentLevelData.target.text);
        float finalSimilarity = wavesManager.GetGridSimilarity(targetData); // Valor entre 0.0 e 1.0

        // Trata o erro de ponto flutuante para garantir 100% de preenchimento
        float fillTarget = finalSimilarity;
        if (finalSimilarity > 0.9999f)
        {
            fillTarget = 1.0f;
        }

        // CALCULA A DURAÇÃO DINÂMICA
        float dynamicFillDuration = fillDuration * fillTarget;

        // *** CORREÇÃO: Usa fillTarget para animar fillAmount ***
        float currentFillAmount = progressBarFillImage.fillAmount;

        // 2. Tweening do preenchimento usando o fillAmount
        DOTween.To(() => currentFillAmount, x =>
        {
            // Atualiza o Fill Amount real da barra (Visual)
            progressBarFillImage.fillAmount = x;

            // Usa o Gradiente de cor, baseado no fillAmount
            if (ColorGradient != null)
            {
                progressBarFillImage.color = ColorGradient.Evaluate(x);
            }

            // Verifica e dispara os métodos de animação da estrela no OnUpdate
            CheckStarThresholds(x);

        }, fillTarget, dynamicFillDuration) // O valor alvo é fillTarget (0.0 a 1.0)
            .SetEase(fillEase)
            .SetDelay(fillStartDelay)
            .OnComplete(() =>
            {
                int stars = levelManager.EvaluateLevelRating();
                Debug.Log($"Finalizado! Estrelas conquistadas: {stars}");
            });
    }

    // ... (CheckStarThresholds e AnimateStar inalterados) ...
    private void CheckStarThresholds(float currentProgress)
    {
        // Estrela 1
        if (!_star1Animated && currentProgress >= levelManager.OneStarThreshold)
        {
            _star1Animated = true;
            AnimateStar(1);
        }

        // Estrela 2
        if (!_star2Animated && currentProgress >= levelManager.TwoStarsThreshold)
        {
            _star2Animated = true;
            AnimateStar(2);
        }

        // Estrela 3
        if (!_star3Animated && currentProgress >= levelManager.ThreeStarsThreshold)
        {
            _star3Animated = true;
            AnimateStar(3);
        }
    }

    /// <summary>
    /// Anima a estrela correspondente ao número (1, 2, ou 3).
    /// </summary>
    private void AnimateStar(int starNumber)
    {
        RectTransform starRect = null;

        switch (starNumber)
        {
            case 1:
                starRect = star1Rect;
                break;
            case 2:
                starRect = star2Rect;
                break;
            case 3:
                starRect = star3Rect;
                break;
        }

        if (starRect != null)
        {
            // Animação: Dá um "Punch" na escala da estrela e volta, tornando-a visível
            starRect.DOScale(Vector3.one * starPunchScale, 0.2f)
                .SetEase(Ease.OutCirc)
                .OnComplete(() =>
                {
                    starRect.DOScale(Vector3.one, 0.1f);
                });

            Debug.Log($"Disparado AnimateStar({starNumber})!");
        }
    }
}