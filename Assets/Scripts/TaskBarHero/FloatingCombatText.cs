using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Spawns a short-lived UI Text that rises and fades out, anchored to a
// world position projected into the given Canvas — used for damage/gold
// popups above the hero and enemy.
public class FloatingCombatText : MonoBehaviour
{
    private const float Duration = 0.9f;
    private const float RiseDistance = 65f;
    private const float FadeStartFraction = 0.5f;

    public static void Spawn(RectTransform canvasRect, Vector2 anchoredPosition, string text, Color color, int fontSize = 30)
    {
        if (canvasRect == null) return;

        var go = new GameObject("FloatingText", typeof(RectTransform));
        go.transform.SetParent(canvasRect, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition + new Vector2(Random.Range(-12f, 12f), 0f);
        rt.sizeDelta = new Vector2(240, 60);

        var textComp = go.AddComponent<Text>();
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = fontSize;
        textComp.fontStyle = FontStyle.Bold;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = color;
        textComp.text = text;
        textComp.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        outline.effectDistance = new Vector2(2f, -2f);

        var floater = go.AddComponent<FloatingCombatText>();
        floater.StartCoroutine(floater.Animate(rt, textComp));
    }

    private IEnumerator Animate(RectTransform rt, Text text)
    {
        Vector2 startPos = rt.anchoredPosition;
        Color startColor = text.color;
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / Duration;

            rt.anchoredPosition = startPos + Vector2.up * RiseDistance * p;
            rt.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, Mathf.Clamp01(p / 0.2f));

            var c = startColor;
            c.a = 1f - Mathf.Clamp01((p - FadeStartFraction) / (1f - FadeStartFraction));
            text.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}
