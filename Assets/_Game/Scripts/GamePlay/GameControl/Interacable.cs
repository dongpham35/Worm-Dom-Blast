using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles interactive elements with click, hold, and drag functionality
/// </summary>
public class Interacable : MonoSingleton<Interacable>, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Action<Vector2> OnTap;
    public Action<Vector2> OnHold;
    public Action<Vector2> OnDragAction;
    public Action<bool>    OnMouseDown;

    private Vector2 _startPos;
    private Vector2 _lastSwipePos; // NEW: để theo dõi điểm trước đó trong swipe liên tục
    private float   _holdTime;
    private bool    _isHolding;
    private bool    _hasSwiped;

    [Header("Settings")]
    public float HoldThreshold  = 0.3f;   // Giữ bao lâu thì tính là hold
    public  float   SwipeThreshold = 100f; // Vuốt ít nhất bao xa để tính là swipe
    private Vector2 _lastDragPosition;
    public Vector2 LastDragPosition => _lastDragPosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        _startPos         = eventData.position;
        _lastSwipePos     = _startPos;
        _holdTime         = 0f;
        _isHolding        = true;
        _hasSwiped        = false;
        _lastDragPosition = _startPos;
        OnMouseDown?.Invoke(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isHolding && !_hasSwiped)
        {
            float totalTime = _holdTime;

            if (totalTime < HoldThreshold && Vector3.Distance(_startPos, eventData.position) < SwipeThreshold)
            {
                // Tap
                // OnTap?.Invoke(eventData.position);
                TryTapWorm(eventData.position);
            }
        }
        OnMouseDown?.Invoke(false);
        _isHolding = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - _lastDragPosition;
        _lastDragPosition = eventData.position;

        OnDragAction?.Invoke(delta);
    }

    public void UpdateLastDragPosition(Vector2 pos) => _lastDragPosition = pos;

    private void Update()
    {
        return;
        if (_isHolding)
        {
            _holdTime += Time.deltaTime;
            if(_holdTime >= HoldThreshold && Vector3.Distance(_startPos, _lastDragPosition) < SwipeThreshold)
            {
                //Hold
                OnHold?.Invoke(_startPos);
                _isHolding = false;
            }
        }
    }

    private bool TryTapWorm(Vector2 tapPosition)
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3(tapPosition.x, tapPosition.y, Camera.main.transform.position.y));
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            var worm = hitInfo.collider.GetComponentInParent<WormControl>();
            if (worm != null)
            {
                worm.ChoseWorm();
                return true;
            }
        }

        return false;
    }
}
