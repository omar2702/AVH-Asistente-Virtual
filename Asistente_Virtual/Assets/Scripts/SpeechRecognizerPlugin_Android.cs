using UnityEngine;

public class SpeechRecognizerPlugin_Android : SpeechRecognizerPlugin
{
    public SpeechRecognizerPlugin_Android(string gameObjectName) : base(gameObjectName) { }

    private string javaClassPackageName = "com.example.eric.unityspeechrecognizerplugin.SpeechRecognizerFragment";
    private AndroidJavaClass javaClass = null;
    private AndroidJavaObject instance = null;
    private AndroidJavaObject audioManager = null;
    private int originalNotificationVolume;


    protected override void SetUp()
    {
        Debug.Log("SetUpAndroid " + gameObjectName);
        javaClass = new AndroidJavaClass(javaClassPackageName);
        javaClass.CallStatic("SetUp", gameObjectName);
        instance = javaClass.GetStatic<AndroidJavaObject>("instance");

        // Obtener el contexto de la actividad actual de Unity
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // Obtener el servicio Audio Manager
        audioManager = activity.Call<AndroidJavaObject>("getSystemService", "audio");

        if (audioManager != null)
        {
            // Ajustar el volumen de multimedia al 80%
            int maxMediaVolume = audioManager.Call<int>("getStreamMaxVolume", 3);  // STREAM_MUSIC = 3
            int newMediaVolume = (int)(maxMediaVolume * 0.8f);
            audioManager.Call("setStreamVolume", 3, newMediaVolume, 0);

            // Guardar el volumen original de notificaciones
            originalNotificationVolume = audioManager.Call<int>("getStreamVolume", 5);  // STREAM_NOTIFICATION = 5

            // Silenciar el volumen de notificaciones
            audioManager.Call("setStreamVolume", 5, 0, 0);  // STREAM_NOTIFICATION = 5
        }
    }

    void OnApplicationQuit()
    {
        // Restaurar el volumen de notificaciones al salir de la aplicación
        if (audioManager != null)
        {
            audioManager.Call("setStreamVolume", 5, originalNotificationVolume, 0);  // STREAM_NOTIFICATION = 5
        }
    }

    public override void StartListening()
    {
        instance.Call("StartListening", this.isContinuousListening, this.language, this.maxResults);
    }

    public override void StartListening(bool isContinuous = false, string newLanguage = "en-US", int newMaxResults = 10)
    {
        instance.Call("StartListening", isContinuous, language, maxResults);
    }

    public override void StopListening()
    {
        instance.Call("StopListening");
    }        

    public override void SetContinuousListening(bool isContinuous)
    {
        this.isContinuousListening = isContinuous;
        instance.Call("SetContinuousListening", isContinuous);
    }

    public override void SetLanguageForNextRecognition(string newLanguage)
    {
        this.language = newLanguage;
        instance.Call("SetLanguage", newLanguage);
    }

    public override void SetMaxResultsForNextRecognition(int newMaxResults)
    {
        this.maxResults = newMaxResults;
        instance.Call("SetMaxResults", newMaxResults);
    }    
}