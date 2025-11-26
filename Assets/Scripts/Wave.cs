using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UIElements.Experimental; // NECESSÁRIO PARA DOTWEEN E O NOVO ENUM EASE

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
    private float targetCollapse = 0.0f;

    [Header("Collapse Settings (Preview)")]
    public float CollapseSpeed = 0.1f;
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

    public void PreviewCollapse()
    {
        targetCollapse = 0.0f;

        foreach (var influence in Influences)
        {
            if (influence.source != null)
            {
                targetCollapse += influence.value;
            }
        }

        targetCollapse += NeighborsInfluence.value;

        float divisor = (float)CurrentNeighborCount + Influences.Count;

        if (divisor > 0)
        {
            targetCollapse /= divisor;
        }
        else
        {
            targetCollapse = 0.0f;
        }

        float t = Time.deltaTime * CollapseSpeed;
        t = Mathf.Clamp01(t);

        float easedT = DOVirtual.EasedValue(0, 1, t, PreviewEaseType);

        targetCollapse = Mathf.Clamp01(targetCollapse);
        Collapse = Mathf.Lerp(Collapse, targetCollapse, easedT);
        Collapse = Mathf.Clamp01(Collapse);

        Color newColor = ColorGradient.Evaluate(Collapse);
        Renderer.color = newColor;
    }

    private float FindNearestDiscreteTarget()
    {
        if (DiscreteCollapseTargets == null || DiscreteCollapseTargets.Length == 0)
        {
            return Collapse;
        }

        float nearestTarget = DiscreteCollapseTargets[0];
        float minDifference = Mathf.Abs(Collapse - nearestTarget);

        foreach (float target in DiscreteCollapseTargets)
        {
            float difference = Mathf.Abs(Collapse - target);

            if (difference < minDifference)
            {
                minDifference = difference;
                nearestTarget = target;
            }
        }
        return nearestTarget;
    }

    private Vector3 originalPosition;
    public Ease ToEase = Ease.Linear;
    // MÉTODO MODIFICADO
    public void CollapseNow(float delay = 0)
    {
        Collapse = targetCollapse;
        originalPosition = transform.localPosition;

        // 0. Interrompe tweens anteriores (importante para evitar jumps e conflitos de cor)
        DOTween.Kill(this.transform);

        // 1. Encontrar o alvo de colapso discreto
        float finalCollapseTarget = FindNearestDiscreteTarget();

        //if (Collapse < 0.3f)
        //    Collapse = 0.3f;

        // 2. TWEEN DO VALOR DE COLAPSO (Collapse) E DA COR
        // Faz o tween do Collapse do valor atual para o alvo final
        DOTween.To(() => Collapse, x => Collapse = x, finalCollapseTarget, ShakeDuration+ delay)
            .SetEase(ToEase) // Mantenha Linear para uma transição de valor suave e contínua
            .OnUpdate(() =>
            {
                // A. ATUALIZA A COR: A cada frame do tween, atualiza a cor
                // usando o valor de Collapse tweenado e o TargetCollapseGradient.
                Renderer.color = TargetCollapseGradient.Evaluate(Collapse);
            })
            .OnComplete(() =>
            {
                // Garante que o valor final seja exatamente o alvo discreto
                Collapse = finalCollapseTarget;
                // A cor final já foi definida no último OnUpdate.
            })
            .SetTarget(this); // Target no script para Kill futuros

        // 3. SHAKE DE POSIÇÃO (corre em paralelo com o tween de valor/cor)
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
            if(finalCollapseTarget > 0)
            {
                audioSource.Play();
            }            
        });
    }

    //public void OnPointerClick(PointerEventData eventData)
    //{
    // ... (código desativado)
    //}
}