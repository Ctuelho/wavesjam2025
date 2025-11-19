// ObserversManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObserversManager : MonoBehaviour
{
    public WavesManager WavesManager;

    public float ObserverSize = 1f;

    public float TotalHeight { get; private set; }
    public float TotalWidth { get; private set; }

    private float _waveSize;
    private List<Observer> _currentObservers = new List<Observer>();

    private float _maxColumnHeight;

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

        transform.localScale = Vector3.one * ObserverSize;
    }

    public void Observe(List<GameObject> observerPrefabs)
    {
        if (observerPrefabs == null || observerPrefabs.Count == 0)
        {
            TotalHeight = 0f;
            TotalWidth = 0f;
            return;
        }

        foreach (var obs in _currentObservers)
        {
            if (obs != null)
                Destroy(obs.gameObject);
        }
        _currentObservers.Clear();

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

    private void RecalculateObserverPositions()
    {
        List<Observer> availableObservers = _currentObservers
            .Where(obs => obs != null && obs.CurrentSlot == null)
            .ToList();

        int count = availableObservers.Count;

        if (count == 0)
        {
            TotalHeight = 0f;
            TotalWidth = 0f;
            return;
        }

        int maxObserversPerColumn = Mathf.FloorToInt(_maxColumnHeight / ObserverSize);
        maxObserversPerColumn = Mathf.Max(1, maxObserversPerColumn);

        int numColumns = Mathf.CeilToInt((float)count / maxObserversPerColumn);

        float columnSpacing = ObserverSize * 0.1f;

        TotalWidth = numColumns * ObserverSize + (numColumns - 1) * columnSpacing;

        if (numColumns == 1) TotalWidth = ObserverSize;

        TotalHeight = Mathf.Min(count, maxObserversPerColumn) * ObserverSize;

        float baseOffsetX = -TotalWidth + ObserverSize;

        for (int i = 0; i < count; i++)
        {
            Observer observer = availableObservers[i];

            int indexInColumn = i % maxObserversPerColumn;

            int columnIndex = i / maxObserversPerColumn;

            float columnBaseY = (maxObserversPerColumn * ObserverSize / 2f) - (ObserverSize / 2f);
            float yPos = columnBaseY - (indexInColumn * ObserverSize);

            float xPos = columnIndex * (ObserverSize + columnSpacing);
            xPos += baseOffsetX;

            observer.transform.SetParent(this.transform);
            observer.transform.localPosition = new Vector3(xPos, yPos, 0);
            observer.transform.localScale = Vector3.one;
            observer.gameObject.name = $"Observer {i + 1} (Col {columnIndex})";
        }
    }
}