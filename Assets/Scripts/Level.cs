using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Level : MonoBehaviour, IPointerClickHandler
{
    public static Level SelectedLevel;

    [Header("Visual Settings")]
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public GameObject completeMark;
    public GameObject closedMark;
    public CanvasGroup interactable;

    [Header("Level Data")]
    public int levelId;
    public string levelName;
    public int duration = 30;
    public List<GameObject> observers;
    public int gridSize = 3;
    public Sprite target;

    public Level OpensIf;

    private int stars = 0;
    private bool _isComplete;
    public bool IsComplete 
    {
        get
        {
            return _isComplete;
        }
        set
        {
            _isComplete = value;
            completeMark.SetActive(value);
            interactable.interactable = (value && true);
        }
    }

    private void Awake()
    {
        if (PlayerPrefs.HasKey(levelId.ToString()))
        {            
            stars = PlayerPrefs.GetInt(levelId.ToString());
            IsComplete = true;
        }
    }

    public static void TryOpenLevels()
    {
        Level[] allLevels = FindObjectsByType<Level>(FindObjectsSortMode.None);

        foreach (Level lvl in allLevels)
        {
            bool willOpen = false;
            if (lvl.OpensIf == null)
            {
                willOpen = true;
            }
            else
            {
                willOpen = lvl.OpensIf.IsComplete;
            }

            if(willOpen)
            {
                lvl.interactable.interactable = willOpen;
                lvl.closedMark.SetActive(!willOpen);
            }
        }
    }

    public void Complete(int stars)
    {
        this.stars = stars;
        PlayerPrefs.SetInt(levelId.ToString(), stars);
        PlayerPrefs.Save();
    }

    public static void ClearSave()
    {
        Level[] allLevels = FindObjectsByType<Level>(FindObjectsSortMode.None);

        foreach (Level lvl in allLevels)
        {
            PlayerPrefs.DeleteKey(lvl.levelId.ToString());
            lvl.IsComplete = false;
        }

        PlayerPrefs.Save();
    }

    private void Start()
    {
        UpdateVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SelectedLevel != null && SelectedLevel != this)
        {
            Level previousSelection = SelectedLevel;
            SelectedLevel = null;
            previousSelection.UpdateVisualState();
        }

        SelectedLevel = this;
        UpdateVisualState();
    }

    public void UpdateVisualState()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = (SelectedLevel == this) ? selectedColor : normalColor;
        }
    }
}