using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for all sliding UI window panels.
/// Provides animated open/close tween and mutual exclusivity —
/// opening one window automatically closes all others.
/// </summary>
public abstract class UIWindow : MonoBehaviour
{
    private static readonly List<UIWindow> _allWindows = new List<UIWindow>();

    [Header("Window Y Positions")]
    [SerializeField] private float closedY = -750f;
    [SerializeField] private float openY = 750f;

    [Header("Window Tween")]
    [Min(0f)]
    [SerializeField] private float durationSeconds = 0.35f;

    [SerializeField]
    private AnimationCurve easeInCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1.5f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [SerializeField] private bool useUnscaledTime = true;

    [Header("Window State")]
    [SerializeField] private bool startOpen = false;

    private RectTransform _rectTransform;
    private Coroutine _moveRoutine;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    protected virtual void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _isOpen = startOpen;
        ApplyY(_isOpen ? openY : closedY);

        if (!_allWindows.Contains(this))
            _allWindows.Add(this);
    }

    protected virtual void OnDestroy()
    {
        _allWindows.Remove(this);
    }

    // ───────────────────────── Public API ─────────────────────────
    /// <summary>
    /// Opens this window and closes every other open window.
    /// </summary>
    public void Open(bool instant = false)
    {
        CloseAllExcept(this);
        SetOpen(true, instant);
    }

    /// <summary>
    /// Closes this window.
    /// </summary>
    public void Close(bool instant = false)
    {
        SetOpen(false, instant);
    }

    /// <summary>
    /// Toggles this window. If opening, closes all other windows first.
    /// </summary>
    public void Toggle(bool instant = false)
    {
        if (_isOpen)
            Close(instant);
        else
            Open(instant);
    }

    // ───────────────────────── Internals ──────────────────────────

    private void SetOpen(bool open, bool instant)
    {
        _isOpen = open;
        float targetY = open ? openY : closedY;

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        if (instant || durationSeconds <= 0f)
        {
            ApplyY(targetY);
            OnWindowStateChanged(open);
            return;
        }

        _moveRoutine = StartCoroutine(MoveToYRoutine(targetY, open));
    }

    /// <summary>
    /// Override in subclasses to react when the window finishes opening or closing.
    /// </summary>
    protected virtual void OnWindowStateChanged(bool opened) { }

    private IEnumerator MoveToYRoutine(float targetY, bool opening)
    {
        float startY = GetCurrentY();
        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, durationSeconds);

        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = easeInCurve != null ? Mathf.Clamp01(easeInCurve.Evaluate(t)) : t;

            ApplyY(Mathf.LerpUnclamped(startY, targetY, easedT));
            yield return null;
        }

        ApplyY(targetY);
        _moveRoutine = null;
        OnWindowStateChanged(opening);
    }

    private static void CloseAllExcept(UIWindow except)
    {
        for (int i = 0; i < _allWindows.Count; i++)
        {
            UIWindow w = _allWindows[i];
            if (w != except && w.IsOpen)
                w.Close();
        }
    }

    private float GetCurrentY()
    {
        if (_rectTransform != null)
            return _rectTransform.anchoredPosition.y;

        return transform.localPosition.y;
    }

    private void ApplyY(float y)
    {
        if (_rectTransform != null)
        {
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.y = y;
            _rectTransform.anchoredPosition = pos;
            return;
        }

        Vector3 pos3 = transform.localPosition;
        pos3.y = y;
        transform.localPosition = pos3;
    }
}
