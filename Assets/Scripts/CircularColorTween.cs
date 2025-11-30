using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class CircularColorTween : MonoBehaviour
{
    public Image targetImage;
    public List<Color> availableColors = new List<Color>();
    public float minHoldTime = 1.0f;
    public float maxHoldTime = 3.0f;
    public float minTweenSpeed = 0.5f;
    public float maxTweenSpeed = 1.5f;

    private List<Color> colorSequence;
    private int sequenceIndex = 0;
    private bool firstCycle = true;
    private Coroutine colorCycleCoroutine;

    void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        colorSequence = new List<Color>(availableColors);
    }

    void OnEnable()
    {
        if (availableColors.Count == 0) return;

        targetImage.DOKill();
        colorCycleCoroutine = StartCoroutine(ColorCycle());
    }

    void OnDisable()
    {
        targetImage.DOKill();

        if (colorCycleCoroutine != null)
        {
            StopCoroutine(colorCycleCoroutine);
            colorCycleCoroutine = null;
        }
    }

    bool firstColor = false;
    IEnumerator ColorCycle()
    {
        firstColor = true;
        while (true)
        {
            bool wasFirstCircle = firstCycle;
            if (sequenceIndex >= colorSequence.Count)
            {
                if (firstCycle)
                {
                    firstCycle = false;
                }
                else
                {
                    ShuffleColorSequence();
                }
                sequenceIndex = 0;
            }

            Color nextColor = colorSequence[sequenceIndex];

            bool isStartingWhite = (targetImage.color == Color.white && firstCycle);

            if (!isStartingWhite)
            {
                //float holdTime = Random.Range(minHoldTime, maxHoldTime);
                yield return new WaitForSeconds(0);
            }

            float tweenDuration = Random.Range(minTweenSpeed, maxTweenSpeed);
            yield return targetImage.DOColor(nextColor, tweenDuration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();

            sequenceIndex++;
        }
    }

    private void ShuffleColorSequence()
    {
        int n = colorSequence.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            Color value = colorSequence[k];
            colorSequence[k] = colorSequence[n];
            colorSequence[n] = value;
        }

        if (IsColorSimilar(colorSequence[0], targetImage.color))
        {
            if (colorSequence.Count > 1)
            {
                Color temp = colorSequence[0];
                colorSequence[0] = colorSequence[1];
                colorSequence[1] = temp;
            }
        }
    }

    private bool IsColorSimilar(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f &&
               Mathf.Abs(a.g - b.g) < 0.01f &&
               Mathf.Abs(a.b - b.b) < 0.01f;
    }
}