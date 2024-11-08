using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using TMPro;
using System.Text;
using System;
using UnityEngine.Events;
using UnityEngine.AI;

public class Avatar : MonoBehaviour
{
    public Animator avatarAnimator;

    private static readonly int WaitTrigger = Animator.StringToHash("AHWait");
    private static readonly int ExplainTrigger = Animator.StringToHash("AHExplain");
    private static readonly int StandTrigger = Animator.StringToHash("AHStand");
    private static readonly int GreetingsTrigger = Animator.StringToHash("AHGreetings");
    private static readonly int InactiveTrigger = Animator.StringToHash("AHInactive");

    private AudioClip clip; //audio o grabación del estudiante
    private AudioClip responseClip; //Sergio 22/05/2024 audio respuesta de gpt
    private bool isRecording = false;
    private int recordingDuration = 18;
    private Coroutine recordingCoroutine;
    private byte[] audioBytesUser; //bytes del audio del estudiante
    private float lastSoundTime = 0f;
    private bool spokeOnce = false;
    public static event System.Action Inactivated; //evento que se dispara cuando se terminó de reproducir la respuesta
    private List<DataGPT> background = new List<DataGPT>(); //historial
    private bool active = false;//Sergio 04/06/2024
    private bool error = false;//Sergio 05/06/2024
    private string action = "";//Sergio 08/06/2024
    // [SerializeField] private AudioClip bell;
    [SerializeField] private AudioClip waitSound1; //Sergio 22/05/2024
    [SerializeField] private AudioClip waitSound2; //Sergio 22/05/2024
    [SerializeField] private AudioClip waitSound3; //Sergio 22/05/2024
    [SerializeField] private AudioClip waitSound4; //Sergio 23/05/2024
    [SerializeField] private AudioClip errorSound; //Sergio 23/05/2024
    [SerializeField] private AudioClip inactiveSound; //Sergio 04/06/2024

    void Start() {
        avatarAnimator = GetComponent<Animator>();

        // Bloquea la orientación a horizontal
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        // Asegúrate de que la orientación se bloquea y no pueda cambiar
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        // Forzar pantalla completa
        Screen.fullScreen = true;

        AnimationStand();
    }

    void Update() {
        if(isRecording) { //detectar si no se habla durante 3 segundos para parar de grabar y mandarlo a lambda
            if (clip != null && Microphone.GetPosition(null) > 0 && IsSilent()) {
                
                if ((Time.time - lastSoundTime > 2.5f) && spokeOnce == true) {
                    GenerateResponse();
                }
                else if ((Time.time - lastSoundTime > 9.0f) && spokeOnce == false) {
                    Inactivate();
                }
            }
            else {
                lastSoundTime = Time.time;
            }
        }     
    }

    public bool getActive() {
        return this.active;
    }

    public void setActive(bool active) {
        this.active = active;
    }

    IEnumerator StopRecordingAfterDuration(int duration) {
        // Espera durante el tiempo especificado
        yield return new WaitForSeconds(duration);

        if (isRecording && this.spokeOnce)
        {
            GenerateResponse();
        }
    }

    public void StartRecording() { //esta función se llama desde SpeechRecognizer
        //Sergio 04/06/2024
        if (active)
            ControllerSound.SoundCompleted -= StartRecording;
        //fin sergio
        clip = Microphone.Start(null, false, recordingDuration, 44100);
        isRecording = true;
        spokeOnce = false;
        active = true;//Sergio 04/06/2024
        lastSoundTime = Time.time;
        recordingCoroutine = StartCoroutine(StopRecordingAfterDuration(recordingDuration));
        AnimationStand();
    }

        //Sergio 04/06/2024
    public void GenerateResponse() {
        StopRecording();
        StartCoroutine(SendRecording());
    }
    //fin Sergio
    //Sergio 04/06/2024
    public void Inactivate() {
        StopRecording();
        responseClip = inactiveSound;
        ControllerSound.SoundCompleted += CompletelyInactivated;
        ControllerSound.Instance.ExecuteSound(responseClip);
        AnimationInactive();
    }
    //fin Sergio

