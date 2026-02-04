using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.IO;

public class OpeningVideoManager : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName = "Map";
    public VideoClip openingClip; // Changed from string path to Asset Reference

    [Header("References")]
    public VideoPlayer videoPlayer;
    public GameObject menuUIRoot;
    public GameObject videoDisplayUI; 

    private void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            videoPlayer.targetCamera = Camera.main;
        }

        videoPlayer.loopPointReached += OnVideoEnd;
        if (videoDisplayUI) videoDisplayUI.SetActive(false);
    }

    public void PlayOpening()
    {
        if (openingClip == null)
        {
            Debug.LogError("No Opening Video Clip assigned! Starting game.");
            StartGame();
            return;
        }

        // Hide Menu
        if (menuUIRoot) menuUIRoot.SetActive(false);

        // Show Video UI (if any)
        if (videoDisplayUI) videoDisplayUI.SetActive(true);

        // Setup and Play
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = openingClip;
        videoPlayer.isLooping = false;
        videoPlayer.Play();
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        StartGame();
    }

    // Call this if Skip button is pressed too
    public void StartGame()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
