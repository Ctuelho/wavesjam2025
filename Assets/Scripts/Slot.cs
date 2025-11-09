using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour
{
    public int GridX;
    public int GridY;
    public Observer CurrentObserver;
    public CornerType Corner = CornerType.None;
    public DirectionType Direction = DirectionType.None;

    private WavesManager _manager;

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
            CurrentObserver.CurrentSlot = this;
            CurrentObserver.transform.position = transform.position;
            CurrentObserver.transform.SetParent(transform);

            if (_manager != null)
            {
                _manager.ApplyObserverInfluence(CurrentObserver, this);
            }
        }
    }

    public void RemoveObserver()
    {
        if (CurrentObserver != null)
        {
            if (_manager != null)
            {
                _manager.RemoveInfluenceSource(CurrentObserver);
            }

            CurrentObserver.CurrentSlot = null;
            CurrentObserver.transform.SetParent(null);
            CurrentObserver = null;
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