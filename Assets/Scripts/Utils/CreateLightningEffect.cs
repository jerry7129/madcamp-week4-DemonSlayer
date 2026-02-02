using UnityEngine;
using UnityEditor;

public class CreateLightningEffect : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create Lightning Effect")]
    static void Create()
    {
        GameObject go = new GameObject("LightningEffect");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        // 1. Main Module & Randomness
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        
        // Speed determines LENGTH in Stretched Billboard mode
        // Randomize speed to get "Random Lengths" (Glitchy)
        main.startSpeed = new ParticleSystem.MinMaxCurve(-10f, -40f); 
        
        // Stretched Billboard uses 'startSize' as WIDTH (Thickness)
        // INCREASED WIDTH to prevent sub-pixel blurring
        main.startSize3D = false; 
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); // Thicker, chunky pixels per unit
        
        // MIX COLORS (Blue & Yellow)
        ParticleSystem.MinMaxGradient gradient = new ParticleSystem.MinMaxGradient(
            new Color(0.0f, 0.8f, 1f, 1f), // Cyan
            new Color(1f, 0.9f, 0.2f, 1f)  // Yellow
        );
        gradient.mode = ParticleSystemGradientMode.TwoColors; 
        main.startColor = gradient;
        
        main.simulationSpace = ParticleSystemSimulationSpace.World; 
        main.playOnAwake = false;

        // 2. Emission (Burst + Distance)
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.rateOverDistance = 40f; 
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        // 3. Shape (TUBE/VOLUME)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f; 
        shape.radius = 0.3f; // Spawn in a thick tube (Multiple strands feel)

        // 4. Noise (Visual Chaos)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.5f; // Stronger jitter (Zigzag)
        noise.frequency = 10.0f; 
        noise.quality = ParticleSystemNoiseQuality.High;

        // 5. Trails
        var trails = ps.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.lifetime = new ParticleSystem.MinMaxCurve(0.1f);
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0))); 

        // Size over Lifetime (Fade out)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));

        // 6. Renderer (PIXEL ART STYLE)
        // Use a Hard-Edge Square Texture for "Pixel" look
        Texture2D pixelTex = new Texture2D(1, 1);
        pixelTex.SetPixel(0, 0, Color.white);
        pixelTex.filterMode = FilterMode.Point; // Critical for Pixel Look
        pixelTex.Apply();

        // ALIGN TO DIRECTION Logic:
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.cameraVelocityScale = 0f; // Ignore Camera
        psr.velocityScale = 0.02f; // REDUCED scale (Less stretch = More blocky)
        psr.lengthScale = 1f;
        
        Material pixelMat = new Material(Shader.Find("Sprites/Default"));
        pixelMat.mainTexture = pixelTex;
        
        psr.material = pixelMat;
        psr.trailMaterial = pixelMat; // Apply to Trails too!
        
        Selection.activeGameObject = go;
    }
#endif
}
