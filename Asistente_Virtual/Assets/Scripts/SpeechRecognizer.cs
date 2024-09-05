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
    private bool close = false;
    private SpeechRecognizerPlugin plugin = null;
   
    private void Start() {
        plugin = SpeechRecognizerPlugin.GetPlatformPluginVersion(this.gameObject.name);
        avatar = GameObject.FindGameObjectWithTag("Avatar").GetComponent<Avatar>();
        ControllerSound.SoundCompleted += StartRecording; //suscribirse al evento que indica cuando el sonido "bell" terminó
        Avatar.Inactivated += AvatarInactivated; //suscribirse al evento que indica cuando se terminó de reproducir la respuesta
        Invoke("StartListening", 1f);
    }

    private void Update() {
    }

    private void StartListening() {
        plugin.StartListening();
        plugin.SetContinuousListening(true);
        plugin.SetLanguageForNextRecognition("es-ES");
        plugin.SetMaxResultsForNextRecognition(1);
    }

    private void StopListening() {
        plugin.StopListening();
    }

    public void OnResult(string recognizedResult) {
        char[] delimiterChars = { '~' };
        string[] result = recognizedResult.Split(delimiterChars);

        for (int i = 0; i < result.Length; i++)
        {
            // if(result[i].ToLower().Contains("akira") || result[i].ToLower().Contains("akima") || result[i].ToLower().Contains("aki") || result[i].ToLower().Contains("iki"))
            if(result[i].ToLower().Contains("luna") || result[i].ToLower().Contains("lun"))
            {
                keywordFound = true;
            }
            if(result[i].ToLower().Contains("cerrar"))
            {
                close = true;
            }
            
        }

        if (keywordFound)
        {   
            keywordFound = false;           
            // StartRecording();
            StopListeningName();
        }

        if (close)
        {
            // Cierra la aplicación completamente en dispositivos Android
        #if UNITY_ANDROID
            AndroidJavaObject activity = new AndroidJavaObject("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = activity.GetStatic<AndroidJavaObject>("currentActivity");
            context.Call("finishAndRemoveTask"); // Finaliza la actividad y la remueve de las tareas recientes
        #else
            Application.Quit(); // Para otras plataformas, utiliza Application.Quit()
        #endif
        }
    }

    public void OnError(string recognizedError) {}

    private void StopListeningName() {
        //dejar de escuchar la palabra clave "luna"
        StopListening();//cambio de prueba
        ControllerSound.Instance.ExecuteSound(bell);
        avatar.AnimationListen();
    }
    private void StartRecording() {
        //se termina el audio "bell" y empieza a grabar el audio del estudiante
        ControllerSound.SoundCompleted -= StartRecording; //desuscribirse al evento que indica cuando "bell" terminó
        avatar.StartRecording(); //empieza a grabar el audio del estudiante y enviarlo a lambda
    }

    private void AvatarInactivated() {
        plugin.StartListening(); //vuelve a escuchar la palabra clave "luna"
        ControllerSound.SoundCompleted += StartRecording; //se suscribe al evento que indica cuando "bell" terminó
    }

}
