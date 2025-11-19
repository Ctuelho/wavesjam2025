using UnityEngine;
using UnityEngine.EventSystems;

public class MouseHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Renderer objRenderer;
    private Color corOriginal = Color.white;
    private Color corHover = Color.yellow;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();

        if (objRenderer != null)
        {
            corOriginal = objRenderer.material.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (objRenderer != null)
        {
            objRenderer.material.color = corHover;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (objRenderer != null)
        {
            objRenderer.material.color = corOriginal;
        }
    }
}