    public void StopRecording() {
        if (!isRecording) return;
        var position = Microphone.GetPosition(null);
        Microphone.End(null);
        if (recordingCoroutine != null) {
            StopCoroutine(recordingCoroutine);
            recordingCoroutine = null;
        }
        var samples = new float[position * clip.channels];
        clip.GetData(samples, 0);
        isRecording = false;
        audioBytesUser = EncodeAsWAV(samples, clip.frequency, clip.channels);
        // ControllerSound.Instance.ExecuteSound(bell);
    }

    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels) { //obtener los bytes del audio
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2)) {
            using (var writer = new BinaryWriter(memoryStream)) {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples) {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }

    private bool IsSilent() {
        int sampleWindow = 128; 
        float[] samples = new float[sampleWindow];
        int microphonePosition = Microphone.GetPosition(null) - sampleWindow + 1;
        if (microphonePosition < 0) return false;
        clip.GetData(samples, microphonePosition);
        float averageLevel = GetAverageVolume(samples);
        float threshold = 0.03f; 
        if (averageLevel < threshold) {
            return true;
        }
        spokeOnce = true;
        return false;
    }

    private float GetAverageVolume(float[] samples) {
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        return sum / samples.Length;
    }      

    private IEnumerator SendRecording() {
        string base64String = System.Convert.ToBase64String(audioBytesUser);
        // Convertir el diccionario a JSON
        FileData fileData = new FileData();
        fileData.file = base64String;
        fileData.background = background;
        string json = JsonUtility.ToJson(fileData);

        //Sergio 22/05/2024
        AudioClip[] waitSounds = new AudioClip[] { waitSound1, waitSound2, waitSound3, waitSound4 };
        AudioClip waitSound = waitSounds[UnityEngine.Random.Range(0, waitSounds.Length)];
        ControllerSound.Instance.ExecuteSound(waitSound);
        AnimationWait(); // Omar 29/05/2024
        // Iniciar la corutina para esperar al final del audio y cambiar la animación
        //StartCoroutine(SynchronizeAnimationWithAudio(waitSound.length, PlayResponseClip));
        //Fin Sergio

        var request = new UnityWebRequest("https://h3f4f43iybmddlglvzxbe23ksm0yampi.lambda-url.us-east-2.on.aws/", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Status Code: " + request.responseCode + " Message: " + request.downloadHandler.text);
            //Sergio 23/05/2024
            error = true;//Sergio 05/06/2024
            responseClip = errorSound;
            if (!ControllerSound.Instance.IsPlaying()) {           
                PlayResponseClip();
            } else {
                //Esperar que termine el audio de espera
                ControllerSound.SoundCompleted += WaitForSoundAndPlayResponse;
            }
            //Fin Sergio
        } 
        else {
            var jsonResponse = request.downloadHandler.text;
            Debug.Log(jsonResponse);
            // Analizar el JSON
            ResponseApi response = JsonUtility.FromJson<ResponseApi>(jsonResponse);
            string audioBase64 = response.response_gpt_voice_base64;
            action = response.action;//Sergio 08/06/2024

            background.Clear();
            background.AddRange(response.background_updated);
            
            var audioBytesResponse = Convert.FromBase64String(audioBase64);
            var tempPath = Application.persistentDataPath + "tmpMP3Base64.mp3";
            File.WriteAllBytes(tempPath, audioBytesResponse);
            System.Uri _uri = new Uri(tempPath);
            UnityWebRequest requestAudio = UnityWebRequestMultimedia.GetAudioClip(_uri, AudioType.MPEG);
            yield return requestAudio.SendWebRequest();

            if (requestAudio.result.Equals(UnityWebRequest.Result.ConnectionError)){
                //Sergio 24/05/2024
                error = true;//Sergio 05/06/2024
                responseClip = errorSound;
                if (!ControllerSound.Instance.IsPlaying()) {
                    PlayResponseClip();
                } else {
                    //Esperar que termine el audio de espera
                    ControllerSound.SoundCompleted += WaitForSoundAndPlayResponse;
                }
                //Fin Sergio
            }    
            else {
                //Sergio 22/05/2024
                error = false;//Sergio 05/06/2024
                responseClip = DownloadHandlerAudioClip.GetContent(requestAudio);
                if (!ControllerSound.Instance.IsPlaying()) { //se terminó de reproducir el audio de espera
                    PlayResponseClip();
                } else {
                    //Esperar que termine el audio de espera
                    ControllerSound.SoundCompleted += WaitForSoundAndPlayResponse;
                }
                //Fin Sergio
            }
        }
    }

    //Sergio 22/05/2024
    public void WaitForSoundAndPlayResponse() {
        ControllerSound.SoundCompleted -= WaitForSoundAndPlayResponse;
        PlayResponseClip();
    }

    //Sergio 22/05/2024
    public void PlayResponseClip() {
        // Suscribirse al evento que indica cuando terminó de reproducirse la respuesta
        //Sergio 05/06/2024
        if(error || action.ToLower() == "descansar") {
            ControllerSound.SoundCompleted += CompletelyInactivated;
        }
        else {
            ControllerSound.SoundCompleted += StartRecording;
        }
        //fin Sergio
        ControllerSound.Instance.ExecuteSound(responseClip);
        AnimationExplain();
    }

    public void CompletelyInactivated(){
        active = false;//Sergio 05/06/2024
        ControllerSound.SoundCompleted -= CompletelyInactivated; //desuscribirse al evento del audio de la respuesta
        //es importante desuscribirse y suscribirse solo cuando es necesario, porque son dos clases quienes usan los eventos de ControllerSound
        Inactivated?.Invoke();// se dispara el evento indicando que se terminó de reproducir el audio
        AnimationStand();
    }
    

    [System.Serializable]
    public class FileData {
        public string file;
        public List<DataGPT> background;
    }
    [System.Serializable]
    public class ResponseApi {
        public string response_gpt_voice_base64;
        public List<DataGPT> background_updated;
        public string action; //Sergio 08/06/2024
    }
    [System.Serializable]
    public class DataGPT {
        public string role;
        public string content;
    }
        //Animaciones
    public void AnimationWait() // Animacion Esperar
    {       
         avatarAnimator.SetTrigger(WaitTrigger);
         StartCoroutine(ReturnToStandAfterAnimation("Wait"));//Sergio 05/06/2024
    }

    public void AnimationStand()
    {
        avatarAnimator.SetTrigger(StandTrigger);
    }

    public void AnimationExplain()
    {
        // Sincronizar la animación "Explain" con el audio y cambiar a "Stand" cuando termine
        avatarAnimator.SetTrigger(ExplainTrigger);
        // StartCoroutine(ReturnToStandAfterAnimation("Explain"));
    }
    //Sergio 05/06/2024
    private IEnumerator ReturnToStandAfterAnimation(string animationName) {
        yield return new WaitUntil(() => !avatarAnimator.GetCurrentAnimatorStateInfo(0).IsName(animationName));
        AnimationStand();
    }
    //fin sergio

    public void AnimationListen()
    {
        avatarAnimator.SetTrigger(GreetingsTrigger);
    }

    public void AnimationInactive()
    {
        avatarAnimator.SetTrigger(InactiveTrigger);
        StartCoroutine(ReturnToStandAfterAnimation("Inactive"));
    }
}