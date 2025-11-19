using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class Wave : MonoBehaviour, IPointerClickHandler
{
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
    public float CollapseSpeed = 0.1f;

    public SpriteRenderer Renderer;
    public Gradient ColorGradient;

    private float currentAlpha = 0.0f;
    public float AlphaFadeInSpeed = 0.5f;

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
    }

    public void SlowlyCollapse()
    {
        foreach (var influence in Influences)
        {
            if (influence.source != null)
            {
                targetCollapse += influence.value;
            }
        }
        targetCollapse += NeighborsInfluence.value;
        targetCollapse /= (float)(8 + Influences.Count);

        targetCollapse = Mathf.Clamp01(targetCollapse);
        Collapse = Mathf.Lerp(Collapse, targetCollapse, Time.deltaTime * CollapseSpeed);
        Collapse = Mathf.Clamp(Collapse, 0f, 1f);

        currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, Time.deltaTime * AlphaFadeInSpeed);
        currentAlpha = Mathf.Clamp01(currentAlpha);

        Color newColor = ColorGradient.Evaluate(Collapse);
        //newColor.a = currentAlpha;
        Renderer.color = newColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Influences.Add(new Influence() { source = this, value = 1 });
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            Influences.Add(new Influence() { source = this, value = 0 });
        }
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            ClearNullInfluences();
        }
    }
}