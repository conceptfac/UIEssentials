using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Concept.UI
{

    [UxmlElement]
    public partial class CustomSpinner : VisualElement
    {

        public enum SpinnerValueType
        {
            Integer,
            Double,
            Currency
        }

        private const string USSClassName = "spinner-field";

        //private VisualElement this;

        private TextField m_value;
        private Button m_decrease;
        private Button m_increase;


        public SpinnerValueType m_valueType { get; set; }

        [UxmlAttribute("value-type")]
        public SpinnerValueType ValueType
        {
            get => m_valueType; set
            {
                m_valueType = value;
                Reset(); // Reset value when type changes
            }
        }



        [UxmlAttribute("culture-info")]
        public string cultureInfo = "pt-BR";
        public CultureInfo culture;


        [UxmlAttribute("min-value")]
        [Tooltip("Integer value with decimals\nEx: 100 = 1.00")]
        [SerializeField] public int minValue { get; set; } = 0;

        [UxmlAttribute("max-value")]
        [Tooltip("Integer value with decimals\nEx: 100 = 1.00\n0 is infinite.")]
        [SerializeField] public int maxValue { get; set; } = 0;

        public int Value
        {
            get {


                string numeric = Regex.Replace(m_value.text, @"[^0-9]", "").TrimStart('0');
                int value = 0;
                if (double.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    value = (int)(result);
                return value;
            }

            set
            {
                string formatted = ValueType switch
                {
                    SpinnerValueType.Integer => value.ToString(),
                    SpinnerValueType.Double => (value / 100d).ToString("N2", CultureInfo.InvariantCulture),
                    SpinnerValueType.Currency => (value / 100m).ToString("N2", culture),
                    _ => throw new ArgumentOutOfRangeException(nameof(ValueType), $"[CustomSpinner] Unexpected value type: {ValueType}")
                };
                m_value.SetValueWithoutNotify(formatted);

            }
        }

        [UxmlAttribute("value")]
        public string ValueText
        {
            get => m_value.value;
            set
            {
                m_value.value = value;
            }
        }

        [UxmlAttribute("decimal-increase")]
        [Tooltip("True: Incease by decimal part(+0.1). False: Increase by integer part(+1).")]
        public bool decimalIncrease { get; set; } = true;

        public event Action<int> OnChangeValue;


        private IVisualElementScheduledItem _repeatSchedule;
        private int _repeatDirection;



        public CustomSpinner()
        {

            culture = new CultureInfo(cultureInfo);

            //AddToClassList(USSClassName);

            var visualTree = Resources.Load<VisualTreeAsset>("Widgets/CustomSpinner/CustomSpinnerField");
            if (visualTree == null)
            {
                Debug.LogError("CustomSpinnerField não encontrado em Resources!");
                return;
            }

            visualTree.CloneTree(this);
            //hierarchy.Add(this);

            this.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                SetupNumericTextField(m_value, OnChangeValue, culture, minValue, maxValue, m_valueType);
            });




            m_value = this.Q<TextField>("InputValue");

            m_value.RegisterCallback<FocusInEvent>(evt =>
            {
                m_value.SelectAll();
            });


            m_value.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (m_value.focusController.focusedElement != m_value)
                {
                    m_value.SelectAll();
                }
            });

            m_decrease = this.Q<Button>("DecreaseButton");
            m_increase = this.Q<Button>("IncreaseButton");


            m_increase.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation();
                StartRepeat(1, m_increase);
            }, TrickleDown.TrickleDown);

            m_increase.RegisterCallback<PointerUpEvent>(evt =>
            {
                evt.StopPropagation();
                StopRepeat(m_increase);
            }, TrickleDown.TrickleDown);

            m_increase.RegisterCallback<PointerMoveEvent>(evt =>
            {
                var rect = m_increase.resolvedStyle;
                bool isInside = evt.localPosition.x >= 0 && evt.localPosition.x <= rect.width &&
                                evt.localPosition.y >= 0 && evt.localPosition.y <= rect.height;

                if (!isInside)
                {
                    StopRepeat(m_increase);
                }
            }, TrickleDown.TrickleDown);


            m_increase.RegisterCallback<PointerCancelEvent>(evt =>
            {
                StopRepeat(m_increase);
            }, TrickleDown.TrickleDown);

            m_decrease.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation();
                StartRepeat(-1, m_decrease);
            }, TrickleDown.TrickleDown);

            m_decrease.RegisterCallback<PointerUpEvent>(evt =>
            {
                evt.StopPropagation();
                StopRepeat(m_decrease);
            }, TrickleDown.TrickleDown);

            m_decrease.RegisterCallback<PointerMoveEvent>(evt =>
            {
                var rect = m_decrease.resolvedStyle;
                bool isInside = evt.localPosition.x >= 0 && evt.localPosition.x <= rect.width &&
                                evt.localPosition.y >= 0 && evt.localPosition.y <= rect.height;

                if (!isInside)
                {
                    StopRepeat(m_decrease);
                }
            }, TrickleDown.TrickleDown);


            m_decrease.RegisterCallback<PointerCancelEvent>(evt =>
            {
                StopRepeat(m_decrease);
            }, TrickleDown.TrickleDown);




            styleSheets.Add(Resources.Load<StyleSheet>("Widgets/CustomSpinner/"+GetType().Name + "Styles"));
        }

        private void StartRepeat(int direction, VisualElement button)
        {
            _repeatDirection = direction;
            ChangeValue(_repeatDirection);

            button.CapturePointer(0); // captura o ponteiro para detectar release fora do botão

            _repeatSchedule?.Pause();
            _repeatSchedule = this.schedule.Execute(() => ChangeValue(_repeatDirection))
                                         .Every(50)
                                         .StartingIn(500);
        }
        private void StopRepeat(VisualElement button)
        {
            _repeatSchedule?.Pause();
            _repeatSchedule = null;

            if (button.HasPointerCapture(0))
                button.ReleasePointer(0);
        }


        public void SetupNumericTextField(TextField textField, Action<int> onValidInput, CultureInfo culture = null, double valueMin = 0, double valueMax = -1, SpinnerValueType valueType = SpinnerValueType.Integer)
        {
            culture ??= CultureInfo.InvariantCulture;

            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                string numeric = Regex.Replace(textField.text, @"[^0-9]", "").TrimStart('0');
                int value = 0;
                if (double.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    value = (int)(result);

                if (value < minValue) value = minValue;
                else
                if (maxValue > 0 &&  value > maxValue) value = maxValue;



                    string formatted = valueType switch
                    {
                        SpinnerValueType.Integer => value.ToString(),
                        SpinnerValueType.Double => (value / 100d).ToString("N2", CultureInfo.InvariantCulture),
                        SpinnerValueType.Currency => (value / 100m).ToString("N2", culture),
                        _ => throw new ArgumentOutOfRangeException(nameof(valueType), $"[CustomSpinner] Unexpected value type: {valueType}")
                    };

                textField.SetValueWithoutNotify(formatted);

            });

            textField.RegisterValueChangedCallback(evt =>
            {
               onValidInput?.Invoke(Value);
            });
            /*
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {

                char c = evt.character;

                if (!char.IsNumber(c))
                {
                    evt.StopPropagation();
                    return;
                }

            }, TrickleDown.TrickleDown);


            textField.RegisterValueChangedCallback(evt =>
            {
                string numeric = Regex.Replace(evt.newValue, @"[^0-9]", "").TrimStart('0');
                int value = 0;
                    if (double.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                        value = (int)(result);
                    
                    string formatted = valueType switch
                    {
                        SpinnerValueType.Integer => value.ToString(),
                        SpinnerValueType.Double => (value / 100f).ToString("N2", CultureInfo.InvariantCulture),
                        SpinnerValueType.Currency => (value / 100m).ToString("N2", culture),
                        _ => throw new ArgumentOutOfRangeException(nameof(valueType), $"[CustomSpinner] Unexpected value type: {valueType}")
                    };

                Debug.LogWarning($"[CustomSpinner] Parsed value: {formatted} from input '{numeric}'");
                    textField.SetValueWithoutNotify(formatted);
                
               textField.textSelection.cursorIndex = 1000;

            });

            */
        }

        public static void SetupNumericTextFieldOLD(TextField textField, Action<double> onValidInput, CultureInfo culture = null, double valueMin = 0, double valueMax = -1, SpinnerValueType valueType = SpinnerValueType.Integer)
        {
            culture ??= CultureInfo.InvariantCulture;

            textField.RegisterValueChangedCallback(evt =>
            {
                string input = evt.newValue;

                // Sanitiza: só números e um único separador decimal
                int commaCount = 0;
                string clean = "";
                foreach (char c in input)
                {

                    if (char.IsDigit(c))
                    {
                        clean += c;
                    }
                    else if ((c == '.' || c == ',') && commaCount == 0)
                    {
                        clean += culture.NumberFormat.NumberDecimalSeparator;
                        commaCount++;
                    }
                }

                if (double.TryParse(clean, NumberStyles.Any, culture, out double parsed))
                {
                    if (parsed < valueMin) parsed = valueMin;


                    string formatted = valueType switch
                    {
                        SpinnerValueType.Integer => ((int)parsed).ToString(),
                        SpinnerValueType.Double => parsed.ToString("0.00", CultureInfo.InvariantCulture),
                        SpinnerValueType.Currency => parsed.ToString("C2", CultureInfo.CurrentCulture),
                        _ => throw new ArgumentOutOfRangeException(nameof(valueType), $"[CustomSpinner] Unexpected value type: {valueType}")
                    };



                    textField.SetValueWithoutNotify(formatted);
                    onValidInput?.Invoke(parsed);
                }
                else
                {
                    onValidInput?.Invoke(0);
                }
            });
        }





        public void Reset(int value = 0)
        {
            switch (ValueType)
            {
                case SpinnerValueType.Integer:
                    m_value.value = value == 0
                        ? ((int)minValue).ToString()
                        : value.ToString();
                    break;

                case SpinnerValueType.Double:
                    m_value.value = value == 0
                        ? (minValue / 100d).ToString("N2", CultureInfo.InvariantCulture)
                        : (value / 100d).ToString("N2", CultureInfo.InvariantCulture);
                    break;

                case SpinnerValueType.Currency:
                    m_value.value = value == 0
                        ? (minValue / 100m).ToString("N2", culture)
                        : (value / 100m).ToString("N2", culture);
                    break;
            }


        }

        private void ChangeValue(int direction)
        {
            int current = Value;

            switch (ValueType)
            {
                case SpinnerValueType.Integer:
                    {
                        current += direction;
                        if (current < minValue) current = (int)minValue;
                        else
                        if (maxValue > 0 && current > maxValue) current = (int)maxValue;

                        m_value.value = current.ToString();
                        break;
                    }

                case SpinnerValueType.Double:
                case SpinnerValueType.Currency:
                    {
                        if (decimalIncrease)
                            current += direction;
                        else
                            current += direction * 100;

                        if (current < minValue) current = minValue;
                        if (current > maxValue) current = maxValue;

                        m_value.value = (current / 100m).ToString("N2", (ValueType == SpinnerValueType.Currency) ? culture : CultureInfo.InvariantCulture);
                        break;
                    }

            }
        }

    }

}