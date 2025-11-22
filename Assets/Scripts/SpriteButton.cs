using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Implementa todas as interfaces IPointer
public class SpriteButton : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    public UnityEvent OnClick = new UnityEvent();

    [Header("Colors")]
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color clickColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Rastreamento de Estado
    private bool isPointerOver = false;
    private bool isPressed = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Armazena a cor inicial
            originalColor = spriteRenderer.color;
        }

        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogError("SpriteButton requer um Collider 2D para funcionar com IPointer!");
        }
    }

    // Adiciona uma função de limpeza para restaurar a cor quando o objeto é desativado ou destruído.
    private void OnDisable()
    {
        // Garante que a cor volte ao normal caso o componente seja desativado enquanto estava em hover/clique.
        RestoreOriginalColor();
    }

    // --- Métodos de Controle de Cor ---

    private void SetColor(Color targetColor)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
        }
    }

    private void RestoreOriginalColor()
    {
        SetColor(originalColor);
    }

    // --- Implementação IPointer ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        // Se já estiver pressionado (Down) e o mouse voltar, mantenha a cor de clique.
        if (isPressed) return;

        SetColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        // Se o mouse sair, restaure a cor original,
        // mesmo que estivesse pressionado.
        RestoreOriginalColor();
        isPressed = false; // Força a saída do estado "pressionado" se o ponteiro sair
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        SetColor(clickColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        // Se o mouse for liberado E AINDA ESTIVER POR CIMA, volta para Hover.
        if (isPointerOver)
        {
            SetColor(hoverColor);
        }
        // Se o mouse for liberado E ESTIVER FORA (o que pode acontecer se outro elemento roubou o clique),
        // a cor já deveria ter sido restaurada pelo OnPointerExit, mas restauramos por segurança.
        else
        {
            RestoreOriginalColor();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // O OnPointerClick só é chamado após o OnPointerUp bem-sucedido.
        OnClick.Invoke();

        // O estado de cor é gerenciado por OnPointerUp, não precisamos de lógica de cor aqui.
    }
}