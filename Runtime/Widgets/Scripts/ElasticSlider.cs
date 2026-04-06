using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConceptFactory.UIEssentials.Widgets
{
    /// <summary>
    /// Drag axis for <see cref="ElasticSlider"/>.
    /// </summary>
    public enum ElasticSliderAxis
    {
        Horizontal = 0,
        Vertical = 1
    }

    /// <summary>
    /// One-axis elastic thumb: drag like a joystick half-axis; on release returns to center.
    /// Normalized value is in [-1, 1] with optional power curve so response accelerates toward the ends of travel.
    /// </summary>
    public sealed class ElasticSlider : VisualElement
    {
        public const string UxmlPath = "Widgets/ElasticSlider/ElasticSlider";

        private const string UssClassRoot = "elastic-slider";
        private const string UssClassHorizontal = "elastic-slider--horizontal";
        private const string UssClassVertical = "elastic-slider--vertical";
        private const string UssClassThumbDragging = "elastic-slider__thumb--dragging";
        private const string UssClassPillHorizontal = "elastic-slider__pill--horizontal";
        private const string UssClassPillVertical = "elastic-slider__pill--vertical";

        private VisualElement _pill;
        private VisualElement _track;
        private VisualElement _thumb;
        private Label _caption;

        private ElasticSliderAxis _axis = ElasticSliderAxis.Horizontal;
        private float _accelerationExponent = 1.35f;
        private float _directionDeadZone = 0.02f;
        private float _maxTravelPixels = 40f;

        private bool _dragging;
        private int _activePointerId = -1;

        /// <summary>Normalized value in [-1, 1] after power mapping (sticky center when exponent &gt; 1).</summary>
        public float Value { get; private set; }

        /// <summary>Raw linear position in [-1, 1] before acceleration curve.</summary>
        public float LinearValue { get; private set; }

        public ElasticSliderAxis Axis
        {
            get => _axis;
            set
            {
                _axis = value;
                ApplyAxisClasses();
            }
        }

        /// <summary>
        /// Curve exponent on |linear|. Values &gt; 1 keep the center less sensitive and ramp faster near ±1 (physical travel feels “accelerated” at extremes).
        /// </summary>
        public float AccelerationExponent
        {
            get => _accelerationExponent;
            set => _accelerationExponent = Mathf.Max(1.01f, value);
        }

        /// <summary>Linear norm magnitude below this is treated as neutral for <see cref="DirectionChanged"/> (0–1 scale).</summary>
        public float DirectionDeadZone
        {
            get => _directionDeadZone;
            set => _directionDeadZone = Mathf.Clamp01(value);
        }

        /// <summary>Fired every pointer move while dragging; argument is mapped value in [-1, 1].</summary>
        public event Action<float> ValueChanged;

        /// <summary>Fired when linear travel crosses neutral: -1 negative, 0 center band, +1 positive.</summary>
        public event Action<int> DirectionChanged;

        /// <summary>Started dragging (pointer captured).</summary>
        public event Action DragStarted;

        /// <summary>Released; argument is final mapped value (0 after snap).</summary>
        public event Action<float> DragEnded;

        /// <summary>Optional centered label drawn behind the thumb (e.g. hint text).</summary>
        public string Caption
        {
            get => _caption != null ? _caption.text : string.Empty;
            set
            {
                if (_caption != null)
                    _caption.text = value ?? string.Empty;
            }
        }

        private int _lastSign;

        public ElasticSlider()
        {
            AddToClassList(UssClassRoot);

            var tree = Resources.Load<VisualTreeAsset>(UxmlPath);
            if (tree == null)
            {
                Debug.LogError($"[ElasticSlider] Missing VisualTreeAsset at Resources/{UxmlPath}");
                return;
            }

            tree.CloneTree(this);
            _pill = this.Q<VisualElement>("pill");
            _track = this.Q<VisualElement>("track");
            _thumb = this.Q<VisualElement>("thumb");
            _caption = this.Q<Label>("caption");

            if (_track == null || _thumb == null)
            {
                Debug.LogError("[ElasticSlider] UXML must define 'track' and 'thumb'.");
                return;
            }

            ApplyAxisClasses();
            RegisterCallback<GeometryChangedEvent>(_ => RefreshMaxTravelFromLayout());

            _track.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _track.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _track.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            _track.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
        }

        private void ApplyAxisClasses()
        {
            RemoveFromClassList(UssClassHorizontal);
            RemoveFromClassList(UssClassVertical);
            if (_axis == ElasticSliderAxis.Horizontal)
                AddToClassList(UssClassHorizontal);
            else
                AddToClassList(UssClassVertical);

            if (_pill != null)
            {
                _pill.RemoveFromClassList(UssClassPillHorizontal);
                _pill.RemoveFromClassList(UssClassPillVertical);
                if (_axis == ElasticSliderAxis.Horizontal)
                    _pill.AddToClassList(UssClassPillHorizontal);
                else
                    _pill.AddToClassList(UssClassPillVertical);
            }
        }

        private void RefreshMaxTravelFromLayout()
        {
            if (_track == null || _thumb == null)
                return;

            float trackSize = _axis == ElasticSliderAxis.Horizontal ? _track.layout.width : _track.layout.height;
            float thumbSize = _axis == ElasticSliderAxis.Horizontal ? _thumb.layout.width : _thumb.layout.height;

            if (trackSize <= 1f || thumbSize <= 1f)
                return;

            _maxTravelPixels = Mathf.Max(4f, (trackSize - thumbSize) * 0.5f);
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            if (_track == null)
                return;

            _track.CapturePointer(e.pointerId);
            _activePointerId = e.pointerId;
            _dragging = true;
            _thumb?.AddToClassList(UssClassThumbDragging);
            DragStarted?.Invoke();
            ApplyPointerLocalPosition(e.localPosition);
            e.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (!_dragging || e.pointerId != _activePointerId || _track == null)
                return;

            if (!_track.HasPointerCapture(e.pointerId))
                return;

            ApplyPointerLocalPosition(e.localPosition);
            e.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            if (e.pointerId != _activePointerId)
                return;

            FinishDrag();
            e.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent e)
        {
            if (e.pointerId != _activePointerId)
                return;

            FinishDrag();
            e.StopPropagation();
        }

        private void FinishDrag()
        {
            if (_track != null && _activePointerId >= 0)
                _track.ReleasePointer(_activePointerId);

            _dragging = false;
            _activePointerId = -1;
            _thumb?.RemoveFromClassList(UssClassThumbDragging);

            SetThumbTranslatePixels(0f, 0f);
            LinearValue = 0f;
            Value = 0f;
            if (_lastSign != 0)
                DirectionChanged?.Invoke(0);
            _lastSign = 0;
            ValueChanged?.Invoke(0f);
            DragEnded?.Invoke(0f);
        }

        private void ApplyPointerLocalPosition(Vector2 localPos)
        {
            float trackW = _track.layout.width;
            float trackH = _track.layout.height;
            if (trackW <= 1f || trackH <= 1f)
                RefreshMaxTravelFromLayout();

            float cx = trackW * 0.5f;
            float cy = trackH * 0.5f;
            float raw = _axis == ElasticSliderAxis.Horizontal ? localPos.x - cx : localPos.y - cy;

            float offsetPx = Mathf.Clamp(raw, -_maxTravelPixels, _maxTravelPixels);
            float linearNorm = _maxTravelPixels > 0.01f ? Mathf.Clamp(offsetPx / _maxTravelPixels, -1f, 1f) : 0f;
            LinearValue = linearNorm;

            float ax = Mathf.Abs(linearNorm);
            float mapped = Mathf.Sign(linearNorm) * Mathf.Pow(ax, _accelerationExponent);
            mapped = Mathf.Clamp(mapped, -1f, 1f);
            Value = mapped;

            float dz = _directionDeadZone;
            int newSign = linearNorm > dz ? 1 : linearNorm < -dz ? -1 : 0;
            if (newSign != _lastSign)
            {
                DirectionChanged?.Invoke(newSign);
                _lastSign = newSign;
            }

            if (_axis == ElasticSliderAxis.Horizontal)
                SetThumbTranslatePixels(offsetPx, 0f);
            else
                SetThumbTranslatePixels(0f, offsetPx);

            ValueChanged?.Invoke(Value);
        }

        private void SetThumbTranslatePixels(float x, float y)
        {
            if (_thumb == null)
                return;

            _thumb.style.translate = new Translate(
                new Length(x, LengthUnit.Pixel),
                new Length(y, LengthUnit.Pixel));
        }

        public new class UxmlFactory : UxmlFactory<ElasticSlider, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<ElasticSliderAxis> _axis =
                new UxmlEnumAttributeDescription<ElasticSliderAxis> { name = "axis", defaultValue = ElasticSliderAxis.Horizontal };

            private readonly UxmlFloatAttributeDescription _acceleration =
                new UxmlFloatAttributeDescription { name = "acceleration-exponent", defaultValue = 1.35f };

            private readonly UxmlFloatAttributeDescription _deadZone =
                new UxmlFloatAttributeDescription { name = "direction-dead-zone", defaultValue = 0.02f };

            private readonly UxmlStringAttributeDescription _caption =
                new UxmlStringAttributeDescription { name = "caption", defaultValue = string.Empty };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var el = (ElasticSlider)ve;
                el.Axis = _axis.GetValueFromBag(bag, cc);
                el.AccelerationExponent = _acceleration.GetValueFromBag(bag, cc);
                el.DirectionDeadZone = _deadZone.GetValueFromBag(bag, cc);
                el.Caption = _caption.GetValueFromBag(bag, cc);
            }
        }
    }
}
