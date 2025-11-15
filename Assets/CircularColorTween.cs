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
    private bool isRunning = false;

    void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        colorSequence = new List<Color>(availableColors);
    }

    void Start()
    {
        isRunning = true;

        if (targetImage != null)
        {
            targetImage.color = Color.white;
        }

        colorCycleCoroutine = StartCoroutine(ColorCycle());
    }

    void OnEnable()
    {
        DOTween.Play(targetImage);

        if (colorCycleCoroutine == null && isRunning)
        {
            colorCycleCoroutine = StartCoroutine(ColorCycle());
        }
    }

    void OnDisable()
    {
        DOTween.Pause(targetImage);

        if (colorCycleCoroutine != null)
        {
            StopCoroutine(colorCycleCoroutine);
            colorCycleCoroutine = null;
        }
        // isRunning = false deve ser feito apenas se quisermos forçar um reset completo. 
        // Para permitir o OnEnable retomar, mantemos o estado.
    }

    IEnumerator ColorCycle()
    {
        if (!isRunning) yield break;

        while (isRunning)
        {
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

            if (targetImage.color != Color.white)
            {
                float holdTime = Random.Range(minHoldTime, maxHoldTime);
                yield return new WaitForSeconds(holdTime);
            }

            float tweenDuration = Random.Range(minTweenSpeed, maxTweenSpeed);

            targetImage.DOColor(nextColor, tweenDuration).SetEase(Ease.InOutQuad).SetId(this);

            yield return new WaitForSeconds(tweenDuration);

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

        if (colorSequence[0] == targetImage.color)
        {
            if (colorSequence.Count > 1)
            {
                Color temp = colorSequence[0];
                colorSequence[0] = colorSequence[1];
                colorSequence[1] = temp;
            }
        }
    }
}