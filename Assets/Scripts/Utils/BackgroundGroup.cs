using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BackgroundGroup : MonoBehaviour
{
    [Header("Settings")]
    public float fadeDuration = 1.0f;
    public bool startVisible = false;

    private List<SpriteRenderer> sprites = new List<SpriteRenderer>();
    private Coroutine fadeRoutine;
    // private float targetAlpha = 1f; // Unused

    void Start()
    {
        // Gather all sprites in this group (including children created in Awake by ParallaxLayer)
        sprites.AddRange(GetComponentsInChildren<SpriteRenderer>(true));

        if (startVisible)
        {
            SetAlpha(1f);
            gameObject.SetActive(true);
        }
        else
        {
            SetAlpha(0f);
            gameObject.SetActive(false);
        }
    }

    public void FadeIn()
    {
        gameObject.SetActive(true);
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f));
    }

    public void FadeOut()
    {
        if (!gameObject.activeInHierarchy) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(0f));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float startAlpha = GetCurrentAverageAlpha();
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            float newAlpha = Mathf.Lerp(startAlpha, target, t);
            SetAlpha(newAlpha);
            yield return null;
        }

        SetAlpha(target);

        // Disable object if fully invisible to save performance
        if (target <= 0.01f)
        {
            gameObject.SetActive(false);
        }
    }

    private void SetAlpha(float alpha)
    {
        foreach (var sr in sprites)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }

    private float GetCurrentAverageAlpha()
    {
        if (sprites.Count == 0) return 0f;
        if (sprites[0] == null) return 0f;
        return sprites[0].color.a;
    }
}
