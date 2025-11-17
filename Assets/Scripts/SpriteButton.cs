using UnityEngine;
using UnityEngine.Events;

public class SpriteButton : MonoBehaviour
{
    public UnityEvent OnClick = new UnityEvent();

    [Header("Configurações Visuais (Opcional)")]
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color clickColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogError("SpriteButton Collider 2D is null!");
        }
    }

    private void OnMouseDown()
    {
        OnClick.Invoke();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = clickColor;
        }
    }
    private void OnMouseUp()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnMouseEnter()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hoverColor;
        }
    }

    private void OnMouseExit()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}