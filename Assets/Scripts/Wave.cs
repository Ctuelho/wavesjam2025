using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using DG.Tweening;

public class Wave : MonoBehaviour//, IPointerClickHandler
{
    // O enum CollapseTweenType foi removido e substituído por DG.Tweening.Ease

    [System.Serializable]
    public class Influence
    {
        public object source;
        public float value;
    }

    private bool AddSelfInfluence = true;
    public List<Influence> Influences = new List<Influence>();
    public Influence NeighborsInfluence = new Influence();
    public float Collapse = 0.0f;
    public float targetCollapse = 0.0f;
    public float realCollpase = 0.0f;

    [Header("Collapse Settings (Preview)")]
    public float CollapseSpeed = 0.1f;
    public float ColorFadeSpeed = 8.0f; // Campo existente
    // Campo atualizado para usar o enum Ease nativo do DOTween
    public Ease PreviewEaseType = Ease.OutQuad;

    [Header("Collapse Targets & Final State")]
    public float[] DiscreteCollapseTargets = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f }; // Alvos discretos
    public Gradient TargetCollapseGradient; // Gradiente para a cor final após o colapso

    [Header("DOTween Shake Settings")]
    public float ShakeDuration = 0.5f;
    public float ShakeStrength = 0.1f;
    public int ShakeVibrato = 10;
    public float ShakeRandomness = 90f;
    public bool ShakeFadeOut = true;

    public SpriteRenderer Renderer;
    public Gradient ColorGradient;

    private float currentAlpha = 0.0f;
    public float AlphaFadeInSpeed = 0.5f;

    public int CurrentNeighborCount;

    public AudioSource audioSource;

    private void Awake()
    {
        if (Renderer == null)
        {
            Renderer = GetComponent<SpriteRenderer>();
        }
        Renderer.color = new Color(Renderer.color.r, Renderer.color.g, Renderer.color.b, 0f);

        if (AddSelfInfluence)
        {
            AddInfluence(new Influence() { source = this, value = 0.0f });
        }
    }

    private void Update()
    {
    }

    public void AddInfluence(Influence newInfluence)
    {
        RemoveInfluence(newInfluence.source);
        Influences.Add(newInfluence);
    }

    public void RemoveInfluence(object from)
    {
        Influences.RemoveAll(i => i.source == from);
    }

    public void ClearNullInfluences()
    {
        Influences.Clear();
        NeighborsInfluence = new Influence();
        if (AddSelfInfluence)
        {
            AddInfluence(new Influence() { source = this, value = 0.0f });
        }
    }

    /// <summary>
    /// Calcula o valor de colapso alvo (targetCollapse) como a média real das influências.
    /// </summary>
    /// <returns>O valor de targetCollapse normalizado entre 0 e 1.</returns>
    private float CalculateCurrentTargetCollapse()
    {
        float totalInfluence = 0.0f;

        foreach (var influence in Influences)
        {
            if (influence.source != null)
            {
                totalInfluence += influence.value;
            }
        }

        totalInfluence += NeighborsInfluence.value;

        // O divisor deve ser o CurrentNeighborCount (vindo da WavesManager)
        // mais o número de outras influências (Observadores + SelfInfluence).
        //float divisor = (float)CurrentNeighborCount + Influences.Count;
        float divisor = 8 + Influences.Count;

        float calculatedTarget = 0.0f;

        if (divisor > 0)
        {
            calculatedTarget = totalInfluence / divisor;
        }
        // Se divisor for 0, calculatedTarget permanece 0.0f, o que está correto.

        return Mathf.Clamp01(calculatedTarget);
    }

    public void PreviewCollapse()
    {
        float realTarget = CalculateCurrentTargetCollapse();
        realCollpase = realTarget;
        targetCollapse = realTarget;
        float nearest = FindNearestDiscreteTarget();
        targetCollapse = nearest;

        // 1. TWEEN DO VALOR DE COLLAPSE
        float t = Time.deltaTime * CollapseSpeed;
        t = Mathf.Clamp01(t);

        float easedT = DOVirtual.EasedValue(0, 1, t, PreviewEaseType);

        Collapse = Mathf.Lerp(Collapse, targetCollapse, easedT);
        Collapse = Mathf.Clamp01(Collapse);

        // 2. TWEEN DA COR (AGORA SUAVIZADO)

        // A. Calcula a cor ALVO baseada no Collapse (que já está tweened)
        Color targetColor = ColorGradient.Evaluate(Collapse);

        // B. Define o fator de interpolação da cor usando o ColorFadeSpeed
        float colorLerpT = Time.deltaTime * ColorFadeSpeed;
        colorLerpT = Mathf.Clamp01(colorLerpT);

        // ✅ ALTERAÇÃO SOLICITADA: Aplica a suavização da cor
        Renderer.color = Color.Lerp(Renderer.color, targetColor, colorLerpT);
    }

    private float FindNearestDiscreteTarget()
    {
        if (DiscreteCollapseTargets == null || DiscreteCollapseTargets.Length == 0)
        {
            return Collapse;
        }

        float collapseValueToCheck = targetCollapse;

        float targetZero = DiscreteCollapseTargets[0];

        if (collapseValueToCheck <= targetZero)
        {
            return targetZero;
        }

        if (collapseValueToCheck < DiscreteCollapseTargets[1])
            return 0;

        // 2. ITERAÇÃO E PONTO MÉDIO SEQUENCIAL
        for (int i = 1; i < DiscreteCollapseTargets.Length; i++)
        {
            float targetAnterior = DiscreteCollapseTargets[i - 1];
            float proximoTarget = DiscreteCollapseTargets[i];

            // Se o valor já é maior que o alvo superior, continua para o próximo par.
            if (collapseValueToCheck > proximoTarget)
            {
                continue;
            }

            float pontoMedio = (targetAnterior + proximoTarget) / 2.0f;

            if (collapseValueToCheck > pontoMedio)
            {
                return proximoTarget;
            }
            else
            {
                return targetAnterior;
            }
        }

        return DiscreteCollapseTargets[DiscreteCollapseTargets.Length - 1];
    }

    private Vector3 originalPosition;
    public Ease ToEase = Ease.Linear;

    /// <summary>
    /// Colapsa a onda imediatamente, usando o valor REAL e atual das influências como ponto de partida para o tween final.
    /// </summary>
    public void CollapseNow(float delay = 0)
    {
        originalPosition = transform.localPosition;

        // 🛑 MUDANÇA CRÍTICA: Recalcula o alvo real e ATUALIZA o Collapse para esse valor.
        // Isso força a onda a "começar" o colapso a partir da média não-tweened das influências.
        float startingCollapse = CalculateCurrentTargetCollapse();
        Collapse = startingCollapse;

        // 0. Interrompe tweens anteriores
        DOTween.Kill(this.transform, true); // True para completar o Shake anterior imediatamente

        // 1. Encontrar o alvo de colapso discreto final
        // O valor base para encontrar o alvo discreto é o 'startingCollapse' (que foi o alvo real)
        // Nota: O método FindNearestDiscreteTarget usa o campo targetCollapse, que não foi atualizado 
        // para o startingCollapse aqui, mas usarei o valor de collapse (que foi atualizado) para 
        // garantir que a lógica funcione com o targetCollapse.
        targetCollapse = startingCollapse;
        float finalCollapseTarget = FindNearestDiscreteTarget();

        // 2. TWEEN DO VALOR DE COLAPSO (Collapse) E DA COR
        // Faz o tween do Collapse do valor 'startingCollapse' (o valor real) para o alvo final discreto
        DOTween.To(() => Collapse, x => Collapse = x, finalCollapseTarget, ShakeDuration + delay)
            .SetEase(ToEase)
            .SetTarget(this.transform) // Target no transform para Kill futuros
            .OnUpdate(() =>
            {
                // A. ATUALIZA A COR: Usa o TargetCollapseGradient, pois estamos no estado de colapso final.
                // Este tween de cor TEM prioridade sobre o Color.Lerp do PreviewCollapse()
                Renderer.color = TargetCollapseGradient.Evaluate(Collapse);
            })
            .OnComplete(() =>
            {
                // Garante que o valor final seja exatamente o alvo discreto
                Collapse = finalCollapseTarget;
                Renderer.color = TargetCollapseGradient.Evaluate(Collapse);
            });

        // 3. SHAKE DE POSIÇÃO (corre em paralelo)
        transform.DOShakePosition(
            ShakeDuration + delay,
            ShakeStrength,
            ShakeVibrato,
            ShakeRandomness,
            ShakeFadeOut
        ).OnComplete(() =>
        {
            // Garante que a posição volte ao zero exato.
            transform.localPosition = originalPosition;
            if (finalCollapseTarget > 0)
            {
                if (audioSource != null)
                {
                    audioSource.Play();
                }
            }
        });
    }
}