using UnityEngine;

public class SimpleColor : MonoBehaviour
{
    public Color targetColor = Color.white;

    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            // Create a temporary material instance to avoid leaking
            r.material.color = targetColor;
        }
    }
}
