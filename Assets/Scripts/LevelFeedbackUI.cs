using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class LevelFeedbackUI : MonoBehaviour
{
    private int stars;
    // Variável privada para controlar o tempo entre os toques de áudio.
    private float _nextSoundTime = 0f;
    // Tempo de início do tweening da barra de preenchimento (após o delay inicial).
    private float _tweenStartTime = 0f;
    // Rastreia o fillAmount na última vez que o som foi tocado para garantir progresso mínimo.
    private float _lastSoundFillAmount = 0f;

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
    public GameObject playAgainButton;

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
    public float fillDuration = 2.5f; // Duração MÁXIMA da animação (100% similaridade)
    public float fillStartDelay = 0.5f;
    public Ease fillEase = Ease.OutCubic;
    public float starPunchScale = 1.5f;
    [Header("4. Progress Bar Color Gradient")]
    public Gradient ColorGradient;

    // 🔊 AUDIO DA BARRA DE PROGRESSO (Fill)
    [Header("Audio Feedback - Progress Bar")]
    [Tooltip("O AudioSource que tocará o som de preenchimento.")]
    public AudioSource audioSource;
    [Tooltip("O clip de áudio para o efeito de preenchimento.")]
    public AudioClip fillSoundClip;
    [Tooltip("O pitch inicial do som (Ex: 0.5).")]
    [Range(0.0f, 3f)]
    public float startPitch = 0.5f;
    [Tooltip("O pitch máximo do som (Ex: 2.0).")]
    [Range(0.0f, 3f)]
    public float endPitch = 2.0f;

    [Tooltip("Fator mínimo do intervalo entre os sons (0 a 1). Garante que a cadência inicial não seja muito rápida.")]
    [Range(0.0f, 1f)]
    public float minFillSoundIntervalFactor = 0.25f; // Valor padrão 0.25 (25% do intervalo máximo)


    // 🌟 NOVOS CAMPOS DE ÁUDIO PARA ESTRELAS
    [Header("Audio Feedback - Stars")]
    [Tooltip("O AudioSource para tocar os sons das estrelas.")]
    public AudioSource starAudioSource;
    [Tooltip("Clip de áudio quando a 1ª estrela é alcançada.")]
    public AudioClip star1Clip1;
    public float Pitch1, Volume1;
    [Tooltip("Clip de áudio quando a 2ª estrela é alcançada.")]
    public AudioClip star2Clip;
    public float Pitch2, Volume2;
    [Tooltip("Clip de áudio quando a 3ª estrela é alcançada.")]
    public AudioClip star3Clip;
    public float Pitch3, Volume3;
    // 🌟 FIM DOS NOVOS CAMPOS DE ÁUDIO PARA ESTRELAS

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

        ResetFinalFeedbackUI();
        gameObject.SetActive(false);
    }

    public void StartLevelEvaluation()
    {
        if (levelManager == null || wavesManager == null || ColorGradient == null || star1Rect == null)
        {
            Debug.LogError("Dependencies or Star Rects not fully set in LevelFeedbackUI.");
            return;
        }

        _starReferences.Clear();
        _starReferences.Add((levelManager.OneStarThreshold, star1Rect));
        _starReferences.Add((levelManager.TwoStarsThreshold, star2Rect));
        _starReferences.Add((levelManager.ThreeStarsThreshold, star3Rect));

        gameObject.SetActive(true);
        _leftSprite = wavesManager.LeftSpriteRenderer;

        _star1Animated = _star2Animated = _star3Animated = false;

        ResetAndPositionStars();
        ResetFinalFeedbackUI();

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

        float containerWidth = progressBarContainer.rect.width;
        float containerHeight = progressBarContainer.rect.height;

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

            if (starRect != null)
            {
                // Garante que a estrela comece invisível/pequena
                starRect.localScale = Vector3.zero;
            }
        }
    }

    private void AnimateTargetSpriteToGrid()
    {
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

    private float fillTarget;
    private void StartFillAnimation()
    {
        if (progressBarFillImage == null || percentage == null)
        {
            Debug.LogError("ProgressBarFillImage ou Percentage Text não estão definidos.");
            return;
        }

        WavesManager.CollapsedGridData targetData = JsonUtility.FromJson<WavesManager.CollapsedGridData>(LevelManager.CurrentLevelData.target.text);
        float finalSimilarity = wavesManager.GetGridSimilarity(targetData);
        if (finalSimilarity >= levelManager.ThreeStarsThreshold)
        {
            finalSimilarity = 1f;
        }
        fillTarget = finalSimilarity;

        if (finalSimilarity > 0.9999f)
        {
            fillTarget = 1.0f;
        }

        // Duração dinâmica baseada no target
        float dynamicFillDuration = fillDuration * fillTarget;
        float currentFillAmount = progressBarFillImage.fillAmount;

        percentage.gameObject.SetActive(true);
        percentage.text = "Similarity 0.0%";

        // ================================================================
        // 🔊 CÓDIGO DE ÁUDIO REESTRUTURADO (Cadência pela Velocidade Relativa) 🔊
        // ================================================================

        _nextSoundTime = 0f;
        // Inicializa o último ponto de toque do som
        _lastSoundFillAmount = progressBarFillImage.fillAmount;

        // 1. CALCULA O FATOR DE PROPORCIONALIDADE (Proporção da Duração)
        float durationFactor = 1f;
        if (fillDuration > 0)
        {
            durationFactor = dynamicFillDuration / fillDuration;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 1f;
        }

        // 2. REGISTRA O TEMPO DE INÍCIO DO TWEEN
        _tweenStartTime = Time.time + fillStartDelay;

        // 3. Tweening do preenchimento usando o fillAmount
        DOTween.To(() => currentFillAmount, x =>
        {
            // Lógica de preenchimento, cor e texto (inalterada)
            progressBarFillImage.fillAmount = x;

            if (ColorGradient != null)
            {
                progressBarFillImage.color = ColorGradient.Evaluate(x);
            }

            float percentValue = x * 100f;
            string formattedPercent = percentValue.ToString("F1");
            percentage.text = $"Similarity {formattedPercent}%";

            CheckStarThresholds(x);

            // ================================================================
            // 🎧 NOVO CONTROLE DE RITMO AJUSTADO PELA CURVA QUADRÁTICA E MÍNIMO 🎧
            // ================================================================

            if (audioSource != null && fillSoundClip != null)
            {
                // Calcula o progresso do tempo (0 a 1) da animação
                float timeElapsed = Time.time - _tweenStartTime;
                float timeProgress = Mathf.Clamp01(timeElapsed / dynamicFillDuration);

                // Aplica a curva de aceleração (Quadrática: t*t)
                float easedTimeProgress = timeProgress * timeProgress;

                // Intervalo MÁXIMO de 0.5s é reduzido pelo durationFactor.
                float maxIntervalBase = 0.5f;
                float maxIntervalAdjusted = maxIntervalBase * durationFactor;

                // Define o intervalo inicial MÍNIMO (ex: 25% do maxIntervalAdjusted)
                float minIntervalStart = maxIntervalAdjusted * minFillSoundIntervalFactor;

                // O intervalo interpola suavemente (usando a curva quadrática) de minIntervalStart para maxIntervalAdjusted
                float calculatedInterval = Mathf.Lerp(minIntervalStart, maxIntervalAdjusted, easedTimeProgress);

                float minInterval = calculatedInterval;

                // --- NOVAS CONDIÇÕES DE TOQUE ---
                float minProgressDelta = 0.01f; // O progresso mínimo (1%) necessário para tocar o som novamente.

                // Condição 1: O tempo mínimo do intervalo passou.
                bool isTimeReady = Time.time >= _nextSoundTime;

                // Condição 2: O progresso atual (x) avançou o suficiente desde o último som.
                bool isProgressReady = (x - _lastSoundFillAmount) >= minProgressDelta;


                // O som só toca se o tempo e o progresso mínimo tiverem sido alcançados, e se não tivermos atingido o alvo final.
                if (isTimeReady && isProgressReady && x < fillTarget)
                {
                    // O pitch cresce de startPitch para endPitch baseado no valor da barra (x)
                    float currentPitch = Mathf.Lerp(startPitch, endPitch, x);
                    audioSource.pitch = currentPitch;

                    audioSource.PlayOneShot(fillSoundClip);

                    // ATUALIZAÇÃO: Define o tempo que o próximo som poderá ser tocado
                    _nextSoundTime = Time.time + minInterval;

                    // NOVO: Atualiza o ponto de progresso do último som
                    _lastSoundFillAmount = x;
                }
            }
            // ================================================================

        }, fillTarget, dynamicFillDuration)
            .SetEase(fillEase)
            .SetDelay(fillStartDelay)
            .OnComplete(() =>
            {
                // Garante que a variável de controle seja resetada
                _nextSoundTime = 0f;
                _lastSoundFillAmount = 0f; // Resetar após o fim da animação

                if (fillTarget >= 0.99f)
                {
                    percentage.text = "Similarity 100.0%";
                }

                int stars = levelManager.EvaluateLevelRating();
                Debug.Log($"Finalizado! Estrelas conquistadas: {stars}");

                DisplayFinalFeedback(stars);
            });
    }

    private void OnDisable()
    {
        ResetFinalFeedbackUI();

        DOTween.Kill(progressBarContainer, complete: true);
        if (_leftSprite != null)
        {
            DOTween.Kill(_leftSprite.transform, complete: true);
        }

        // MATA TWEENS E PARA O SOM DA BARRA
        if (audioSource != null)
        {
            DOTween.Kill(audioSource);
            audioSource.Stop();
            audioSource.pitch = 1f;
        }

        // MATA TWEENS E PARA O SOM DAS ESTRELAS
        if (starAudioSource != null)
        {
            DOTween.Kill(starAudioSource);
            starAudioSource.Stop();
        }

        // Garante que o estado interno de controle de áudio seja resetado
        _nextSoundTime = 0f;
        _lastSoundFillAmount = 0f;
    }

    private void DisplayFinalFeedback(int stars)
    {
        // 1. FADE DO OVERLAY DE FUNDO
        Sequence finalSequence = DOTween.Sequence();

        if (backgroundOverlay != null)
        {
            finalSequence.Append(backgroundOverlay.DOFade(targetOverlayOpacity, overlayFadeDuration));
        }

        // 2. HABILITAR E ANIMAR MENSAGEM
        RectTransform messageRect = GetMessageRectForStars(stars);

        if (messageRect != null)
        {
            finalSequence.AppendCallback(() =>
            {
                messageRect.gameObject.SetActive(true);

                if (stars > 0)
                {
                    // Sucesso (1, 2, 3 estrelas): Punch Scale
                    messageRect.localScale = Vector3.one;
                    messageRect.DOPunchScale(Vector3.one * messagePunchScale, punchDuration, punchVibrato, punchElasticity);
                }
                else // 0 estrelas (Falha)
                {
                    // Falha (0 estrelas): Scale up com Fade In
                    messageRect.localScale = Vector3.one * 0.5f;

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
        finalSequence.AppendInterval(finalDisplayDelay)
            .OnComplete(() =>
            {
                continueButton?.SetActive(true);
                playAgainButton?.SetActive(false);
                SaveLevel();
            });
    }

    private void SaveLevel()
    {
        stars = levelManager.ConvertSimilarityToStars(fillTarget);
        if (Level.SelectedLevel != null && stars > 0)
        {
            Level.SelectedLevel.Complete(stars);
        }
    }

    private RectTransform GetMessageRectForStars(int stars)
    {
        RectTransform targetRect = null;

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
        if (!_star3Animated && currentProgress >= 1)
        {
            _star3Animated = true;
            AnimateStar(3);

            // Opcional: Anima a 3ª estrela um pouco antes se o fillTarget for 1.0, 
            // mas o valor real não atinge 1.0 devido a precisão do float.
            // Para simplicidade, vou manter a lógica como está, já que 'currentProgress >= 1' 
            // no loop de tweening DOTween.To deve ser suficiente para o caso 100%.
        }
    }

    private void AnimateStar(int starNumber)
    {
        RectTransform starRect = null;
        AudioClip starClip = null;

        switch (starNumber)
        {
            case 1:
                starRect = star1Rect;
                starClip = star1Clip1; // 🌟 NOVO: Atribui o clip
                starAudioSource.volume = Volume1;
                starAudioSource.pitch = Pitch1;
                break;
            case 2:
                starRect = star2Rect;
                starClip = star2Clip; // 🌟 NOVO: Atribui o clip
                starAudioSource.volume = Volume2;
                starAudioSource.pitch = Pitch2;
                break;
            case 3:
                starRect = star3Rect;
                starClip = star3Clip; // 🌟 NOVO: Atribui o clip
                starAudioSource.volume = Volume3;
                starAudioSource.pitch = Pitch3;
                break;
        }

        if (starRect != null)
        {
            starRect.DOScale(Vector3.one * starPunchScale, 0.2f)
                .SetEase(Ease.OutCirc)
                .OnComplete(() =>
                {
                    starRect.DOScale(Vector3.one, 0.1f);
                });
        }

        // 🔊 NOVO: Toca o som da estrela
        if (starAudioSource != null && starClip != null)
        {
            starAudioSource.PlayOneShot(starClip);
        }
        // 🔊 FIM DO NOVO ÁUDIO
    }

    private void ResetFinalFeedbackUI()
    {
        // 1. Desativa mensagens e botão
        goodMessage?.gameObject.SetActive(false);
        excellentMessage?.gameObject.SetActive(false);
        perfectMessage?.gameObject.SetActive(false);
        failureMessage?.gameObject.SetActive(false);
        continueButton?.SetActive(false);
        playAgainButton?.SetActive(false);

        // 2. Reseta a opacidade do Overlay
        if (backgroundOverlay != null)
        {
            Color c = backgroundOverlay.color;
            backgroundOverlay.color = new Color(c.r, c.g, c.b, 0f);
        }

        // Reseta e desativa o texto de porcentagem
        if (percentage != null)
        {
            percentage.gameObject.SetActive(false);
            percentage.text = "Similarity 0.0%";
        }
    }
}