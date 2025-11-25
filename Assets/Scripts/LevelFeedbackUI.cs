using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class LevelFeedbackUI : MonoBehaviour
{
    // Dependências
    [Header("Dependencies")]
    public LevelManager levelManager;
    public WavesManager wavesManager;
    public Image progressBarFillImage;
    public RectTransform progressBarContainer;
    public TextMeshProUGUI percentage;

    // ... (Referências de Estrela inalteradas) ...
    [Header("Stars UI References")]
    [Tooltip("Estrela 1 (Threshold mais baixo). Deve ter pivot (0.5, 0.5) para escala correta.")]
    public RectTransform star1Rect;
    [Tooltip("Estrela 2 (Threshold médio). Deve ter pivot (0.5, 0.5) para escala correta.")]
    public RectTransform star2Rect;
    [Tooltip("Estrela 3 (Threshold mais alto). Deve ter pivot (0.5, 0.5) para escala correta.")]
    public RectTransform star3Rect;

    // NOVAS REFERÊNCIAS DE FEEDBACK E BOTÃO
    [Header("5. Final Feedback UI")]
    [Tooltip("Fundo que irá aumentar a opacidade para escurecer a tela.")]
    public Image backgroundOverlay;
    [Tooltip("Mensagem para 1 estrela.")]
    public RectTransform goodMessage;
    [Tooltip("Mensagem para 2 estrelas.")]
    public RectTransform excellentMessage;
    [Tooltip("Mensagem para 3 estrelas.")]
    public RectTransform perfectMessage;
    [Tooltip("Mensagem para 0 estrelas (Falha).")]
    public RectTransform failureMessage;
    [Tooltip("Botão para continuar a ser habilitado no final.")]
    public GameObject continueButton;

    // Parâmetros de Animação
    [Header("6. Final Animation Parameters")]
    public float overlayFadeDuration = 0.5f;
    [Range(0f, 1f)]
    public float targetOverlayOpacity = 0.7f;
    public float messagePunchScale = 1.2f;
    public float failureScaleDuration = 0.5f;
    public float finalDisplayDelay = 0.5f; // Delay antes de mostrar a mensagem após a barra

    // ... (Parâmetros de Animação e Gradiente inalterados) ...
    [Header("1. Target Sprite Animation")]
    public float spriteMoveDuration = 1.0f;
    public Ease spriteMoveEase = Ease.OutBack;
    public float spriteScaleDuration = 1.0f;
    public Ease spriteScaleEase = Ease.InOutQuad;
    [Header("2. Progress Bar Punch")]
    public float punchDuration = 0.5f;
    public float punchScale = 1.1f;
    public int punchVibrato = 10;
    public float punchElasticity = 0.5f;
    [Header("3. Progress Bar Fill Animation")]
    public float fillDuration = 2.5f;
    public float fillStartDelay = 0.5f;
    public Ease fillEase = Ease.OutCubic;
    public float starPunchScale = 1.5f;
    [Header("4. Progress Bar Color Gradient")]
    public Gradient ColorGradient;


    // Referências privadas...
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
            progressBarFillImage.fillAmount = 0f;
            if (ColorGradient != null)
            {
                progressBarFillImage.color = ColorGradient.Evaluate(0f);
            }
        }

        // Chama o Reset completo no Awake para garantir o estado inicial zero
        // antes mesmo do OnEnable/StartLevelEvaluation.
        ResetFinalFeedbackUI();

        // Desativa UI no início (deve ser a última linha)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Inicia o processo de feedback e animação do nível.
    /// </summary>
    public void StartLevelEvaluation()
    {
        // ... (Verificações de dependência inalteradas) ...
        if (levelManager == null || wavesManager == null || ColorGradient == null || star1Rect == null)
        {
            Debug.LogError("Dependencies or Star Rects not fully set in LevelFeedbackUI.");
            return;
        }

        // 1. Inicializa a lista de estrelas
        _starReferences.Clear();
        _starReferences.Add((levelManager.OneStarThreshold, star1Rect));
        _starReferences.Add((levelManager.TwoStarsThreshold, star2Rect));
        _starReferences.Add((levelManager.ThreeStarsThreshold, star3Rect));

        gameObject.SetActive(true);
        _leftSprite = wavesManager.LeftSpriteRenderer;

        // Resetar o estado das estrelas e animação
        _star1Animated = _star2Animated = _star3Animated = false;

        // Resetar a UI de Feedback
        ResetAndPositionStars();
        ResetFinalFeedbackUI();

        // Resetar barra de progresso
        if (progressBarFillImage != null)
        {
            progressBarFillImage.color = ColorGradient.Evaluate(0f);
            progressBarFillImage.fillAmount = 0f;
        }

        if (_leftSprite == null)
        {
            Debug.LogError("Left Sprite Renderer not found in WavesManager.");
            return;
        }
        _initialLeftSpriteScale = _leftSprite.transform.localScale.x;
        AnimateTargetSpriteToGrid();
    }

    private void ResetAndPositionStars()
    {
        if (progressBarContainer == null)
        {
            Debug.LogWarning("ProgressBarContainer is null. Skipping star positioning.");
            return;
        }

        // Tenta obter a largura correta calculada pelo sistema de layout
        float containerWidth = progressBarContainer.rect.width;
        float containerHeight = progressBarContainer.rect.height; // Usamos rect.height

        // Fallback robusto para problemas de timing 
        if (containerWidth <= 0 || containerHeight <= 0)
        {
            containerWidth = progressBarContainer.sizeDelta.x;
            containerHeight = progressBarContainer.sizeDelta.y;
            if (containerWidth <= 0 || containerHeight <= 0)
            {
                Debug.LogError("Não foi possível determinar a largura/altura da barra para posicionar as estrelas corretamente. Verifique se a UI está ativa e configurada.");
                return;
            }
        }
        
        foreach (var starRef in _starReferences)
        {
            RectTransform starRect = starRef.starRect;
            //float normalizedPosition = starRef.threshold;

            if (starRect != null)
            {
                // Calcula a posição local no eixo X
                //float xPos = normalizedPosition * containerWidth; // Distância da borda esquerda.

                // Cálculo da Posição Y (acima da barra)
                //float starHeight = starRect.sizeDelta.y;
                //float yPos = (containerHeight / 2f) + (starHeight / 2f); // Altura: metade da barra + metade da estrela

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
        // ... (Animação do Sprite inalterada) ...
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
        if (progressBarFillImage == null || percentage == null)
        {
            Debug.LogError("ProgressBarFillImage ou Percentage Text não estão definidos.");
            return;
        }

        WavesManager.CollapsedGridData targetData = JsonUtility.FromJson<WavesManager.CollapsedGridData>(LevelManager.CurrentLevelData.target.text);
        float finalSimilarity = wavesManager.GetGridSimilarity(targetData);
        float fillTarget = finalSimilarity;

        if (finalSimilarity > 0.9999f)
        {
            fillTarget = 1.0f;
        }

        float dynamicFillDuration = fillDuration * fillTarget;
        float currentFillAmount = progressBarFillImage.fillAmount;

        // 🎯 CORREÇÃO AQUI: ATIVAR O GAMEOBJECT DO TEXTO
        percentage.gameObject.SetActive(true);

        // Garante que o texto comece em 0%
        percentage.text = "Similarity 0.0%";

        // 2. Tweening do preenchimento usando o fillAmount
        DOTween.To(() => currentFillAmount, x =>
        {
            progressBarFillImage.fillAmount = x;

            if (ColorGradient != null)
            {
                progressBarFillImage.color = ColorGradient.Evaluate(x);
            }

            // ================================================================
            // 🎯 NOVO CÓDIGO: ATUALIZAÇÃO DO TEXTO DE PORCENTAGEM
            // ================================================================

            // 1. Converte o valor normalizado (0 a 1) para porcentagem (0 a 100)
            float percentValue = x * 100f;

            // 2. Formata o valor com UMA casa decimal (Ex: 95.7%)
            // Usa o especificador F1 para formatar com uma casa decimal.
            string formattedPercent = percentValue.ToString("F1");

            // 3. Atualiza o componente TextMeshPro
            percentage.text = $"Similarity {formattedPercent}%";

            // ================================================================

            // Verifica e dispara os métodos de animação da estrela no OnUpdate
            CheckStarThresholds(x);

        }, fillTarget, dynamicFillDuration)
            .SetEase(fillEase)
            .SetDelay(fillStartDelay)
            .OnComplete(() =>
            {
                // Opcional: Garante 100.0% se o fillTarget for 1.0f
                if (fillTarget >= 0.99f)
                {
                    percentage.text = "Similarity 100.0%";
                }

                // 4. Ao completar o preenchimento, exibe o feedback final
                int stars = levelManager.EvaluateLevelRating();
                Debug.Log($"Finalizado! Estrelas conquistadas: {stars}");

                // Chamada para a nova função de feedback final
                DisplayFinalFeedback(stars);
            });
    }
    private void DisplayFinalFeedback(int stars)
    {
        // 1. FADE DO OVERLAY DE FUNDO
        // Inicia o fade do background overlay para escurecer a cena
        Sequence finalSequence = DOTween.Sequence();

        if (backgroundOverlay != null)
        {
            // Faz o fade da opacidade da imagem
            finalSequence.Append(backgroundOverlay.DOFade(targetOverlayOpacity, overlayFadeDuration));
        }

        // 2. HABILITAR E ANIMAR MENSAGEM
        RectTransform messageRect = GetMessageRectForStars(stars);

        if (messageRect != null)
        {
            // O Delay é aplicado pela Sequence
            finalSequence.AppendCallback(() =>
            {
                // Ativa o objeto da mensagem
                messageRect.gameObject.SetActive(true);

                if (stars > 0)
                {
                    // Sucesso (1, 2, 3 estrelas): Punch Scale
                    messageRect.localScale = Vector3.one; // Garante que a escala base seja 1 antes do punch
                    messageRect.DOPunchScale(Vector3.one * messagePunchScale, punchDuration, punchVibrato, punchElasticity);
                }
                else // 0 estrelas (Falha)
                {
                    // Falha (0 estrelas): Scale up com Fade In
                    Image messageImage = messageRect.GetComponent<Image>();
                    // Desativa temporariamente para garantir a opacidade correta do texto/imagens filhas
                    messageRect.localScale = Vector3.one * 0.5f; // Começa pequeno

                    // Zera a opacidade (garantindo que todas as imagens filhas também fadem)
                    CanvasGroup cg = messageRect.GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        cg = messageRect.gameObject.AddComponent<CanvasGroup>();
                    }
                    cg.alpha = 0f;

                    // Animação de Scale e Fade In
                    messageRect.DOScale(Vector3.one, failureScaleDuration).SetEase(Ease.OutBack);
                    cg.DOFade(1f, failureScaleDuration);
                }
            });
        }

        // 3. HABILITAR BOTÃO DE CONTINUAR
        // Adiciona um delay adicional antes de mostrar o botão (opcional)
        finalSequence.AppendInterval(finalDisplayDelay)
            .OnComplete(() =>
            {
                continueButton?.SetActive(true);
            });
    }

    /// <summary>
    /// Retorna o RectTransform da mensagem correspondente ao número de estrelas.
    /// Desativa as outras mensagens.
    /// </summary>
    private RectTransform GetMessageRectForStars(int stars)
    {
        RectTransform targetRect = null;

        // Lista de todas as mensagens para desativar as incorretas
        List<RectTransform> allMessages = new List<RectTransform> { goodMessage, excellentMessage, perfectMessage, failureMessage };

        switch (stars)
        {
            case 3:
                targetRect = perfectMessage;
                break;
            case 2:
                targetRect = excellentMessage;
                break;
            case 1:
                targetRect = goodMessage;
                break;
            case 0:
                targetRect = failureMessage;
                break;
            default:
                Debug.LogWarning($"Resultado de estrela inesperado: {stars}. Nenhuma mensagem será exibida.");
                return null;
        }

        // Desativa todas, exceto a alvo
        foreach (var msg in allMessages)
        {
            if (msg != targetRect)
            {
                msg?.gameObject.SetActive(false);
            }
        }

        return targetRect;
    }


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
        }
    }

    /// <summary>
    /// Desativa todos os elementos de feedback final no início.
    /// </summary>
    private void ResetFinalFeedbackUI()
    {
        // 1. Desativa mensagens e botão
        goodMessage?.gameObject.SetActive(false);
        excellentMessage?.gameObject.SetActive(false);
        perfectMessage?.gameObject.SetActive(false);
        failureMessage?.gameObject.SetActive(false);
        continueButton?.SetActive(false);

        // 2. Reseta a opacidade do Overlay
        if (backgroundOverlay != null)
        {
            Color c = backgroundOverlay.color;
            backgroundOverlay.color = new Color(c.r, c.g, c.b, 0f);
        }

        // NOVO CÓDIGO: Reseta e desativa o texto de porcentagem
        if (percentage != null)
        {
            percentage.gameObject.SetActive(false);
            percentage.text = "Similarity 0.0%";
        }
    }

    private void OnDisable()
    {
        // NOVO CÓDIGO: Garante que todos os elementos de feedback sejam resetados
        // quando o componente (ou o GameObject) é desativado, prevenindo bugs
        // visuais na próxima ativação.
        ResetFinalFeedbackUI();

        // Opcional: Para cancelar quaisquer Tweens ativos na barra ou sprites,
        // garantindo que não haja animações fantasmas.
        DOTween.Kill(progressBarContainer, complete: true);
        if (_leftSprite != null)
        {
            DOTween.Kill(_leftSprite.transform, complete: true);
        }
    }
}