using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    public GameObject confirmPanel;
    public Level firstLevel;

    private void OnEnable()
    {
        confirmPanel.SetActive(false);
    }

    private void OnDisable()
    {
        Level.SelectedLevel = null;
    }

    private void Update()
    {
        confirmPanel.SetActive(Level.SelectedLevel != null);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        StartCoroutine(CallTryOpenLevelsNextFrame());
    }

    private IEnumerator CallTryOpenLevelsNextFrame()
    {
        yield return null;
        yield return null;

        Level.TryOpenLevels();

        if(Level.SelectedLevel != null)
        {
            Level.SelectedLevel.OnPointerClick(null);
        }
        else
        {
            firstLevel.OnPointerClick(null);
        }
    }

    public void ClickStart()
    {
        LevelManager.SetLevelData(Level.SelectedLevel);
    }
}
