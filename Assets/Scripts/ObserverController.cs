using UnityEngine;

public class ObserverController : MonoBehaviour
{
    private Observer observer;

    void Awake()
    {
        Transform currentParent = transform.parent;

        while (currentParent != null)
        {
            observer = currentParent.GetComponent<Observer>();

            if (observer != null)
            {
                Debug.Log("Observer found at: " + currentParent.name);
                break;
            }

            currentParent = currentParent.parent;
        }

        if (observer == null)
        {
            Debug.LogError("Observer not found.");
        }
    }

    public void IncreaseObserverParameter()
    {
        if(observer != null)
        {
            observer.IncreaseRange();
        }
    }

    public void DecreaseObserverParameter()
    {
        if (observer != null)
        {
            observer.DecreaseRange();
        }
    }
}