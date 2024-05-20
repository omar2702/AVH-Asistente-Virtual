using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using TMPro;
using System.Text;
using System;
using UnityEngine.Events;

public class Avatar : MonoBehaviour
{
    public Animation avatarAniamtion;
    public string animationName = "Explicar";

    private AudioClip clip; //audio o grabación del estudiante
    private bool isRecording = false;
    private byte[] audioBytesUser; //bytes del audio del estudiante
    private float lastSoundTime = 0f;
    public static event System.Action Completed; //evento que se dispara cuando se terminó de reproducir la respuesta
    private List<DataGPT> background = new List<DataGPT>(); //historial
    [SerializeField] private AudioClip bell;

    void Start() {
        // Comprueba que el componente Animator esté asignado
        if (avatarAniamtion == null)
        {
            avatarAniamtion = GetComponent<Animation>();
        }

        // Llama al método para reproducir el audio con la animación
    }


        void Update()
    {
        if(isRecording) { //detectar si no se habla durante 3 segundos para parar de grabar y mandarlo a lambda
            if (clip != null && Microphone.GetPosition(null) > 0 && IsSilent()) {
                
                if (Time.time - lastSoundTime > 3.0f) {
                    StopRecording();
                }
            }
            else {
                lastSoundTime = Time.time;
            }
        }     
    }

    public void StartRecording() { //esta función se llama desde SpeechRecognizer
        clip = Microphone.Start(null, false, 15, 44100);
        isRecording = true;
        lastSoundTime = Time.time;
    }

    public void StopRecording() {
        if (!isRecording) return;
        var position = Microphone.GetPosition(null);
        Microphone.End(null);
        var samples = new float[position * clip.channels];
        clip.GetData(samples, 0);
        isRecording = false;
        audioBytesUser = EncodeAsWAV(samples, clip.frequency, clip.channels);
        ControllerSound.Instance.ExecuteSound(bell);
        StartCoroutine(SendRecording());
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
        return averageLevel < threshold;
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

        var request = new UnityWebRequest("https://h3f4f43iybmddlglvzxbe23ksm0yampi.lambda-url.us-east-2.on.aws/", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Status Code: " + request.responseCode + " Message: " + request.downloadHandler.text);
        } 
        else {
            ControllerSound.SoundCompleted += CompletedResponse; //suscribirse al evento que indica cuando terminó de reproducirse la respuesta
            var jsonResponse = request.downloadHandler.text;
            Debug.Log(jsonResponse);
            // Analizar el JSON
            ResponseApi response = JsonUtility.FromJson<ResponseApi>(jsonResponse);
            string audioBase64 = response.response_gpt_voice_base64;

            background.Clear();
            background.AddRange(response.background_updated);
            
            var audioBytesResponse = Convert.FromBase64String(audioBase64);
            var tempPath = Application.persistentDataPath + "tmpMP3Base64.mp3";
            File.WriteAllBytes(tempPath, audioBytesResponse);
            System.Uri _uri = new Uri(tempPath);
            UnityWebRequest requestAudio = UnityWebRequestMultimedia.GetAudioClip(_uri, AudioType.MPEG);
            yield return requestAudio.SendWebRequest();

            if (requestAudio.result.Equals(UnityWebRequest.Result.ConnectionError))
                Debug.LogError(requestAudio.error);
                
            else {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(requestAudio);
                ControllerSound.Instance.ExecuteSound(audioClip); // Audio
                                                                  // Configurar la animación para que se reproduzca en bucle
                AnimationState animationState = avatarAniamtion["Explicar"]; // Reemplaza "Explicar" con el nombre de tu animación
                animationState.wrapMode = WrapMode.Loop;

                // Iniciar la animación en bucle
                avatarAniamtion.Play("Explicar");

                // Iniciar la corutina para detener la animación al final del audio
                StartCoroutine(StopAnimationWhenAudioEnds(animationState));
            }
        }
    }

    private IEnumerator StopAnimationWhenAudioEnds(AnimationState animationState) 
    {
        yield return new WaitForSeconds(clip.length);
        // Detener la animación después de que el audio termine
        avatarAniamtion.Stop("Explicar");

        // Opcionalmente, restablecer la animación al primer frame
        animationState.time = 0;
        avatarAniamtion.Sample();
        avatarAniamtion.Stop();
    }

    public void CompletedResponse(){// funcion que se invoca cuando se terminó de reroducir la respuesta
        ControllerSound.SoundCompleted -= CompletedResponse; //desuscribirse al evento del audio de la respuesta
        //es importante desuscribirse y suscribirse solo cuando es necesario, porque son dos clases quienes usan los eventos de ControllerSound
        Completed?.Invoke();// se dispara el evento indicando que se terminó de reproducir el audio
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
    }
    [System.Serializable]
    public class DataGPT {
        public string role;
        public string content;
    }
}