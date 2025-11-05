using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClipPlayer : MonoBehaviour
{
    [Tooltip("The AudioSource that will play the clips")]
    public AudioSource audioSource;

    [Tooltip("List of all piano keys (AnswerButton scripts)")]
    public List<AnswerButton> pianoKeys = new(); // 🆕 Assign in Inspector

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
        ClearAllHighlights();

        audioSource.PlayOneShot(clip);
        HighlightMatchingKey(clip);
    }

    private IEnumerator PlayClipsCoroutine()
    {
        foreach (var clip in internalBuffer)
        {
            if (clip == null) continue;

            ClearAllHighlights();

            // Highlight the new key
            HighlightMatchingKey(clip);

            audioSource.PlayOneShot(clip);

            // Wait for the clip duration + small buffer before next
            yield return new WaitForSeconds(1f);
        }
        ClearAllHighlights();
        playAllRoutine = null;
    }
    
    private void HighlightMatchingKey(AudioClip clip)
    {
        if (clip == null || pianoKeys == null) return;

        // Assuming your AudioClip name matches AnswerButton.noteValue (e.g., "C4", "D4", etc.)
        string clipName = clip.name.ToUpper();
        AnswerButton key = pianoKeys.Find(k => k.noteValue.ToUpper() == clipName);

        if (key != null)
        {
            key.HighlightKey(true);
        }
    }

    private void ClearAllHighlights()
    {
        foreach (var key in pianoKeys)
        {
            key.HighlightKey(false);
        }
    }
}
