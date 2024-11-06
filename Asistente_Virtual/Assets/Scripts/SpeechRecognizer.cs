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

    private AndroidJavaObject audioManager;
    private int originalMediaVolume;
    private int originalNotificationVolume;
   
    private void Start() {
        #if UNITY_ANDROID
    try
    {
        // Obtener el contexto de la aplicación y el AudioManager
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        
        // Usa "AUDIO_SERVICE" en lugar de "audio"
        audioManager = activity.Call<AndroidJavaObject>("getSystemService", "audio");
        
        if (audioManager != null)
        {
            // Guardar los volúmenes actuales de llamada, multimedia y notificaciones
            originalMediaVolume = audioManager.Call<int>("getStreamVolume", 3); // STREAM_MUSIC
            originalNotificationVolume = audioManager.Call<int>("getStreamVolume", 5); // STREAM_NOTIFICATION

            Debug.Log("Volumen de multimedia: " + originalMediaVolume);
            Debug.Log("Volumen de notificación: " + originalNotificationVolume);
        }
        else
        {
            Debug.LogError("Error: AudioManager no se pudo obtener.");
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError("Error en la obtención del AudioManager: " + e.Message);
    }
    #endif
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
            RestoreVolumesAndCloseApp();
        }
    }

    public void RestoreVolumesAndCloseApp()
    {
        #if UNITY_ANDROID
            // Restaurar los volúmenes originales
            audioManager.Call("setStreamVolume", 3, originalMediaVolume, 0); // STREAM_MUSIC
            audioManager.Call("setStreamVolume", 5, originalNotificationVolume, 0); // STREAM_NOTIFICATION

            // Cerrar la aplicación completamente
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("finishAndRemoveTask"); // Finalizar la actividad y eliminarla de tareas recientes
        #else
            Application.Quit();
        #endif
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
