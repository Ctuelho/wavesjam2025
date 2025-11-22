using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class Wave : MonoBehaviour, IPointerClickHandler
{
    // 1. ENUM PARA TIPOS DE TWEENING
    public enum CollapseTweenType
    {
        Linear,     // Mathf.Lerp
        EaseIn,     // Início lento, fim rápido (Efeito de aceleração)
        EaseOut,    // Início rápido, fim lento (Padrão de suavização)
        SmoothStep  // Suave no início e no fim (curva em S)
    }

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

    [Header("Collapse Settings")]
    public float CollapseSpeed = 0.1f;
    public CollapseTweenType TweenType = CollapseTweenType.EaseOut; // Campo para selecionar o Tweening

    public SpriteRenderer Renderer;
    public Gradient ColorGradient;

    private float currentAlpha = 0.0f;
    public float AlphaFadeInSpeed = 0.5f;

    public int CurrentNeighborCount;

    private void Awake()
    {
        if (Renderer == null)
        {
            Renderer = GetComponent<SpriteRenderer>();
        }
        // Configuração inicial para Alpha 0.0f (pode ser necessário no SlowlyCollapse se você usar o Lerp para alpha)
        Renderer.color = new Color(Renderer.color.r, Renderer.color.g, Renderer.color.b, 0f);

        if (AddSelfInfluence)
        {
            AddInfluence(new Influence() { source = this, value = 0.0f });
        }
    }

    private void Update()
    {
    }

    // ... (Métodos AddInfluence, RemoveInfluence, ClearNullInfluences inalterados) ...
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
    }
    // ...

    /// <summary>
    /// Aplica o cálculo e interpolação do colapso usando o tipo de Tweening configurado.
    /// </summary>
    public void SlowlyCollapse()
    {
        // 1. CÁLCULO DO TARGET COLLAPSE (Inalterado)
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

        // 2. INTERPOLAÇÃO COM TWEENING

        // Fator 't' (determina quão perto estamos do target em um único frame)
        float t = Time.deltaTime * CollapseSpeed;

        // Usamos uma função auxiliar (EASE) para aplicar a curva no fator de interpolação 't'
        float easedT = GetEasedValue(t);

        // Interpolação e Clamp
        targetCollapse = Mathf.Clamp01(targetCollapse);
        Collapse = Mathf.Lerp(Collapse, targetCollapse, easedT);
        Collapse = Mathf.Clamp(Collapse, 0f, 1f);

        // 3. Lógica de Visualização (Inalterada)
        //currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, Time.deltaTime * AlphaFadeInSpeed);
        //currentAlpha = Mathf.Clamp01(currentAlpha);

        Color newColor = ColorGradient.Evaluate(Collapse);
        //newColor.a = currentAlpha; // Garante que o alpha também seja aplicado
        Renderer.color = newColor;
    }

    /// <summary>
    /// Retorna o valor de interpolação ajustado (t) baseado no tipo de Tweening.
    /// </summary>
    /// <param name="t">O valor base da interpolação (Time.deltaTime * CollapseSpeed).</param>
    /// <returns>O valor 't' suavizado.</returns>
    private float GetEasedValue(float t)
    {
        // Garante que 't' não exceda 1, mesmo que CollapseSpeed seja alto.
        t = Mathf.Clamp01(t);

        switch (TweenType)
        {
            case CollapseTweenType.Linear:
                return t; // Sem modificação

            case CollapseTweenType.EaseIn:
                // Começa lentamente (t próximo de 0), acelera (t² -> t³ -> t⁴)
                // Usando t * t (Quadrático) para um EaseIn simples
                return t * t;

            case CollapseTweenType.EaseOut:
                // Começa rapidamente (t próximo de 1), desacelera
                // Usando 1 - (1-t)² para um EaseOut simples
                return 1 - (1 - t) * (1 - t);

            case CollapseTweenType.SmoothStep:
                // SmoothStep (suave no início e no fim, rápido no meio)
                return t * t * (3f - 2f * t);

            default:
                return t;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Adiciona uma influência positiva (força o colapso para 1)
            Influences.Add(new Influence() { source = this, value = 1f });
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Adiciona uma influência negativa (força o colapso para 0)
            Influences.Add(new Influence() { source = this, value = -1f }); // Alterado para -1f
        }
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            ClearNullInfluences();
        }
    }
}