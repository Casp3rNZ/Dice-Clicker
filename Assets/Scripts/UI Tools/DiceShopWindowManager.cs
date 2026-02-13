using UnityEngine;

public class DiceShopWindowManager : MonoBehaviour
{
    [Header("Y Positions")]
    [SerializeField] private float closedY = -750f;
    [SerializeField] private float openY = 750f;

    [Header("Tween")]
    [Min(0f)]
    [SerializeField] private float durationSeconds = 0.35f;

    [SerializeField]
    private AnimationCurve easeInCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1.5f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [SerializeField] private bool useUnscaledTime = true;

    [Header("State")]
    [SerializeField] private bool startOpen = false;

    private RectTransform _rectTransform;
    private Coroutine _moveRoutine;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _isOpen = startOpen;
        ApplyY(_isOpen ? openY : closedY);
    }

    public void OpenShop(bool instant = false)
    {
        SetOpen(true, instant);
    }

    public void CloseShop(bool instant = false)
    {
        SetOpen(false, instant);
    }

    public void ToggleShop(bool instant = false)
    {
        SetOpen(!_isOpen, instant);
    }

    public void SetOpen(bool open, bool instant = false)
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
            return;
        }

        _moveRoutine = StartCoroutine(MoveToYRoutine(targetY));
    }

    private System.Collections.IEnumerator MoveToYRoutine(float targetY)
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
