using UnityEngine;

// Garante que haja um AudioSource no mesmo GameObject
[RequireComponent(typeof(AudioSource))]
public class VolumeListener : MonoBehaviour
{
    private AudioSource myAudioSource;

    [Tooltip("Volume base do AudioSource, que será multiplicado pelo volume global da música.")]
    [Range(0f, 1f)]
    public float baseVolume = 1.0f;

    void Awake()
    {
        // Pega a referência ao AudioSource local
        myAudioSource = GetComponent<AudioSource>();

        // Define o volume inicial usando o valor estático do manager, caso exista
        // Multiplica o volume base pelo volume global atual.
        myAudioSource.volume = baseVolume * BackgroundMusicManager.CurrentVolume;
    }

    void OnEnable()
    {
        // Se inscreve no evento ao ser ativado
        // Garante que o método OnGlobalVolumeChanged seja chamado quando o evento disparar.
        BackgroundMusicManager.OnMusicVolumeChanged += OnGlobalVolumeChanged;
    }

    void OnDisable()
    {
        // É CRUCIAL DESINSCREVER-SE do evento ao ser desativado para evitar memory leaks (objetos desativados tentando receber eventos).
        BackgroundMusicManager.OnMusicVolumeChanged -= OnGlobalVolumeChanged;
    }

    /// <summary>
    /// Método chamado quando o evento OnMusicVolumeChanged é disparado pelo BackgroundMusicManager.
    /// </summary>
    /// <param name="newGlobalVolume">O novo valor de volume máximo global.</param>
    private void OnGlobalVolumeChanged(float newGlobalVolume)
    {
        // Define o volume local do AudioSource multiplicando o volume base deste componente pelo novo volume global.
        myAudioSource.volume = baseVolume * newGlobalVolume;

        Debug.Log($"VolumeListener atualizado. Novo volume local: {myAudioSource.volume}");
    }
}