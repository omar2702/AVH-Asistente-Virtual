using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SpeechRecognizerPlugin;

using System.IO;
using UnityEngine.Networking;
using UnityEngine.Events;

public class SpeechRecognizer : MonoBehaviour, ISpeechRecognizerPlugin
{
    [SerializeField] private AudioClip bell;

    private AudioClip clip;
    public Avatar avatar;
    private bool keywordFound = false;
    private SpeechRecognizerPlugin plugin = null;
   
    private void Start() {
        plugin = SpeechRecognizerPlugin.GetPlatformPluginVersion(this.gameObject.name);
        avatar = GameObject.FindGameObjectWithTag("Avatar").GetComponent<Avatar>();
        ControllerSound.SoundCompleted += StartRecording; //suscribirse al evento que indica cuando el sonido "bell" terminó
        Avatar.Completed += AvatarResponseFinalized; //suscribirse al evento que indica cuando se terminó de reproducir la respuesta
        Invoke("StartListening", 1f);
    }

    private void Update() {
    }

    private void StartListening() {
        plugin.StartListening();
        plugin.SetContinuousListening(true);
        plugin.SetLanguageForNextRecognition("es-pe");
        plugin.SetMaxResultsForNextRecognition(8);
    }

    private void StopListening() {
        plugin.StopListening();
    }

    public void OnResult(string recognizedResult) {
        char[] delimiterChars = { '~' };
        string[] result = recognizedResult.Split(delimiterChars);

        for (int i = 0; i < result.Length; i++)
        {
            if(result[i].ToLower().Contains("ak"))
            {
                keywordFound = true;
                break;
            }
            
        }

        if (keywordFound)
        {   
            keywordFound = false;           
            // StartRecording();
            StopListeningName();
        }
    }

    public void OnError(string recognizedError) {}

    private void StopListeningName() {
        //dejar de escuchar la palabra clave "akira"
        StopListening();
        ControllerSound.Instance.ExecuteSound(bell);
    }
    private void StartRecording() {
        //se termina el audio "bell" y empieza a grabar el audio del estudiante
        ControllerSound.SoundCompleted -= StartRecording; //desuscribirse al evento que indica cuando "bell" terminó
        avatar.StartRecording(); //empieza a grabar el audio del estudiante y enviarlo a lambda
    }

    private void AvatarResponseFinalized() {
        plugin.StartListening(); //vuelve a escuchar la palabra clave "akira"
        ControllerSound.SoundCompleted += StartRecording; //se suscribe al evento que indica cuando "bell" terminó
    }

}
