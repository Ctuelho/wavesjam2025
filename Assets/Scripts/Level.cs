using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class Level : MonoBehaviour, IPointerClickHandler
{
    public static Level SelectedLevel;
    public CollapsedGridDrawerUI drawerUI;

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
    public TextAsset target;

    public Level OpensIf;
    [HideInInspector]
    public bool IsOpen = false;

    public List<GameObject> shadowStars;
    public List<GameObject> trueStars;

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

            if (_isComplete)
            {
                interactable.interactable = true;
                completeMark.SetActive(true);
                closedMark.SetActive(false);
            }
            else
            {
                completeMark.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        UpdateVisualState();
    }
    private void OnValidate()
    {
        levelName = name;
    }

    private void Awake()
    {
        if (PlayerPrefs.HasKey(levelId.ToString()))
        {            
            stars = PlayerPrefs.GetInt(levelId.ToString());
            UpdateStars();
            IsComplete = true;
        }
    }

    public static void TryOpenLevels()
    {
        List<Level> allLevels = FindObjectsByType<Level>(FindObjectsSortMode.None)
                                        .Where(l => l.gameObject.activeSelf)
                                        .ToList();

        foreach (Level lvl in allLevels)
        {
            bool willOpen = false;

            // 1. PRIMEIRA CHECAGEM: SAVE (PlayerPrefs)
            // Se o levelId tiver uma chave salva e as estrelas forem > 0, ele já está COMPLETO e ABERTO.
            if (PlayerPrefs.HasKey(lvl.levelId.ToString()))
            {
                int savedStars = PlayerPrefs.GetInt(lvl.levelId.ToString());

                if (savedStars > 0)
                {
                    lvl.stars = savedStars;
                    lvl.IsComplete = true; // Isso seta o completeMark e o interactable
                    willOpen = true;
                }
            }

            // 2. SEGUNDA CHECAGEM: DEPENDÊNCIA (Apenas se não estiver aberto pelo save)
            if (!willOpen)
            {
                if (lvl.OpensIf == null)
                {
                    // É o primeiro nível ou não tem pré-requisito
                    willOpen = true;
                }
                else
                {
                    // Abertura condicional baseada na conclusão do nível anterior
                    willOpen = lvl.OpensIf.IsComplete;
                }

                // Atualiza o estado de Interação e o marcador Fechado/Completo (se não foi feito em IsComplete = true)
                lvl.interactable.interactable = willOpen;
                lvl.closedMark.SetActive(!willOpen);

                // Se o nível está aberto, mas não completo (porque willOpen é true e não veio do save), 
                // garantimos que o completeMark esteja DESATIVADO, e o closedMark ATIVADO.
                if (!lvl.IsComplete && willOpen)
                {
                    lvl.completeMark.SetActive(false);
                }
            }

            // Aplica o estado de abertura final (necessário mesmo que já completo para a lógica de dependência futura)
            lvl.IsOpen = willOpen;

            // Atualiza a visualização das estrelas (seja do save ou zero)
            lvl.UpdateStars();
        }
    }

    public void Complete(int stars)
    {
        if (stars > 0)
        {
            this.stars = stars;
            UpdateStars();
            PlayerPrefs.SetInt(levelId.ToString(), stars);
            PlayerPrefs.Save();
            completeMark.gameObject.SetActive(true);
        }
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
        if (target != null)
        {
            drawerUI.DrawGridFromData(target.text);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable.interactable)
            return;

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

    public void UpdateStars()
    {
        int count = Mathf.Min(shadowStars.Count, trueStars.Count);

        for (int i = 0; i < count; i++)
        {
            bool starAchieved = i < stars;

            if (trueStars[i] != null)
                trueStars[i].SetActive(starAchieved);

            if (shadowStars[i] != null)
                shadowStars[i].SetActive(!starAchieved);
        }
    }
}