using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// A classe Observer precisa ter a propriedade CurrentSlot
// public class Observer : MonoBehaviour { public object CurrentSlot; public ObserversManager manager; /* ... */ }

public class ObserversManager : MonoBehaviour
{
    public WavesManager WavesManager;

    [Header("Observer Settings")]
    public float ObserverSize = 1f;

    [Header("Layout")]
    [Tooltip("O espaçamento entre linhas e colunas dos slots de observer.")]
    public float SlotSpacing = 0.1f;

    [Header("Visual Feedback")]
    [Tooltip("Sprite a ser usado para o fundo da área de observers.")]
    public Sprite BackgroundSprite;
    public Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

    [Tooltip("Sprite a ser usado para os slots individuais dos observers.")]
    public Sprite SlotSprite;
    public Color SlotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

    // --- PROPRIEDADES DO OBSERVE ACTOR (REFERÊNCIA EXTERNA) ---
    [Header("Observe Actor (Reference)")]
    [Tooltip("Referência ao SpriteRenderer do ator JÁ EXISTENTE na cena.")]
    public SpriteRenderer ObserveActorRenderer; // Referência direta
    public float ActorSpacing = 1.0f;
    public float ActorWidth = 3.0f;
    public float ActorHeight = 1.0f;
    public Color InteractableColor = Color.green;
    public Color NotInteractableColor = Color.red;
    // ---------------------------------------------------

    public float TotalHeight { get; private set; }
    public float TotalWidth { get; private set; }

    private float _waveSize;
    private List<Observer> _currentObservers = new List<Observer>();
    private float _maxColumnHeight;

    private GameObject _backgroundInstance;
    private List<GameObject> _slotInstances = new List<GameObject>();


