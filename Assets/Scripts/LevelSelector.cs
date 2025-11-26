using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems; // Adicionado para usar OnPointerClick

public class LevelSelector : MonoBehaviour
{
    public GameObject confirmPanel;
    public Level firstLevel;

    [Header("Scroll References")]
    [Tooltip("O componente ScrollRect que contém os níveis.")]
    public ScrollRect scrollView;
    [Tooltip("O RectTransform do Content (o pai dos botões de nível).")]
    public RectTransform contentRect;
    [Tooltip("O RectTransform do Viewport (a máscara visível).")]
    public RectTransform viewportRect;


    private void OnEnable()
    {
        confirmPanel.SetActive(false);
    }

    private void OnDisable()
    {
        //Level.SelectedLevel = null;
    }

    private void Update()
    {
        confirmPanel.SetActive(Level.SelectedLevel != null);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        StartCoroutine(CallTryOpenLevelsNextFrame());
    }

    private IEnumerator CallTryOpenLevelsNextFrame()
    {
        yield return null;
        yield return null;

        Level.TryOpenLevels();

        List<Level> levels = GetComponentsInChildren<Level>(true).ToList();

        // Tenta encontrar o último Level que está aberto (IsOpen).
        Level lastOpenLevel = levels
            .OrderBy(l => l.transform.GetSiblingIndex())
            .LastOrDefault(l => l.IsOpen);

        Level levelToSelect = lastOpenLevel ?? firstLevel;

        if (levelToSelect != null)
        {
            // 1. Simula o clique no Level
            levelToSelect.OnPointerClick(null);

            // 2. Espera um frame para garantir que o layout/selected level foi atualizado
            yield return null;

            // 3. Centraliza a view.
            CenterSelectedLevelInScrollView(levelToSelect.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Ajusta a posição do ScrollView para centralizar o item selecionado.
    /// </summary>
    /// <param name="selectedItem">O RectTransform do Level selecionado.</param>
    public void CenterSelectedLevelInScrollView(RectTransform selectedItem)
    {
        if (scrollView == null || contentRect == null || viewportRect == null)
        {
            Debug.LogError("Scroll View references (ScrollRect, ContentRect, or ViewportRect) missing in LevelSelector.");
            return;
        }

        // 1. Obter dimensões
        float viewportHeight = viewportRect.rect.height;
        float contentHeight = contentRect.rect.height;

        // Posição local Y do item dentro do Content (será negativo para items abaixo do topo).
        float itemLocalPosY = selectedItem.localPosition.y;

        // 2. Calcular o Scroll Necessário

        // O valor ideal de scroll (anchoredPosition.y) para trazer o centro do item para o centro do Viewport.
        // A lógica de UI ScrollViews (contentRect.anchoredPosition.y) é:
        // - Valor 0: Content está no topo.
        // - Valores positivos: Content se move para CIMA (o scroll move para BAIXO).

        // Calcula a posição de rolagem necessária para colocar o centro do item em y=0 do Viewport.
        float requiredScroll = -itemLocalPosY;

        // Ajusta o scroll para centralizar o item no meio da Viewport, e não no topo.
        requiredScroll -= (viewportHeight / 2f);

        // Adiciona um ajuste para garantir que o CENTRO do item seja alinhado,
        // corrigindo o cálculo base com o pivot do item (se não for 0.5).
        float pivotAdjustment = (0.5f - selectedItem.pivot.y) * selectedItem.rect.height;
        requiredScroll -= pivotAdjustment;

        // 3. Limitar o Scroll

        // O máximo de rolagem vertical.
        float maxScroll = Mathf.Max(0, contentHeight - viewportHeight);

        // Clamp (limita) a nova posição de rolagem entre 0 (topo) e maxScroll (fundo).
        float newScrollPos = Mathf.Clamp(requiredScroll, 0f, maxScroll);

        // 4. Aplicar a Nova Posição
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, newScrollPos);
    }

    public void ClickStart()
    {
        LevelManager.SetLevelData(Level.SelectedLevel);
    }
}