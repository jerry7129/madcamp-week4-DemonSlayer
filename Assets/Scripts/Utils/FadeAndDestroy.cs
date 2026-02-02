using UnityEngine;
using System.Collections;

public class FadeAndDestroy : MonoBehaviour
{
    public float delay = 0.05f; // Wait before fading
    public float fadeDuration = 0.3f;
    
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        yield return new WaitForSeconds(delay);

        float timer = 0f;
        Color startColor = sr.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / fadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}
