using UnityEngine;
using UnityEngine.EventSystems;

public class Observer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Slot CurrentSlot { get; set; }
    private Vector3 _startPosition;
    private Transform _startParent;
    private Collider2D _col;

    public int range = 0;
    public DecayType decay = DecayType.DoesNotDecay;

    public enum DecayType { DoesNotDecay = 0, Spread = 1, VerySlow = 2, Slow = 3, Medium = 4, Fast = 5, VeryFast = 6 }

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

        if (droppedOn != null)
        {
            Slot targetSlot = droppedOn.GetComponent<Slot>();

            if (targetSlot != null)
            {
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