using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemManager : MonoBehaviour
{
    private static EventSystemManager instance;
    private EventSystem eventSystem;

    public static EventSystem GetEventSystem()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("EventSystemManager");
            instance = go.AddComponent<EventSystemManager>();
            DontDestroyOnLoad(go);
        }
        return instance.eventSystem;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = gameObject.AddComponent<EventSystem>();
            gameObject.AddComponent<StandaloneInputModule>();
        }
    }
}