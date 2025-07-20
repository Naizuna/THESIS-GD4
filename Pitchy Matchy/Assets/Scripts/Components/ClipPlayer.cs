using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClipPlayer : MonoBehaviour
{
    [Tooltip("The AudioSource that will play the clips")]
    public AudioSource audioSource;

    private List<AudioClip> internalBuffer;
    private Coroutine playAllRoutine;

    /// <summary>
    /// Call this to start playing through the list once, with 1s between each clip.
    /// Cancels any single‑clip play in progress.
    /// </summary>
    public void PlayAllClips(List<AudioClip> clips)
    {
        // stop any "play all" already running
        if (playAllRoutine != null)
        {
            StopCoroutine(playAllRoutine);
            playAllRoutine = null;
        }
        // stop any single‑clip sound
        audioSource.Stop();

        internalBuffer = clips;
        if (audioSource == null || internalBuffer == null || internalBuffer.Count == 0)
        {
            Debug.LogWarning("AudioSource or clips list not set up!");
            return;
        }

        playAllRoutine = StartCoroutine(PlayClipsCoroutine());
    }

    /// <summary>
    /// Plays one clip immediately, canceling any ongoing "play all" sequence
    /// or another single clip.
    /// </summary>
    public void PlaySingleClip(AudioClip clip)
    {
        // cancel the "play all" coroutine
        if (playAllRoutine != null)
        {
            StopCoroutine(playAllRoutine);
            playAllRoutine = null;
        }
        // stop currently playing sounds so this clip is front‑and‑center
        audioSource.Stop();

        // fire‑and‑forget the one clip
        audioSource.PlayOneShot(clip);
    }

    private IEnumerator PlayClipsCoroutine()
    {
        foreach (var clip in internalBuffer)
        {
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(1f);
        }
        playAllRoutine = null;
    }
}
