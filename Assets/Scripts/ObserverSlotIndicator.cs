using UnityEngine;

public class ObserverSlotIndicator : MonoBehaviour
{
    // Cores configuráveis no Inspector
    public Color InteractableColor = Color.green;
    public Color NotInteractableColor = Color.red;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("ObserverSlotIndicator requires a SpriteRenderer component.");
            enabled = false;
        }
    }

    /// <summary>
    /// Atualiza a cor do indicador com base na presença de Observers no slot.
    /// </summary>
    /// <param name="hasObserver">True se houver pelo menos um Observer no slot.</param>
    public void SetSlotState(bool hasObserver)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = hasObserver ? InteractableColor : NotInteractableColor;
    }
}