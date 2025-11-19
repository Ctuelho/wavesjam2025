using UnityEngine;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    public GameObject confirmPanel;

    private void OnDisable()
    {
        Level.SelectedLevel = null;
    }

    private void Update()
    {
        confirmPanel.SetActive(Level.SelectedLevel != null);
    }
}
