using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObserversManager : MonoBehaviour
{
    public float ObserverSize = 1f;

    public float TotalHeight { get; private set; }

    private float _waveSize;
    private List<Observer> _currentObservers = new List<Observer>();

    public void Initialize(float waveSize)
    {
        _waveSize = waveSize;
        foreach (var obs in _currentObservers)
        {
            if (obs != null)
                Destroy(obs.gameObject);
        }
        _currentObservers.Clear();
        TotalHeight = 0f;

        transform.localScale = Vector3.one * ObserverSize;
    }

    public void Observe(List<GameObject> observerPrefabs)
    {
        if (observerPrefabs == null || observerPrefabs.Count == 0)
        {
            TotalHeight = 0f;
            return;
        }

        int count = observerPrefabs.Count;
        TotalHeight = count * ObserverSize;

        float startY = (TotalHeight / 2f) - (ObserverSize / 2f);

        for (int i = 0; i < count; i++)
        {
            GameObject observerGO = Instantiate(observerPrefabs[i], transform);
            Observer observer = observerGO.GetComponent<Observer>();
            observer.manager = this;

            if (observer != null)
            {
                float yPos = startY - (i * ObserverSize);

                observerGO.transform.localPosition = new Vector3(0, yPos, 0);
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
        // 1. Cria uma nova lista temporária (availableObservers) com APENAS os observadores
        // da lista mestre que estão disponíveis (CurrentSlot == null).
        List<Observer> availableObservers = _currentObservers
            .Where(obs => obs != null && obs.CurrentSlot == null)
            .ToList();

        int count = availableObservers.Count;
        TotalHeight = count * ObserverSize;

        if (count == 0) return;

        float startY = (TotalHeight / 2f) - (ObserverSize / 2f);

        for (int i = 0; i < count; i++)
        {
            Observer observer = availableObservers[i];
            float yPos = startY - (i * ObserverSize);

            observer.transform.SetParent(this.transform);
            observer.transform.localPosition = new Vector3(0, yPos, 0);
            observer.transform.localScale = Vector3.one;
            observer.gameObject.name = $"Observer {i + 1}";
        }
    }
}