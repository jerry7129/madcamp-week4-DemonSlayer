using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // 6. Renderer (PIXEL ART STYLE) - PERSISTENT ASSET FIX
        
        string matPath = "Assets/Materials/PixelParticle.mat";
        string texPath = "Assets/Materials/PixelSquare.png";

        // A. Ensure Texture Exists
        Texture2D pixelTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (pixelTex == null)
        {
            pixelTex = new Texture2D(32, 32); // Slightly larger for MIP preservation if needed, but 1x1 is fine too.
            // Fill white
            Color[] pixels = new Color[32 * 32];
            for(int i=0; i<pixels.Length; i++) pixels[i] = Color.white;
            pixelTex.SetPixels(pixels);
            pixelTex.Apply();

            byte[] bytes = pixelTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(texPath, bytes);
            AssetDatabase.Refresh();
            
            // Re-import to set Filter Mode
            TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureType = TextureImporterType.Default;
                importer.SaveAndReimport();
            }
            
            pixelTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath); // Load fresh
        }

        // B. Ensure Material Exists
        Material pixelMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (pixelMat == null)
        {
            pixelMat = new Material(Shader.Find("Particles/Standard Unlit")); // Better for particles
            pixelMat.mainTexture = pixelTex;
            
            // Allow Vertex Color (StartColor/ColorOverLifetime) to tint it
            // Standard Unlit usually allows this by default.
            
            // Set Blend Mode to Additive or Alpha Blended? 
            // Lightning usually looks good Additive, but "Pixel Art" might want Alpha.
            // Let's stick to Standard Unlit default (Opaque/Cutout/Fade/Transparent).
            // We'll set it to Fade or Transparent.
            
            pixelMat.SetFloat("_Mode", 2); // Fade
            // Fix keywords for Fade
            pixelMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            pixelMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            pixelMat.SetInt("_ZWrite", 0);
            pixelMat.DisableKeyword("_ALPHATEST_ON");
            pixelMat.EnableKeyword("_ALPHABLEND_ON");
            pixelMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            pixelMat.renderQueue = 3000;

            AssetDatabase.CreateAsset(pixelMat, matPath);
        }
        
        // C. Assign
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.cameraVelocityScale = 0f; 
        psr.velocityScale = 0.02f; 
        psr.lengthScale = 1f;
        
        psr.material = pixelMat;
        psr.trailMaterial = pixelMat;
        
        Selection.activeGameObject = go;
    }
#endif
}
