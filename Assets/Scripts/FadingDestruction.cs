using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controla o desvanecimento (fade-out) da transparência de um SpriteRenderer
/// e destrói o GameObject ao final da animação.
/// Implementa uma sequência de Flash: 0% -> 100% -> 0%.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FadingDestruction : MonoBehaviour
{
    public Color PositiveColor = Color.cyan;
    public Color NegativeColor = Color.red;

    [Header("Configuração")]
    [Tooltip("O tempo total que a animação de desvanecimento deve levar, em segundos.")]
    public float fadeDuration = 1.0f;

    // O Easing aqui agora se aplica APENAS ao estágio de DECAY (100% -> 0%)
    [Tooltip("O tipo de curva de aceleração/desaceleração da fase de decay.")]
    public Ease fadeEasing = Ease.OutQuad; // OutQuad ou OutSine são boas opções para decay suave.

    [Header("Flash Timing")]
    [Tooltip("A porcentagem da duração total gasta no 'Flash' (0% -> 100% alpha).")]
    [Range(0.01f, 0.5f)]
    public float flashPercentage = 0.2f; // Se você quer que o flash seja em 20% do tempo

    private SpriteRenderer _spriteRenderer;
    private float _initialAlpha = 0;
    private float _targetAlpha;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
        {
            Debug.LogError("O FadingDestruction requer um SpriteRenderer no mesmo GameObject.");
            Destroy(gameObject);
            return;
        }

        // 1. OBRIGATÓRIO: Setar a cor inicial para Transparente (0% Alpha)
        Color initialColor = _spriteRenderer.color;
        initialColor.a = 0f;
        _spriteRenderer.color = initialColor;

        StartFadeAndDestroySequence();
    }

    private void StartFadeAndDestroySequence()
    {
        // Calcula as durações baseadas na porcentagem
        float flashDuration = fadeDuration * flashPercentage;
        float decayDuration = fadeDuration * (1f - flashPercentage);

        Sequence sequence = DOTween.Sequence();

        // 2. PASSO 1 (FLASH-UP): Vai de 0% para 100% (quase instantâneo)
        // Usamos Ease.OutSine para um flash que acelera rapidamente.
        sequence.Append(_spriteRenderer.DOFade(_targetAlpha, flashDuration)
            .SetEase(Ease.OutSine));

        // 3. PASSO 2 (DECAY): Vai de 100% para 0% (resto do tempo)
        // Usa o Easing customizado para controlar a curva de decay.
        sequence.Append(_spriteRenderer.DOFade(0f, decayDuration)
            .SetEase(fadeEasing));

        // 4. Autodestruição ao final da sequência
        sequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });

        sequence.Play();
    }

    public void SetFadeProperties(float startAlpha, float finalAlpha)
    {
        //_initialAlpha = startAlpha;
        _targetAlpha = finalAlpha;
    }
}