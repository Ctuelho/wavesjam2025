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

    private WavesManager _manager;

    public enum DecayType { DoesNotDecay = 0, VerySlow = 2, Slow = 3, Medium = 4, Fast = 5, VeryFast = 6 }
    public DecayType CurrentDecayType = DecayType.DoesNotDecay;

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
            CurrentObserver.transform.position = transform.position;

            UpdateObserverRotation(CurrentObserver.transform);

            InstantiateEffectObject(CurrentObserver.transform);

            if (_manager != null)
            {
                // Chamada simplificada, sem passar a lista de efeitos.
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
        }
    }

    public void CycleDecayType()
    {
        if (CurrentObserver == null) return;

        int nextDecay = ((int)CurrentDecayType + 1) % (System.Enum.GetValues(typeof(DecayType)).Length);
        CurrentDecayType = (DecayType)nextDecay;

        Debug.Log($"Slot ({GridX}, {GridY}) Decay alterado para: {CurrentDecayType}");

        if (_manager != null)
        {
            // Chamada simplificada, sem passar a lista de efeitos.
            _manager.ApplyObserverInfluenceAndShowEffects(CurrentObserver, this, CurrentObserver.WaveEffectPrefab);
        }
    }

    // ... (Métodos UpdateObserverRotation, InstantiateEffectObject, DestroyEffectObject e HandleObserverDrop permanecem inalterados)

    private void UpdateObserverRotation(Transform observerTransform)
    {
        Observer observerScript = observerTransform.GetComponent<Observer>();
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
            // Isto faz com que ele herde a ROTAÇÃO GLOBAL do pai (Observer),
            // mas depois a ZERA, mantendo sua orientação na identidade do mundo,
            // desde que o Observer não esteja no mundo rodado.
            _currentObserverController.transform.localRotation = Quaternion.identity; // <--- Rotação local zerada

            // 4. Se o Observer já estiver rotacionado (ex: 90 graus), o controller TAMBÉM
            // estará rotacionado 90 graus. Para COMPENSAR a rotação do PAI,
            // você precisa ZERAR a rotação *global* OU desanexá-lo imediatamente,
            // mas o mais simples é garantir que a rotação local seja zero e 
            // que a rotação do Observer seja baseada no mundo.

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