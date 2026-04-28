using UnityEngine;
using UnityEngine.UI;

public class FadeOut : MonoBehaviour
{
    public float displayTime = 2f; 
    public float fadeSpeed = 1.5f; 
    private CanvasGroup canvasGroup;
    private float timer = 0f;
    private bool fading = false;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= displayTime)
        {
            fading = true;
        }

        if (fading)
        {
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;

            if (canvasGroup.alpha <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}