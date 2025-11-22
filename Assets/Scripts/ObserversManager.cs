using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObserversManager : MonoBehaviour
{
    public WavesManager WavesManager;

    [Header("Observer Settings")]
    public float ObserverSize = 1f;

    [Header("Layout")]
    [Tooltip("O espaçamento entre linhas e colunas dos slots de observer.")]
    public float SlotSpacing = 0.1f; // <-- RENOMEADO para refletir uso 2D

    [Header("Visual Feedback")]
    [Tooltip("Sprite a ser usado para o fundo da área de observers.")]
    public Sprite BackgroundSprite;
    public Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

    [Tooltip("Sprite a ser usado para os slots individuais dos observers.")]
    public Sprite SlotSprite;
    public Color SlotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

    public float TotalHeight { get; private set; }
    public float TotalWidth { get; private set; }

    private float _waveSize;
    private List<Observer> _currentObservers = new List<Observer>();
    private float _maxColumnHeight;

    private GameObject _backgroundInstance;
    private List<GameObject> _slotInstances = new List<GameObject>();


    public void Initialize(float waveSize, int gridSize)
    {
        _waveSize = waveSize;
        _maxColumnHeight = gridSize * _waveSize;

        foreach (var obs in _currentObservers)
        {
            if (obs != null)
                Destroy(obs.gameObject);
        }
        _currentObservers.Clear();
        TotalHeight = 0f;
        TotalWidth = 0f;

        ClearVisuals();

        transform.localScale = Vector3.one * ObserverSize;
    }

    public void Observe(List<GameObject> observerPrefabs)
    {
        foreach (var obs in _currentObservers)
        {
            if (obs != null)
                Destroy(obs.gameObject);
        }
        _currentObservers.Clear();

        ClearVisuals();

        int count = observerPrefabs.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject observerGO = Instantiate(observerPrefabs[i], transform);
            Observer observer = observerGO.GetComponent<Observer>();
            observer.manager = this;

            if (observer != null)
            {
                observerGO.transform.localScale = Vector3.one;
                _currentObservers.Add(observer);
                observerGO.name = $"Observer {i + 1}";
            }
        }

        RecalculateObserverPositions();
    }

    public void ReturnObserver(Observer observer)
    {
        if (observer == null)
            return;

        if (!_currentObservers.Contains(observer))
        {
            _currentObservers.Add(observer);
        }
        RecalculateObserverPositions();
    }

    private SpriteRenderer CreateSprite(string name, Sprite sprite, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(this.transform);
        go.transform.localPosition = Vector3.zero;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return sr;
    }

    private void ClearVisuals()
    {
        if (_backgroundInstance != null)
        {
            Destroy(_backgroundInstance);
            _backgroundInstance = null;
        }

        foreach (var slot in _slotInstances)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        _slotInstances.Clear();
    }

    private void RecalculateObserverPositions()
    {
        int totalObserverCount = _currentObservers.Count;

        List<Observer> availableObservers = _currentObservers
            .Where(obs => obs != null && obs.CurrentSlot == null)
            .ToList();

        ClearVisuals();

        if (totalObserverCount == 0)
        {
            TotalHeight = 0f;
            TotalWidth = 0f;
            return;
        }

        // --- CÁLCULO DE LAYOUT ---
        float spacing = SlotSpacing; // Usa o espaçamento único para linhas e colunas

        // 1. Cálculo da Altura Máxima da Coluna (agora considera o espaçamento vertical)
        // Observamos o tamanho da coluna sem o espaçamento entre elementos
        float maxAvailableVerticalSpace = _maxColumnHeight;

        // A altura de um elemento + espaçamento é (ObserverSize + spacing).
        // Contamos quantos blocos cabem, arredondando para baixo.
        int maxObserversPerColumn = Mathf.FloorToInt((maxAvailableVerticalSpace + spacing) / (ObserverSize + spacing));
        maxObserversPerColumn = Mathf.Max(1, maxObserversPerColumn);

        int numColumns = Mathf.CeilToInt((float)totalObserverCount / maxObserversPerColumn);

        // 2. Cálculo da Altura Total (Baseada na coluna mais alta)
        int numRows = Mathf.Min(totalObserverCount, maxObserversPerColumn); // Número de linhas na coluna mais alta

        // Altura Total: (Número de linhas * ObserverSize) + (Número de espaços entre linhas * Spacing)
        TotalHeight = numRows * ObserverSize + (numRows - 1) * spacing;

        // 3. Cálculo da Largura Total
        // Largura Total: (Número de colunas * ObserverSize) + (Número de espaços entre colunas * Spacing)
        TotalWidth = numColumns * ObserverSize + (numColumns - 1) * spacing;

        // Offset centralizado:
        float baseOffsetX = -(TotalWidth / 2f) + (ObserverSize / 2f);

        // --- Criação e Escala do Fundo da Área ---
        if (BackgroundSprite != null)
        {
            SpriteRenderer bgRenderer = CreateSprite("Background Area", BackgroundSprite, BackgroundColor, -2);
            _backgroundInstance = bgRenderer.gameObject;

            // O fundo deve ser exatamente do tamanho TotalWidth e TotalHeight calculados.
            // Não precisamos de padding adicional se quisermos que ele contenha APENAS os slots.

            float backgroundScaleX = TotalWidth / ObserverSize;
            float backgroundScaleY = TotalHeight / ObserverSize;

            // Centraliza o fundo no Y
            // O centro vertical é 0, já que a posição dos slots deve ser simétrica em relação ao centro (0) do ObserversManager.
            float backgroundYPos = 0f;

            _backgroundInstance.transform.localPosition = new Vector3(0, backgroundYPos, 0);

            // Aplica a escala exata
            _backgroundInstance.transform.localScale = new Vector3(backgroundScaleX, backgroundScaleY, 1f);
        }
        // --- FIM Fundo da Área ---

        // Loop 1: Cria todos os slots visuais (totalObserverCount)
        for (int i = 0; i < totalObserverCount; i++)
        {
            int indexInColumn = i % maxObserversPerColumn;
            int columnIndex = i / maxObserversPerColumn;

            // --- POSICIONAMENTO Y AJUSTADO (agora usa TotalHeight para centralizar corretamente) ---

            // Encontra o topo da coluna de slots
            float columnTop = TotalHeight / 2f;

            // Calcula o ponto inicial para o primeiro slot (Topo - metade do ObserverSize)
            float startY = columnTop - (ObserverSize / 2f);

            // Posição Y com espaçamento vertical:
            float yPos = startY - (indexInColumn * (ObserverSize + spacing));

            // Posição X com espaçamento horizontal:
            float xPos = columnIndex * (ObserverSize + spacing);
            xPos += baseOffsetX;

            // Criação do Slot Visual no local calculado
            if (SlotSprite != null)
            {
                SpriteRenderer slotRenderer = CreateSprite($"Slot_{i}", SlotSprite, SlotColor, -1);
                GameObject slotInstance = slotRenderer.gameObject;

                slotInstance.transform.localPosition = new Vector3(xPos, yPos, 0);
                slotInstance.transform.localScale = Vector3.one;

                _slotInstances.Add(slotInstance);
            }
        }

        // Loop 2: Reposiciona APENAS os Observers disponíveis
        for (int i = 0; i < availableObservers.Count; i++)
        {
            Observer observer = availableObservers[i];

            int indexInColumn = i % maxObserversPerColumn;
            int columnIndex = i / maxObserversPerColumn;

            // Calcula a posição Y e X novamente, garantindo que o Observer fique exatamente sobre o Slot
            float columnTop = TotalHeight / 2f;
            float startY = columnTop - (ObserverSize / 2f);
            float yPos = startY - (indexInColumn * (ObserverSize + spacing));

            float xPos = columnIndex * (ObserverSize + spacing);
            xPos += baseOffsetX;

            observer.transform.SetParent(this.transform);
            observer.transform.localPosition = new Vector3(xPos, yPos, 0);
            observer.transform.localScale = Vector3.one;
            observer.gameObject.name = $"Observer {i + 1} (Col {columnIndex})";
        }
    }
}