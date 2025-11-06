using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClipPlayer : MonoBehaviour
{
    [Tooltip("The AudioSource that will play the clips")]
    public AudioSource audioSource;

    [Tooltip("List of all piano keys (AnswerButton scripts)")]
    public List<AnswerButton> pianoKeys = new();

    private List<AudioClip> internalBuffer;
    private Coroutine playAllRoutine;
    private Coroutine singleRoutine;

    public bool IsPlaying { get; private set; } = false;

    // ===================== PLAY ALL =====================

    public void PlayAllClips(List<AudioClip> clips)
    {
        StopAllAudio();

        internalBuffer = clips;

        if (internalBuffer == null || internalBuffer.Count == 0 || audioSource == null)
        {
            Debug.LogWarning("ClipPlayer missing clips or AudioSource");
            return;
        }

        IsPlaying = true; //  mark as playing before coroutine starts
        playAllRoutine = StartCoroutine(PlayClipsCoroutine());
    }

    // =============== PLAY SINGLE CLIP ===================

    public void PlaySingleClip(AudioClip clip)
    {
        StopAllAudio();  // stop PlayAll or previous single clip

        if (clip == null) return;

        IsPlaying = true;

        ClearAllHighlights();
        audioSource.PlayOneShot(clip);
        HighlightMatchingKey(clip);

        singleRoutine = StartCoroutine(SingleClipWait(clip.length));
    }

    private IEnumerator SingleClipWait(float duration)
    {
        yield return new WaitForSeconds(duration);
        ClearAllHighlights();
        IsPlaying = false;
        singleRoutine = null;
    }


    private IEnumerator PlayClipsCoroutine()
    {
        foreach (var clip in internalBuffer)
        {
            if (clip == null) continue;

            ClearAllHighlights();
            HighlightMatchingKey(clip);

            audioSource.PlayOneShot(clip);

            yield return new WaitForSeconds(1f);
        }

        ClearAllHighlights();
        IsPlaying = false;
        playAllRoutine = null;
    }

    // ===================== STOP AUDIO =====================

    public void StopAllAudio()
    {
        // cancel any routines
        if (playAllRoutine != null)
        {
            StopCoroutine(playAllRoutine);
            playAllRoutine = null;
        }

        if (singleRoutine != null)
        {
            StopCoroutine(singleRoutine);
            singleRoutine = null;
        }

        audioSource.Stop();
        ClearAllHighlights();
        IsPlaying = false;
    }

    // ===================== HELPERS =====================   

    private void HighlightMatchingKey(AudioClip clip)
    {
        if (clip == null) return;

        string clipName = clip.name.ToUpper();
        var key = pianoKeys.Find(k => k.noteValue.ToUpper() == clipName);

        if (key != null)
            key.HighlightKey(true);
    }

    private void ClearAllHighlights()
    {
        foreach (var key in pianoKeys)
            key.HighlightKey(false);
    }
}
