using UnityEngine;
using System.Collections.Generic;

public class BackgroundTrigger : MonoBehaviour
{
    [Header("Target Group")]
    [Tooltip("The group to FADE IN when entering this zone")]
    public BackgroundGroup targetGroup;

    [Header("Groups to Hide")]
    [Tooltip("All OTHER groups to FADE OUT when entering this zone")]
    public List<BackgroundGroup> otherGroups;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if it's the player
        if (collision.CompareTag("Player") || collision.gameObject.name == "Zenitsu")
        {
            SwitchBackground();
        }
    }

    public void SwitchBackground()
    {
        if (targetGroup != null)
        {
            targetGroup.FadeIn();
            Debug.Log($"Background Switched to: {targetGroup.name}");
        }

        foreach (var group in otherGroups)
        {
            if (group != null && group != targetGroup)
            {
                group.FadeOut();
            }
        }
    }
}
