using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ControllerSound : MonoBehaviour
{
    public static ControllerSound Instance;
    public static event System.Action SoundCompleted; //evento que indica cuando un sonido terminó de reproducirse
    private AudioSource audioSource;

    private void Awake () {
        if(Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void ExecuteSound (AudioClip sound) {
        // audioSource.PlayOneShot(sound);
        StartCoroutine(PlaySoundAndWait(sound));
    }

    public IEnumerator PlaySoundAndWait(AudioClip sound) {
        audioSource.clip = sound;
        audioSource.Play();
        // yield return new WaitUntil(() => audioSource.time >= sound.length);
        yield return new WaitUntil(() => !audioSource.isPlaying);
        SoundCompleted?.Invoke(); //se dispara el evento
    }
}
