using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObserversManager : MonoBehaviour
{
    public WavesManager WavesManager;

    [Header("Observer Settings")]
    public float ObserverSize = 1f;

    [Header("Layout")]
    [Tooltip("O espaçamento entre as colunas dos slots de observer.")]
    public float ColumnSpacing = 0.1f;

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

    // Referências visuais
    private GameObject _backgroundInstance;
    private List<GameObject> _slotInstances = new List<GameObject>();


    public void Initialize(float waveSize, int gridSize)
    {
        _waveSize = waveSize;
        _maxColumnHeight = gridSize * _waveSize;

        // Destruição dos Observers existentes
        foreach (var obs in _currentObservers)
        {
            if (obs != null)
                Destroy(obs.gameObject);
        }
        _currentObservers.Clear();
        TotalHeight = 0f;
        TotalWidth = 0f;

        // Limpa visuais antigos
        ClearVisuals();

        transform.localScale = Vector3.one * ObserverSize;
    }

    public void Observe(List<GameObject> observerPrefabs)
    {
        // ... (limpa observers existentes)
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
        int maxObserversPerColumn = Mathf.FloorToInt(_maxColumnHeight / ObserverSize);
        maxObserversPerColumn = Mathf.Max(1, maxObserversPerColumn);

        int numColumns = Mathf.CeilToInt((float)totalObserverCount / maxObserversPerColumn);

        float columnSpacing = ColumnSpacing;

        // Largura total ocupada: (Número de colunas * ObserverSize) + (Número de espaços entre colunas * Spacing)
        TotalWidth = numColumns * ObserverSize + (numColumns - 1) * columnSpacing;

        // Altura total ocupada (Baseada no número de Observers na coluna mais alta)
        TotalHeight = Mathf.Min(totalObserverCount, maxObserversPerColumn) * ObserverSize;

        // Offset centralizado:
        float baseOffsetX = -(TotalWidth / 2f) + (ObserverSize / 2f);

        // --- Criação e Escala do Fundo da Área ---
        if (BackgroundSprite != null)
        {
            SpriteRenderer bgRenderer = CreateSprite("Background Area", BackgroundSprite, BackgroundColor, -2);
            _backgroundInstance = bgRenderer.gameObject;

            // Calculamos o Padding para o fundo
            float paddingX = columnSpacing;
            float paddingY = ObserverSize * 0.1f;

            // A escala deve cobrir o TotalWidth/Height MAIS o Padding.
            float backgroundScaleX = (TotalWidth + paddingX) / ObserverSize;
            float backgroundScaleY = (TotalHeight + paddingY) / ObserverSize;

            // Centraliza o fundo no Y
            float backgroundYPos = 0f;

            _backgroundInstance.transform.localPosition = new Vector3(0, backgroundYPos, 0);

            // Aplica a escala ajustada
            _backgroundInstance.transform.localScale = new Vector3(backgroundScaleX, backgroundScaleY, 1f);
        }
        // --- FIM Fundo da Área ---


        // LOOP 1: Cria todos os slots visuais (totalObserverCount)
        for (int i = 0; i < totalObserverCount; i++)
        {
            int indexInColumn = i % maxObserversPerColumn;
            int columnIndex = i / maxObserversPerColumn;

            float columnBaseY = (_maxColumnHeight / 2f) - (ObserverSize / 2f);
            float yPos = columnBaseY - (indexInColumn * ObserverSize);

            // A posição X deve incluir o espaçamento
            float xPos = columnIndex * ObserverSize + columnIndex * columnSpacing;
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

        // LOOP 2: Reposiciona APENAS os Observers disponíveis
        for (int i = 0; i < availableObservers.Count; i++)
        {
            Observer observer = availableObservers[i];

            int indexInColumn = i % maxObserversPerColumn;
            int columnIndex = i / maxObserversPerColumn;

            float columnBaseY = (_maxColumnHeight / 2f) - (ObserverSize / 2f);
            float yPos = columnBaseY - (indexInColumn * ObserverSize);

            float xPos = columnIndex * ObserverSize + columnIndex * columnSpacing;
            xPos += baseOffsetX;

            // Ajusta a posição do Observer (usa as mesmas coordenadas do Slot)
            observer.transform.SetParent(this.transform);
            observer.transform.localPosition = new Vector3(xPos, yPos, 0);
            observer.transform.localScale = Vector3.one;
            observer.gameObject.name = $"Observer {i + 1} (Col {columnIndex})";
        }
    }
}