using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente para activar eventos basados en cambios en el estado del mundo.
/// Se coloca en cualquier objeto de la escena y puede ejecutar acciones cuando se cumplen condiciones.
/// </summary>
public class WorldStateEventTrigger : MonoBehaviour
{
    [System.Serializable]
    public class StateEvent
    {
        [Tooltip("ID para identificar este evento")]
        public string eventID;

        [Header("Condición de Activación")]
        [Tooltip("Tipo de condición para activar el evento")]
        public ConditionType conditionType = ConditionType.FlagEquals;
        [Tooltip("ID del estado a verificar")]
        public string stateID;
        [Tooltip("Valor booleano para comparar (si aplica)")]
        public bool flagValue = true;
        [Tooltip("Valor entero para comparar (si aplica)")]
        public int counterValue = 0;
        [Tooltip("Operador para comparar contadores")]
        public CounterOperator counterOperator = CounterOperator.GreaterThanOrEqual;
        [Tooltip("Valor string para comparar (si aplica)")]
        public string stringValue = "";

        [Header("Acciones")]
        [Tooltip("Evento a ejecutar cuando se cumple la condición")]
        public UnityEvent onConditionMet;
        [Tooltip("Evento a ejecutar cuando deja de cumplirse la condición")]
        public UnityEvent onConditionUnmet;

        [Header("Opciones")]
        [Tooltip("Ejecutar una sola vez")]
        public bool triggerOnce = false;
        [Tooltip("Verificar condición al inicio")]
        public bool checkOnStart = true;

        // Estado del evento
        [HideInInspector] public bool hasTriggered = false;
        [HideInInspector] public bool lastConditionState = false;
    }

    // Tipos de condiciones
    public enum ConditionType
    {
        FlagEquals,
        CounterEquals,
        CounterGreaterThan,
        CounterLessThan,
        StringEquals
    }

    // Operadores para contadores
    public enum CounterOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    [Header("Configuración de Eventos")]
    [SerializeField] private StateEvent[] stateEvents;

    [Header("Activar/Desactivar Objetos")]
    [SerializeField] private bool manageObjects = false;
    [SerializeField] private ObjectModifier[] objectModifiers;

    [System.Serializable]
    public class ObjectModifier
    {
        [Tooltip("ID del flag que controla este objeto")]
        public string flagID;
        [Tooltip("Escena donde está el objeto")]
        public string sceneName;
        [Tooltip("ID del objeto a activar/desactivar")]
        public string objectID;
        [Tooltip("Invertir la lógica (activar cuando false, desactivar cuando true)")]
        public bool invertLogic = false;
    }

    private void Start()
    {
        InitializeEvents();
    }

