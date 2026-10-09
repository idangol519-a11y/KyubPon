using System.Collections;
using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// A short slash animation drawn between an attacking block and the block it hits.
/// It plays its frames once and then removes itself.
/// </summary>
public class SandboxSlashEffect : MonoBehaviour
{
    private const string EffectName = "Slash";
    private const float SecondsPerFrame = 0.07f;

    // The effect's size compared with one grid square, so it scales with the grid.
    private const float HeightInSquares = 0.95f;
    private const float ExtraLengthInSquares = 0.9f;

    // Used only when the slash PNG frames are missing: a plain white streak.
    private const float PlainStreakHeightInSquares = 0.12f;
    private const float PlainStreakSeconds = 0.3f;

    private UnityEngine.UI.Image _image;

    /// <summary>
    /// Plays a slash from one point to another. Both points and the square size
    /// are measured inside "parent" (the grid area), the same way squares are placed.
    /// </summary>
    public static void Play(Transform parent, Vector2 from, Vector2 to, float squareSize)
    {
        GameObject effectObject = new GameObject("Slash Effect", typeof(RectTransform));
        effectObject.transform.SetParent(parent, false);

        // Halfway between the two blocks, turned to point from the attacker to its target.
        Vector2 direction = to - from;
        RectTransform rect = (RectTransform)effectObject.transform;
        rect.anchoredPosition = (from + to) / 2f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        SandboxSlashEffect effect = effectObject.AddComponent<SandboxSlashEffect>();
        effect._image = effectObject.AddComponent<UnityEngine.UI.Image>();
        effect._image.raycastTarget = false;

        Sprite[] frames = EffectFramesLoader.Load(EffectName);
        float length = direction.magnitude + squareSize * ExtraLengthInSquares;
        if (frames.Length > 0)
        {
            rect.sizeDelta = new Vector2(length, squareSize * HeightInSquares);
            effect.StartCoroutine(effect.PlayFrames(frames));
        }
        else
        {
            rect.sizeDelta = new Vector2(length, squareSize * PlainStreakHeightInSquares);
            effect.StartCoroutine(effect.PlayPlainStreak());
        }
    }

    private IEnumerator PlayFrames(Sprite[] frames)
    {
        WaitForSeconds frameWait = new WaitForSeconds(SecondsPerFrame);
        foreach (Sprite frame in frames)
        {
            _image.sprite = frame;
            yield return frameWait;
        }

        Destroy(gameObject);
    }

    private IEnumerator PlayPlainStreak()
    {
        for (float elapsed = 0f; elapsed < PlainStreakSeconds; elapsed += Time.deltaTime)
        {
            _image.color = new Color(1f, 1f, 1f, 1f - elapsed / PlainStreakSeconds);
            yield return null;
        }

        Destroy(gameObject);
    }
}
