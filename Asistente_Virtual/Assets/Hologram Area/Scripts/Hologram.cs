using UnityEngine;
using System.Collections;
using System;

public class Hologram : MonoBehaviour 
{
    public Camera[] cameras;

    public float hologramArea = 1f;

    public enum Direction
    {
        topDown, bottomUp
    };

    public Direction type = new Direction();

    void OnValidate()
    {
        //These methods only get called when an inspector value for the script has been modified.
        Scale();
        CameraRotation();
    }

    void Start()
    {
        StartCoroutine(FadeViewport());
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Scale()
    {
        // Verificar si es una tablet (o un dispositivo con una pantalla grande)
        if (Screen.width >= 1200 && Screen.height >= 1600)  // Esto es solo un ejemplo
        {
            // Aquí puedes ajustar la escala para tablets
            transform.localScale = new Vector3(1.5f, 1.5f, 1);
        }
        else
        {
            transform.localScale = new Vector3(4, 7, 4);
        }
    }

    void CameraRotation()
    {
        if (type == Direction.topDown)
        {
            cameras[1].transform.eulerAngles = new Vector3(0, 0, 0);
            cameras[2].transform.eulerAngles = new Vector3(0, 180, 180);
        }
        else if (type == Direction.bottomUp)
        {
            cameras[1].transform.eulerAngles = new Vector3(0, 0, 180);
            cameras[2].transform.eulerAngles = new Vector3(0, 180, 0);
        }

        cameras[3].transform.eulerAngles = new Vector3(0, 90, 90);
        cameras[4].transform.eulerAngles = new Vector3(0, 270, 270);
    }

    Vector2 GetMainGameViewSize()
    {
        Type T = Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
        return (Vector2)Res;
    }

    IEnumerator FadeViewport()
    {
        float elapsedTime = 0f;
        float time = 5f;

        while (elapsedTime < time)
        {
            float color = Mathf.Lerp(1f, 0f, elapsedTime / time);
            cameras[0].backgroundColor = new Color(color, color, color, color);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
}