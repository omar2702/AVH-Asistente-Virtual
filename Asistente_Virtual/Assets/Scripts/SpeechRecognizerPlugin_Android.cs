using UnityEngine;

public class SpeechRecognizerPlugin_Android : SpeechRecognizerPlugin
{
    public SpeechRecognizerPlugin_Android(string gameObjectName) : base(gameObjectName) { }

    private string javaClassPackageName = "com.example.eric.unityspeechrecognizerplugin.SpeechRecognizerFragment";
    private AndroidJavaClass javaClass = null;
    private AndroidJavaObject instance = null;
    private AndroidJavaObject audioManager = null;

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

                // Silenciar el volumen de notificaciones
        if (audioManager != null)
        {
            audioManager.Call("setStreamVolume", 5, 0, 0);  // STREAM_NOTIFICATION = 5
        }
    }

    void OnApplicationQuit()
    {
        // Restaurar el volumen de notificaciones al salir de la aplicación
        if (audioManager != null)
        {
            int maxVolume = audioManager.Call<int>("getStreamMaxVolume", 5);  // STREAM_NOTIFICATION = 5
            audioManager.Call("setStreamVolume", 5, maxVolume, 0);
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