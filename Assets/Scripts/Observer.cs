using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Observer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // NOVO ENUM: Define como a influência se propaga.
    public enum InfluenceType { Line = 0, Radius = 1 }

    public Slot CurrentSlot { get; set; }
    private Vector3 _startPosition;
    private Transform _startParent;
    private Collider2D _col;
    public GameObject WaveEffectPrefab;

    // Novo campo para definir o tipo de influência
    public InfluenceType influenceType = InfluenceType.Line;

    // CanRotate agora só deve ser relevante se influenceType for Line.
    public bool CanRotate = true;
    public int range = 0;
    public int force = 0;
    public Slot.DecayType decay = Slot.DecayType.DoesNotDecay;
    internal ObserversManager manager;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPosition = transform.position;
        _startParent = transform.parent;

        if (CurrentSlot != null)
        {
            CurrentSlot.RemoveObserver();
        }

        transform.SetParent(null);
        _col.enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPoint.z = 0;
        transform.position = worldPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
        _col.enabled = true;
        Slot targetSlot = null;

        if (droppedOn == null)
        {
            ReturnToManager();
            return;
        }

        targetSlot = droppedOn.GetComponent<Slot>();
        if (targetSlot != null)
        {
            if (targetSlot.CurrentObserver != null)
            {
                //replace
                targetSlot.CurrentObserver.ReturnToManager();
            }

            targetSlot.HandleObserverDrop(this);
            return;
        }

        //dropped on observer being used
        Observer observerDroppedOn = droppedOn.GetComponent<Observer>();
        if (observerDroppedOn != null)
        {
            //dropped on an observer, now check if it is in use or in the manager
            if (observerDroppedOn.CurrentSlot == null)
            {
                //dropped in the manager, just return this to the manager
                ReturnToManager();
                return;
            }

            targetSlot = observerDroppedOn.CurrentSlot;
            observerDroppedOn.CurrentSlot.RemoveObserver();
            observerDroppedOn.ReturnToManager();
            targetSlot.HandleObserverDrop(this);
            return;
        }

        // Caso não tenha sido dropado em Slot ou Observer, retorna para o manager/posição inicial
        ReturnToManager();
    }

    private void ReturnToManager()
    {
        if (manager != null)
        {
            if (CurrentSlot != null)
            {
                CurrentSlot.RemoveObserver();
            }
            manager.ReturnObserver(this);
        }
        else
        {
            transform.position = _startPosition;
            transform.SetParent(_startParent);
        }
    }

    public void IncreaseRange()
    {
        range++;

        if (range > WavesManager.GridSize)
        {
            range = 0;
        }
    }

    public void DecreaseRange()
    {
        range--;

        if (range < 0)
        {
            range = WavesManager.GridSize;
        }
    }
}