    private void OnEnable()
    {
        // Suscribirse a eventos del WorldStateManager
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.OnFlagChanged += OnFlagChanged;
            WorldStateManager.Instance.OnCounterChanged += OnCounterChanged;
            WorldStateManager.Instance.OnStringChanged += OnStringChanged;

            RegisterObjectModifiers();
        }
    }

    private void OnDisable()
    {
        // Desuscribirse de eventos
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.OnFlagChanged -= OnFlagChanged;
            WorldStateManager.Instance.OnCounterChanged -= OnCounterChanged;
            WorldStateManager.Instance.OnStringChanged -= OnStringChanged;
        }
    }

    // Inicializar y verificar eventos al inicio
    private void InitializeEvents()
    {
        if (stateEvents == null) return;

        foreach (var stateEvent in stateEvents)
        {
            if (stateEvent.checkOnStart)
            {
                bool conditionMet = CheckCondition(stateEvent);

                if (conditionMet && !stateEvent.hasTriggered)
                {
                    stateEvent.onConditionMet?.Invoke();
                    stateEvent.lastConditionState = true;

                    if (stateEvent.triggerOnce)
                    {
                        stateEvent.hasTriggered = true;
                    }
                }
            }
        }
    }

    // Registrar modifiers de objetos
    private void RegisterObjectModifiers()
    {
        if (!manageObjects || objectModifiers == null) return;

        foreach (var modifier in objectModifiers)
        {
            // Para cada modifier, registramos un listener que actualiza el objeto
            WorldStateManager.Instance.RegisterFlagListener(modifier.flagID,
                (value) => UpdateObjectState(modifier, value));

            // Aplicar estado inicial
            bool initialValue = WorldStateManager.Instance.GetFlag(modifier.flagID);
            UpdateObjectState(modifier, initialValue);
        }
    }

    // Actualizar estado de un objeto basado en flags
    private void UpdateObjectState(ObjectModifier modifier, bool flagValue)
    {
        // Determinar si debe estar activo (considerando la inversión de lógica)
        bool shouldBeActive = modifier.invertLogic ? !flagValue : flagValue;

        // Establecer el estado del objeto
        WorldStateManager.Instance.SetObjectActive(
            modifier.sceneName,
            modifier.objectID,
            shouldBeActive);
    }

    // Callback cuando cambia un flag
    private void OnFlagChanged(string flagID, bool value)
    {
        if (stateEvents == null) return;

        foreach (var stateEvent in stateEvents)
        {
            // Si ya se activó una vez y es de un solo uso, saltar
            if (stateEvent.hasTriggered && stateEvent.triggerOnce)
                continue;

            // Si este evento depende del flag que cambió
            if (stateEvent.conditionType == ConditionType.FlagEquals &&
                stateEvent.stateID == flagID)
            {
                bool conditionMet = value == stateEvent.flagValue;

                // Si la condición cambió desde la última verificación
                if (conditionMet != stateEvent.lastConditionState)
                {
                    if (conditionMet)
                    {
                        stateEvent.onConditionMet?.Invoke();

                        if (stateEvent.triggerOnce)
                            stateEvent.hasTriggered = true;
                    }
                    else
                    {
                        stateEvent.onConditionUnmet?.Invoke();
                    }

                    stateEvent.lastConditionState = conditionMet;
                }
            }
        }
    }

    // Callback cuando cambia un contador
    private void OnCounterChanged(string counterID, int value)
    {
        if (stateEvents == null) return;

        foreach (var stateEvent in stateEvents)
        {
            // Si ya se activó una vez y es de un solo uso, saltar
            if (stateEvent.hasTriggered && stateEvent.triggerOnce)
                continue;

            // Si este evento depende del contador que cambió
            if ((stateEvent.conditionType == ConditionType.CounterEquals ||
                 stateEvent.conditionType == ConditionType.CounterGreaterThan ||
                 stateEvent.conditionType == ConditionType.CounterLessThan) &&
                stateEvent.stateID == counterID)
            {
                bool conditionMet = EvaluateCounterCondition(stateEvent, value);

                // Si la condición cambió desde la última verificación
                if (conditionMet != stateEvent.lastConditionState)
                {
                    if (conditionMet)
                    {
                        stateEvent.onConditionMet?.Invoke();

                        if (stateEvent.triggerOnce)
                            stateEvent.hasTriggered = true;
                    }
                    else
                    {
                        stateEvent.onConditionUnmet?.Invoke();
                    }

                    stateEvent.lastConditionState = conditionMet;
                }
            }
        }
    }

    // Callback cuando cambia un string
    private void OnStringChanged(string stringID, string value)
    {
        if (stateEvents == null) return;

        foreach (var stateEvent in stateEvents)
        {
            // Si ya se activó una vez y es de un solo uso, saltar
            if (stateEvent.hasTriggered && stateEvent.triggerOnce)
                continue;

            // Si este evento depende del string que cambió
            if (stateEvent.conditionType == ConditionType.StringEquals &&
                stateEvent.stateID == stringID)
            {
                bool conditionMet = value == stateEvent.stringValue;

                // Si la condición cambió desde la última verificación
                if (conditionMet != stateEvent.lastConditionState)
                {
                    if (conditionMet)
                    {
                        stateEvent.onConditionMet?.Invoke();

                        if (stateEvent.triggerOnce)
                            stateEvent.hasTriggered = true;
                    }
                    else
                    {
                        stateEvent.onConditionUnmet?.Invoke();
                    }

                    stateEvent.lastConditionState = conditionMet;
                }
            }
        }
    }

    // Verificar una condición
    private bool CheckCondition(StateEvent stateEvent)
    {
        if (WorldStateManager.Instance == null)
            return false;

        switch (stateEvent.conditionType)
        {
            case ConditionType.FlagEquals:
                return WorldStateManager.Instance.GetFlag(stateEvent.stateID) == stateEvent.flagValue;

            case ConditionType.CounterEquals:
            case ConditionType.CounterGreaterThan:
            case ConditionType.CounterLessThan:
                int counterValue = WorldStateManager.Instance.GetCounter(stateEvent.stateID);
                return EvaluateCounterCondition(stateEvent, counterValue);

            case ConditionType.StringEquals:
                return WorldStateManager.Instance.GetString(stateEvent.stateID) == stateEvent.stringValue;

            default:
                return false;
        }
    }

    // Evaluar condiciones de contadores
    private bool EvaluateCounterCondition(StateEvent stateEvent, int currentValue)
    {
        switch (stateEvent.counterOperator)
        {
            case CounterOperator.Equal:
                return currentValue == stateEvent.counterValue;

            case CounterOperator.NotEqual:
                return currentValue != stateEvent.counterValue;

            case CounterOperator.GreaterThan:
                return currentValue > stateEvent.counterValue;

            case CounterOperator.LessThan:
                return currentValue < stateEvent.counterValue;

            case CounterOperator.GreaterThanOrEqual:
                return currentValue >= stateEvent.counterValue;

            case CounterOperator.LessThanOrEqual:
                return currentValue <= stateEvent.counterValue;

            default:
                return false;
        }
    }

    // Método público para verificar manualmente las condiciones
    public void CheckAllConditions()
    {
        if (stateEvents == null) return;

        foreach (var stateEvent in stateEvents)
        {
            // Si ya se activó una vez y es de un solo uso, saltar
            if (stateEvent.hasTriggered && stateEvent.triggerOnce)
                continue;

            bool conditionMet = CheckCondition(stateEvent);

            // Si la condición cambió desde la última verificación
            if (conditionMet != stateEvent.lastConditionState)
            {
                if (conditionMet)
                {
                    stateEvent.onConditionMet?.Invoke();

                    if (stateEvent.triggerOnce)
                        stateEvent.hasTriggered = true;
                }
                else
                {
                    stateEvent.onConditionUnmet?.Invoke();
                }

                stateEvent.lastConditionState = conditionMet;
            }
        }
    }
}