using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircularUIController : MonoBehaviour
{
    public List<CanvasGroup> uiItems = new List<CanvasGroup>();

    [HideInInspector]
    public int currentIndex = 0;


    public float fadeOutDuration = 0.25f;
    public float fadeInDuration = 0.25f;

    public Button previousButton;
    public Button nextButton;
    public Button backButton;

    private bool isTransitioning = false;

    void Start()
    {
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(() => ChangeItem(-1));
        }
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => ChangeItem(1));
        }

        InitializeItems();
    }

    void InitializeItems()
    {
        for (int i = 0; i < uiItems.Count; i++)
        {
            if (uiItems[i] != null)
            {
                uiItems[i].alpha = (i == currentIndex) ? 1f : 0f;
                uiItems[i].interactable = (i == currentIndex);
                uiItems[i].blocksRaycasts = (i == currentIndex);
            }
        }
    }

    void OnEnable()
    {
        if (uiItems.Count > 0)
        {
            if (currentIndex != 0)
            {
                currentIndex = 0;
                InitializeItems();
            }
        }
        SetButtonsState(true);
    }

    public void ChangeItem(int direction)
    {
        if (isTransitioning || uiItems.Count < 2)
        {
            return;
        }

        int nextIndex = currentIndex + direction;

        if (nextIndex >= uiItems.Count)
        {
            nextIndex = 0;
        }
        else if (nextIndex < 0)
        {
            nextIndex = uiItems.Count - 1;
        }

        StartCoroutine(TransitionItem(nextIndex));
    }

    IEnumerator TransitionItem(int newIndex)
    {
        isTransitioning = true;
        SetButtonsState(false);

        CanvasGroup currentGroup = uiItems[currentIndex];
        yield return StartCoroutine(FadeCanvasGroup(currentGroup, 1f, 0f, fadeOutDuration));

        currentIndex = newIndex;
        CanvasGroup nextGroup = uiItems[currentIndex];

        nextGroup.alpha = 0f;
        nextGroup.interactable = true;
        nextGroup.blocksRaycasts = true;

        yield return StartCoroutine(FadeCanvasGroup(nextGroup, 0f, 1f, fadeInDuration));

        currentGroup.interactable = false;
        currentGroup.blocksRaycasts = false;

        isTransitioning = false;
        SetButtonsState(true);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        float startTime = Time.time;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        group.alpha = endAlpha;
    }

    void SetButtonsState(bool state)
    {
        if (previousButton != null) previousButton.interactable = state;
        if (nextButton != null) nextButton.interactable = state;
        if (backButton != null) backButton.interactable = state;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Back()
    {
        gameObject.SetActive(false);
    }
}