using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Linq;
using System; // Adicionado para usar Action

public class BackgroundMusicManager : MonoBehaviour
{
    // EVENTO: Qualquer script no jogo pode se inscrever neste evento.
    // Ele será disparado com o novo valor de volume máximo (float).
    public static event Action<float> OnMusicVolumeChanged;

    [Header("Componentes de Áudio")]
    [Tooltip("AudioSource 1. Um dos dois AudioSources usados para o crossfade.")]
    public AudioSource AudioSource1;
    [Tooltip("AudioSource 2. O outro AudioSource usado para o crossfade.")]
    public AudioSource AudioSource2;

    [Header("Configurações de Transição")]
    [Tooltip("Tempo em segundos para o volume da música antiga diminuir para zero (Fade Out).")]
    [Range(0.1f, 5f)]
    public float FadeOutTime = 1.0f;

    [Tooltip("Tempo em segundos de ESPERA após o início do Fade Out, e antes da nova música começar a tocar (Wait).")]
    [Range(0f, 5f)]
    public float WaitBeforeNewMusicStart = 0.5f;

    [Tooltip("Volume máximo de todas as músicas.")]
    [Range(0f, 1f)]
    public float MaxVolume = 0.5f;

    [Header("Volume Cycling")]
    [Tooltip("Lista de volumes (0.0 a 1.0) para ciclar.")]
    public float[] PossibleVolumes = { 0.25f, 0.5f, 0.75f, 1.0f };
    public int currentVolumeIndex = 0;
    public static float CurrentVolume;

    public float mainMusicVolume = 1f;
    public float levelMusicVolume = 1f;

    // PONTEIROS DE ESTADO DINÂMICO
    private AudioSource currentSource;
    private AudioSource previousSource;

    public AudioClip mainClip;
    public AudioClip levelClip;

    private float ajudestedVolume = 1f;

    public void PlayMainClip()
    {
        ChangeMusic(mainClip, mainMusicVolume);
    }

    public void PlayLevelClip()
    {
        ChangeMusic(levelClip, levelMusicVolume);
    }

    private void Awake()
    {
        // Garante que os AudioSources existam
        if (AudioSource1 == null) AudioSource1 = gameObject.AddComponent<AudioSource>();
        if (AudioSource2 == null) AudioSource2 = gameObject.AddComponent<AudioSource>();

        // Configurações básicas
        AudioSource1.loop = true;
        AudioSource2.loop = true;
        AudioSource1.volume = 0f;
        AudioSource2.volume = 0f;

        // Inicializa o índice de volume
        currentVolumeIndex = PossibleVolumes.ToList().IndexOf(MaxVolume);
        if (currentVolumeIndex < 0 && PossibleVolumes.Length > 0)
        {
            currentVolumeIndex = 0;
            MaxVolume = PossibleVolumes[0];
        }

        // Define o source inicial
        currentSource = AudioSource1;
        previousSource = AudioSource2;

        currentVolumeIndex = -1;
        CycleMusicVolume();
        PlayInitialMusic(mainClip, mainMusicVolume);
    }

    public void ChangeMusic(AudioClip newClip, float volumeAdjust = 1f)
    {
        StartCoroutine(TransitionMusicCoroutine(newClip, volumeAdjust));
    }

    private IEnumerator TransitionMusicCoroutine(AudioClip newClip, float volumeAdjust = 1f)
    {
        if (newClip == null || currentSource.clip == newClip)
        {
            if (newClip == null) Debug.LogError("O AudioClip fornecido é nulo.");
            yield break;
        }

        // 1. TROCA DE PAPÉIS (Swap)
        (currentSource, previousSource) = (previousSource, currentSource);

        // 2. CONFIGURA O NOVO SOURCE
        currentSource.clip = newClip;
        currentSource.volume = 0f;
        currentSource.Play();

        // 3. FADE OUT da música antiga
        DOTween.Kill(previousSource);
        previousSource.DOFade(0f, FadeOutTime);

        // 4. FADE IN da nova
        ajudestedVolume = volumeAdjust;
        yield return currentSource.DOFade(MaxVolume * volumeAdjust, WaitBeforeNewMusicStart).WaitForCompletion();

        // 5. Limpeza: 
        previousSource.Stop();
        previousSource.clip = null;

        Debug.Log($"Música trocada para: {newClip.name}. Volume: {MaxVolume}");
    }

    public void PlayInitialMusic(AudioClip clip, float volumeAdjust = 1f)
    {
        if (clip == null) return;

        DOTween.Kill(currentSource);

        currentSource.clip = clip;
        currentSource.volume = MaxVolume * volumeAdjust;
        currentSource.Play();
        ajudestedVolume = volumeAdjust;
        Debug.Log($"Música inicial tocando: {clip.name}. Volume: {MaxVolume}");
    }

    /// <summary>
    /// Altera o MaxVolume global e aplica o volume instantaneamente à música que está tocando agora.
    /// Dispara um evento com o novo volume.
    /// </summary>
    public void CycleMusicVolume()
    {
        if (PossibleVolumes == null || PossibleVolumes.Length == 0)
        {
            Debug.LogWarning("Lista 'PossibleVolumes' está vazia.");
            return;
        }

        // 1. Calcula o próximo índice e define o novo Volume Máximo global
        currentVolumeIndex = (currentVolumeIndex + 1) % PossibleVolumes.Length;
        MaxVolume = PossibleVolumes[currentVolumeIndex];

        // 2. Aplica o novo volume à música que está tocando agora (currentSource) - INSTANTANEAMENTE
        currentSource.volume = MaxVolume;

        // 3. ATUALIZA A VARIÁVEL ESTÁTICA E DISPARA O EVENTO COM O NOVO VALOR
        CurrentVolume = MaxVolume * ajudestedVolume;
        // O símbolo '?' garante que o evento só seja invocado se houver inscritos.
        OnMusicVolumeChanged?.Invoke(CurrentVolume);

        Debug.Log($"Volume ciclado para: {MaxVolume}");
    }
}