    public void Initialize(float waveSize, int gridSize)
    {
        gameObject.SetActive(true);

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
            // observer.manager = this; 

            if (observer != null)
            {
                observerGO.transform.localScale = Vector3.one;
                _currentObservers.Add(observer);
                observerGO.name = $"Observer {i + 1}";
                observer.manager = this;
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

        // Chamada de atualização do estado
        UpdateObserveActorState();
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

        if (ObserveActorRenderer != null)
        {
            ObserveActorRenderer.transform.parent = null;
        }
    }

    private void RecalculateObserverPositions()
    {
        int totalObserverCount = _currentObservers.Count;

        List<Observer> availableObservers = _currentObservers
            // Substitua 'object' pelo tipo real do seu slot, se necessário.
            .Where(obs => obs != null /*&& obs.CurrentSlot == null*/)
            .ToList();

        ClearVisuals();

        if (totalObserverCount == 0)
        {
            TotalHeight = 0f;
            TotalWidth = 0f;

            if (ObserveActorRenderer != null)
            {
                ObserveActorRenderer.gameObject.SetActive(false);
            }
            return;
        }

        if (ObserveActorRenderer != null)
        {
            ObserveActorRenderer.gameObject.SetActive(true);
            ObserveActorRenderer.transform.SetParent(this.transform);
        }

        // --- 1. CÁLCULO DE LAYOUT DA ÁREA DOS OBSERVERS (SLOTS) ---
        float spacing = SlotSpacing;
        float maxAvailableVerticalSpace = _maxColumnHeight;

        int maxObserversPerColumn = Mathf.FloorToInt((maxAvailableVerticalSpace + spacing) / (ObserverSize + spacing));
        maxObserversPerColumn = Mathf.Max(1, maxObserversPerColumn);

        int numColumns = Mathf.CeilToInt((float)totalObserverCount / maxObserversPerColumn);
        int numRows = Mathf.Min(totalObserverCount, maxObserversPerColumn);

        // Dimensões REAIS da área dos slots.
        float observersAreaHeight = numRows * ObserverSize + (numRows - 1) * spacing;
        float observersAreaWidth = numColumns * ObserverSize + (numColumns - 1) * spacing;

        // --- 2. CÁLCULO DE DIMENSÕES FINAIS E DESLOCAMENTO ---

        // Largura Final: Máximo entre a largura dos Observers e a largura do Actor
        TotalWidth = Mathf.Max(observersAreaWidth, ActorWidth);

        // Altura Total da Estrutura (Slots + Spacing + Actor)
        float totalStructureHeight = observersAreaHeight + ActorHeight + ActorSpacing;
        TotalHeight = totalStructureHeight;

        // Deslocamento para centralizar a estrutura TOTAL (Actor + Slots) em Y=0.
        // O ponto mais alto da estrutura (topo do Actor) está em: totalStructureHeight / 2
        // O ponto mais baixo (base do último slot) está em: -observersAreaHeight / 2f
        // O centro da estrutura inteira é 0 no sistema de coordenadas do ObserversManager.

        // Deslocamento do centro da ÁREA DOS OBSERVERS em relação ao Y=0:
        // A área dos observers deve ser empurrada para baixo em (ActorHeight + ActorSpacing) / 2
        // para que o centro da estrutura total fique em Y=0.
        float observersAreaCenterYOffset = -(ActorHeight + ActorSpacing) / 2f;


        // Offset centralizado para slots e observers (baseado na largura dos slots)
        float baseOffsetX = -(observersAreaWidth / 2f) + (ObserverSize / 2f);

        // --- 3. POSICIONAMENTO DO OBSERVE ACTOR (Referência Externa) ---
        if (ObserveActorRenderer != null)
        {
            ObserveActorRenderer.transform.localScale = new Vector3(ActorWidth, ActorHeight, 1f);

            // Posição Y: O topo da área dos observers (observersAreaHeight / 2f) + o espaçamento
            // Posição Y no sistema de coordenadas do ObserversManager (Y=0 no centro da ESTRUTURA)
            float actorYPos = (observersAreaHeight / 2f) + ActorSpacing + (ActorHeight / 2f);

            // Aplicamos o deslocamento vertical (offset) para centralizar
            actorYPos += observersAreaCenterYOffset;

            ObserveActorRenderer.transform.localPosition = new Vector3(0f, actorYPos, 0);
        }


        // --- 4. CRIAÇÃO E ESCALA DO FUNDO DA ÁREA (Ajustado) ---
        if (BackgroundSprite != null)
        {
            SpriteRenderer bgRenderer = CreateSprite("Background Area", BackgroundSprite, BackgroundColor, -2);
            _backgroundInstance = bgRenderer.gameObject;

            // O fundo Cobre APENAS a área dos slots.
            float backgroundScaleX = observersAreaWidth / ObserverSize;
            float backgroundScaleY = observersAreaHeight / ObserverSize;

            // O centro vertical do background deve ser o centro da ÁREA DOS SLOTS
            // (o offset total da área)
            float backgroundYPos = observersAreaCenterYOffset;

            _backgroundInstance.transform.localPosition = new Vector3(0, backgroundYPos, 0);
            _backgroundInstance.transform.localScale = new Vector3(backgroundScaleX, backgroundScaleY, 1f);
        }


        // --- 5. POSICIONAMENTO DE SLOTS E OBSERVERS ---

        float columnTop = observersAreaHeight / 2f;
        float startY = columnTop - (ObserverSize / 2f);

        for (int i = 0; i < totalObserverCount; i++)
        {
            int indexInColumn = i % maxObserversPerColumn;
            int columnIndex = i / maxObserversPerColumn;

            // Posição Y base (sem offset)
            float yPosBase = startY - (indexInColumn * (ObserverSize + spacing));

            // Posição Y final (aplicando o deslocamento de centralização)
            float yPosFinal = yPosBase + observersAreaCenterYOffset;

            float xPos = columnIndex * (ObserverSize + spacing);
            xPos += baseOffsetX;

            // Criação do Slot Visual
            if (SlotSprite != null)
            {
                SpriteRenderer slotRenderer = CreateSprite($"Slot_{i}", SlotSprite, SlotColor, -1);
                GameObject slotInstance = slotRenderer.gameObject;

                slotInstance.transform.localPosition = new Vector3(xPos, yPosFinal, 0);
                slotInstance.transform.localScale = Vector3.one;

                _slotInstances.Add(slotInstance);
            }

            // Reposiciona APENAS os Observers disponíveis
            if (i < availableObservers.Count)
            {
                Observer observer = availableObservers[i];

                observer.transform.SetParent(this.transform);
                observer.transform.localPosition = new Vector3(xPos, yPosFinal, 0);
                observer.transform.localScale = Vector3.one;
                observer.gameObject.name = $"Observer {i + 1} (Col {columnIndex})";
            }
        }

        UpdateObserveActorState();
    }

    public void UpdateObserveActorState()
    {
        if (ObserveActorRenderer == null) return;

        bool isInteractable = _currentObservers.Any(o => o.CurrentSlot != null);

        ObserveActorRenderer.color = isInteractable ? InteractableColor : NotInteractableColor;
    }
}