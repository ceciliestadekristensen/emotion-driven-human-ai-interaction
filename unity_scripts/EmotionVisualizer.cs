using UnityEngine;

/// <summary>
/// Visualises the current emotion on a simple cube:
/// - Smoothly lerps the material colour
/// - Plays a small animation depending on emotion
/// 
/// Used from UIController via:
///     visualizer.SetEmotion(emotion, color);
/// </summary>
public class EmotionVisualizer : MonoBehaviour
{
    [Header("Target")]
    public Renderer targetRenderer;          // The cube's renderer

    [Header("Colour")]
    public float colorLerpSpeed = 5f;        // How fast we blend to new colour

    [Header("Animation strengths")]
    public float happyBounceHeight = 0.25f;  // Up/down for "happy"
    public float happyScalePulse = 0.05f;    // Scale pulse for "happy"

    public float stressedWobbleAngle = 10f;  // Degrees left/right for "stressed"

    public float sadSinkAmount = 0.15f;      // How far it "sinks" down for "sad"

    public float neutralRotateSpeed = 15f;   // Degrees per second around Y

    // --- internal state ---
    private string currentEmotion = "neutral";
    private Color currentColor = Color.white;
    private Color targetColor = Color.white;

    private Vector3 basePosition;
    private Vector3 baseScale;
    private Quaternion baseRotation;

    private float animTime = 0f;

    void Awake()
    {
        // Cache the cube's original transform as our "resting" state
        basePosition = transform.position;
        baseScale = transform.localScale;
        baseRotation = transform.rotation;

        if (targetRenderer != null)
        {
            currentColor = targetRenderer.material.color;
            targetColor = currentColor;
        }
    }

    /// <summary>
    /// Called from UIController whenever Python sends a new emotion + colour.
    /// </summary>
    public void SetEmotion(string emotion, Color color)
    {
        if (string.IsNullOrEmpty(emotion))
            emotion = "neutral";

        currentEmotion = emotion.ToLower();
        targetColor = color;

        // Restart animation time for a nice "pop-in" each time emotion changes
        animTime = 0f;
    }

    void Update()
    {
        animTime += Time.deltaTime;

        // --- Smooth colour blending ---
        if (targetRenderer != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);
            targetRenderer.material.color = currentColor;
        }

        // --- Reset transform before applying per-emotion offsets ---
        transform.position = basePosition;
        transform.localScale = baseScale;
        transform.rotation = baseRotation;

        // --- Per-emotion animation ---
        switch (currentEmotion)
        {
            case "happy":
                AnimateHappy();
                break;

            case "stressed":
                AnimateStressed();
                break;

            case "sad":
                AnimateSad();
                break;

            case "neutral":
            default:
                AnimateNeutral();
                break;
        }
    }

    private void AnimateHappy()
    {
        // bounce +  pulse
        float t = animTime * 3f; // speed
        float bounce = Mathf.Abs(Mathf.Sin(t)) * happyBounceHeight;

        float scalePulse = 1f + Mathf.Sin(t) * happyScalePulse;

        transform.position = basePosition + Vector3.up * bounce;
        transform.localScale = baseScale * scalePulse;
    }

    private void AnimateStressed()
    {
        // Quick wobble around Z (like it's a bit shaky)
        float t = animTime * 6f;
        float angle = Mathf.Sin(t) * stressedWobbleAngle;

        transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void AnimateSad()
    {
        // Sinks slightly below original position and feels a bit "smaller"
        float t = animTime * 2f;
        // Small soft breathing, but overall lower
        float offset = -sadSinkAmount + Mathf.Sin(t) * 0.02f;

        transform.position = basePosition + Vector3.up * offset;
        transform.localScale = baseScale * 0.95f;
    }

    private void AnimateNeutral()
    {
        // Calm, slow rotation around Y-axis
        float angle = neutralRotateSpeed * animTime;
        transform.rotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
    }
}
