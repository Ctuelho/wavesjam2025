using UnityEngine;
using System.Collections.Generic;

public class MultiVolumeListener : MonoBehaviour
{
    // Lista para armazenar todos os AudioSources encontrados no objeto e nos filhos.
    private List<AudioSource> managedAudioSources;

    [Tooltip("Volume base de todos os AudioSources gerenciados. Este será multiplicado pelo volume global da música.")]
    [Range(0f, 1f)]
    public float baseVolume = 1.0f;

    void Awake()
    {
        // Pega todos os AudioSources no objeto atual E em todos os objetos filhos.
        managedAudioSources = new List<AudioSource>(GetComponentsInChildren<AudioSource>());

        if (managedAudioSources.Count == 0)
        {
            Debug.LogWarning($"MultiVolumeListener em {gameObject.name}: Não encontrou AudioSource(s) no objeto ou nos filhos.");
            return;
        }

        // Define o volume inicial usando o valor estático do manager.
        UpdateVolumes(BackgroundMusicManager.CurrentVolume);
    }

    void OnEnable()
    {
        // Se inscreve no evento ao ser ativado
        BackgroundMusicManager.OnMusicVolumeChanged += OnGlobalVolumeChanged;
    }

    void OnDisable()
    {
        // Desinscreve-se do evento ao ser desativado para evitar memory leaks
        BackgroundMusicManager.OnMusicVolumeChanged -= OnGlobalVolumeChanged;
    }

    /// <summary>
    /// Método chamado quando o evento OnMusicVolumeChanged é disparado.
    /// </summary>
    /// <param name="newGlobalVolume">O novo valor de volume máximo global.</param>
    private void OnGlobalVolumeChanged(float newGlobalVolume)
    {
        // Chama a função que itera e aplica o volume
        UpdateVolumes(newGlobalVolume);
    }

    /// <summary>
    /// Itera sobre todos os AudioSources gerenciados e aplica a nova configuração de volume.
    /// </summary>
    /// <param name="globalVolume">O volume global (MaxVolume) atual.</param>
    private void UpdateVolumes(float globalVolume)
    {
        float finalVolume = baseVolume * globalVolume;

        foreach (var source in managedAudioSources)
        {
            if (source != null)
            {
                // Aplica a multiplicação: Volume Base * Volume Global
                source.volume = finalVolume;
            }
        }

        // Opcional: Log para verificar se a atualização ocorreu
        // Debug.Log($"MultiVolumeListener atualizou {managedAudioSources.Count} AudioSource(s). Volume final: {finalVolume}");
    }
}