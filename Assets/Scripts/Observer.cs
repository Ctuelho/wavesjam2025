using UnityEngine;
using UnityEngine.EventSystems;

public class Observer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Slot CurrentSlot { get; set; }
    private Vector3 _startPosition;
    private Transform _startParent;

    public int range = 0;
    public DecayType decay = DecayType.DoesNotDecay;

    public enum DecayType { DoesNotDecay = 0, Spread = 1, VerySlow = 2, Slow = 3, Medium = 4, Fast = 5, VeryFast = 6 }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPosition = transform.position;
        _startParent = transform.parent;

        if (CurrentSlot != null)
        {
            CurrentSlot.RemoveObserver();
        }

        transform.SetParent(null);
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

        if (droppedOn != null)
        {
            Slot targetSlot = droppedOn.GetComponent<Slot>();

            if (targetSlot != null)
            {
                // O Observer interage com o Slot, e o Slot chama o Manager.
                targetSlot.HandleObserverDrop(this);
                return;
            }
        }

        if (_startParent != null && _startParent.GetComponent<Slot>() != null)
        {
            Slot originalSlot = _startParent.GetComponent<Slot>();
            if (originalSlot != null)
            {
                originalSlot.AssignObserver(this);
            }
        }
        else
        {
            transform.position = _startPosition;
            transform.SetParent(_startParent);
        }
    }
}