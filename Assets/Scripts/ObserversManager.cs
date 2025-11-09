using UnityEngine;
using System.Collections.Generic;

public class ObserversManager : MonoBehaviour
{
    public float ObserverSize = 1f; // Tamanho padrão do Observer (e da coluna)

    public float TotalHeight { get; private set; }

    private float _waveSize;
    private List<Observer> _currentObservers = new List<Observer>();

    public void Initialize(float waveSize)
    {
        _waveSize = waveSize;
        // Destrói observers existentes para re-criação
        foreach (var obs in _currentObservers)
        {
            if (obs != null)
                Destroy(obs.gameObject);
        }
        _currentObservers.Clear();
        TotalHeight = 0f;

        // Garante que o ObserversManager tenha o tamanho de referência
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

        // O centro vertical do ObserversManager é (0, 0) (e o centro da Grid também)
        // Posição inicial (topo) = (TotalHeight / 2) - (ObserverSize / 2)
        float startY = (TotalHeight / 2f) - (ObserverSize / 2f);

        for (int i = 0; i < count; i++)
        {
            GameObject observerGO = Instantiate(observerPrefabs[i], transform);
            Observer observer = observerGO.GetComponent<Observer>();

            if (observer != null)
            {
                // Posição Y: startY - (i * ObserverSize)
                float yPos = startY - (i * ObserverSize);

                // Posição X é 0 porque ele está centrado horizontalmente no ObserversManager
                observerGO.transform.localPosition = new Vector3(0, yPos, 0);
                observerGO.transform.localScale = Vector3.one; // Reseta a escala local
                _currentObservers.Add(observer);
                observerGO.name = $"Observer {i + 1}";
            }
        }
    }
}