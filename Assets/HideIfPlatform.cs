using System.Collections.Generic;
using UnityEngine;

public class HideIfPlatform : MonoBehaviour
{
    public List<RuntimePlatform> platformsToHideOn = new List<RuntimePlatform>();
    public bool ifNot = false;

    void Awake()
    {
        CheckPlatform();
    }

    public void CheckPlatform()
    {
        RuntimePlatform currentPlatform = Application.platform;

        bool isPlatformInList = platformsToHideOn.Contains(currentPlatform);

        bool shouldHide = (isPlatformInList && !ifNot) || (!isPlatformInList && ifNot);

        this.gameObject.SetActive(!shouldHide);
    }
}