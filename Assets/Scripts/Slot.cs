using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour
{
    // ... (variáveis existentes)
    public int GridX;
    public int GridY;
    public Observer CurrentObserver;
    public CornerType Corner = CornerType.None;
    public DirectionType Direction = DirectionType.None;

    public GameObject ObserverControllerPrefab;
    private GameObject _currentObserverController;

    private WavesManager _manager; // Assumimos que tem acesso a 'gridSize'

    public enum DecayType { DoesNotDecay = 0, VerySlow = 2, Slow = 3, Medium = 4, Fast = 5, VeryFast = 6 }
    public DecayType CurrentDecayType = DecayType.DoesNotDecay;
    public int currentRange; // <--- Variável do slot para controlar o range

    public enum CornerType { None = 0, TopLeft = 1, TopRight = 2, BottomLeft = 3, BottomRight = 4 }
    public enum DirectionType { None, Up, Down, Left, Right, Diagonal_UpLeft, Diagonal_UpRight, Diagonal_DownLeft, Diagonal_DownRight }

    public void Initialize(int x, int y, DirectionType direction, WavesManager manager, CornerType corner = CornerType.None)
    {
        GridX = x;
        GridY = y;
        Corner = corner;
        Direction = direction;
        _manager = manager;
        gameObject.name = $"Slot ({x}, {y}) - {Corner} - {Direction}";
    }

    public void AssignObserver(Observer observer)
    {
        if (CurrentObserver != null)
        {
            CurrentObserver.CurrentSlot = null;
        }

        CurrentObserver = observer;
        if (CurrentObserver != null)
        {
            CurrentDecayType = CurrentObserver.decay;
            CurrentObserver.CurrentSlot = this;
            CurrentObserver.manager.UpdateObserveActorState();
            CurrentObserver.transform.position = transform.position;

            // 🆕 NOVO: Inicializa currentRange com o gridSize do WavesManager
            if (_manager != null)
            {
                // Assumindo que _manager.GridSize existe e é o valor máximo desejado.
                currentRange = WavesManager.GridSize;
                Debug.Log($"Slot ({GridX}, {GridY}) Range inicializado para: {currentRange}");
            }
            else
            {
                currentRange = 1;
            }

            UpdateObserverRotation(CurrentObserver.transform);            
            //if (CurrentObserver.influenceType == Observer.InfluenceType.Line)
            //{
            //    UpdateObserverRotation(CurrentObserver.transform);
            //}
            //else
            //{
            //    // Se for Radius, garante que a rotação seja zero.
            //    CurrentObserver.transform.rotation = Quaternion.identity;
            //}

            InstantiateEffectObject(CurrentObserver.transform);

            if (_manager != null)
            {
                // Chamada agora vai para o dispatcher no WavesManager
                _manager.ApplyObserverInfluenceAndShowEffects(CurrentObserver, this, CurrentObserver.WaveEffectPrefab);
            }
        }
    }

    public void RemoveObserver()
    {
        if (CurrentObserver != null)
        {
            // OMITIDO: Chamada a DestroyWaveEffects(), pois o ciclo de vida é externo.

            DestroyEffectObject();

            if (_manager != null)
            {
                _manager.RemoveInfluenceSource(CurrentObserver);
            }

            CurrentObserver.transform.rotation = Quaternion.identity;
            CurrentObserver.CurrentSlot = null;
            CurrentObserver.transform.SetParent(null);
            CurrentObserver = null;
            currentRange = 0; // 🆕 Limpa o range ao remover
        }
    }

    // 🔄 Método para ciclar o tipo de decaimento (DecayType)
    public void CycleDecayType()
    {
        if (CurrentObserver == null) return;

        int nextDecay = ((int)CurrentDecayType + 1) % (System.Enum.GetValues(typeof(DecayType)).Length);
        CurrentDecayType = (DecayType)nextDecay;
        CurrentObserver.decay = CurrentDecayType; // 🆕 Atualiza o Observer

        //Debug.Log($"Slot ({GridX}, {GridY}) Decay alterado para: {CurrentDecayType}");

        if (_manager != null)
        {
            _manager.ApplyObserverInfluenceAndShowEffects(CurrentObserver, this, CurrentObserver.WaveEffectPrefab);
        }
    }

    // 🆕 NOVO: Método para ciclar o alcance (Range)
    public void CycleRange()
    {
        if (CurrentObserver == null || _manager == null) return;

        int maxRange = WavesManager.GridSize;
        int minRange = 1;

        // Incrementa o range. Se ultrapassar o máximo, volta para o mínimo.
        int nextRange = currentRange + -1;

        if (nextRange > maxRange)
        {
            nextRange = minRange;
        }
        else if(nextRange < 0)
        {
            nextRange = maxRange;
        }

        currentRange = nextRange;

        Debug.Log($"Slot ({GridX}, {GridY}) Range alterado para: {currentRange} (Min: {minRange}, Max: {maxRange})");

        if (_manager != null)
        {
            // Re-aplica a influência com o novo alcance
            _manager.ApplyObserverInfluenceAndShowEffects(CurrentObserver, this, CurrentObserver.WaveEffectPrefab);
        }
    }

    // ... (Métodos UpdateObserverRotation, InstantiateEffectObject, DestroyEffectObject, HandleObserverDrop)
    private void UpdateObserverRotation(Transform observerTransform)
    {
        Observer observerScript = observerTransform.GetComponent<Observer>();

        // Mantém a verificação CanRotate se o InfluenceType for Line (já tratado no AssignObserver)
        if (observerScript == null || !observerScript.CanRotate)
        {
            observerTransform.rotation = Quaternion.identity;
            return;
        }

        float angle = 0f;

        switch (Direction)
        {
            case DirectionType.Up: angle = 90f; break;
            case DirectionType.Down: angle = -90f; break;
            case DirectionType.Left: angle = 180f; break;
            case DirectionType.Right: angle = 0f; break;

            case DirectionType.Diagonal_UpLeft: angle = 135f; break;
            case DirectionType.Diagonal_UpRight: angle = 45f; break;
            case DirectionType.Diagonal_DownLeft: angle = -135f; break;
            case DirectionType.Diagonal_DownRight: angle = -45f; break;

            default: angle = 0f; break;
        }

        observerTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void InstantiateEffectObject(Transform parentTransform)
    {
        if (ObserverControllerPrefab != null && _currentObserverController == null)
        {
            // 1. Instancia o prefab, definindo o 'parentTransform' (o Observer)
            _currentObserverController = Instantiate(ObserverControllerPrefab, parentTransform);
            var controller = _currentObserverController.GetComponent<ObserverController>();
            controller.SetTargetSlot(this);
            // 2. Garante que a POSIÇÃO LOCAL seja zero (fica centralizado no Observer)
            _currentObserverController.transform.localPosition = Vector3.zero;

            // 3. Garante que a ROTAÇÃO LOCAL seja zero (Quaternion.identity).
            _currentObserverController.transform.localRotation = Quaternion.identity;

            // ⚠️ O método mais robusto: Instanciar no pai, mas garantir que a ROTAÇÃO GLOBAL seja a identidade.
            _currentObserverController.transform.rotation = Quaternion.identity; // <--- ZERA a rotação GLOBAL (o mais importante)

            _currentObserverController.name = $"Controller_{parentTransform.gameObject.name}";
        }
    }
    private void DestroyEffectObject()
    {
        if (_currentObserverController != null)
        {
            Destroy(_currentObserverController);
            _currentObserverController = null;
        }
    }

    public void HandleObserverDrop(Observer observer)
    {
        if (_manager != null)
        {
            _manager.HandleObserverDrop(observer, this);
        }
    }
}