using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class NPCEventBridge : MonoBehaviour
{
    private static NPCEventBridge instance;
    public static NPCEventBridge Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("NPCEventBridge");
                instance = go.AddComponent<NPCEventBridge>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Eventos globales para conversaciones
    public UnityEvent<NPC> OnAnyNPCConversationEnded = new UnityEvent<NPC>();

    // Registro de callbacks específicos por NPC ID
    private Dictionary<int, List<System.Action>> npcCallbacks = new Dictionary<int, List<System.Action>>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Registrar un callback para un NPC específico
    public void RegisterNPCCallback(int npcId, System.Action callback)
    {
        if (!npcCallbacks.ContainsKey(npcId))
        {
            npcCallbacks[npcId] = new List<System.Action>();
        }

        npcCallbacks[npcId].Add(callback);
        Debug.Log($"[NPCEventBridge] Callback registrado para NPC ID: {npcId}");
    }

    // Notificar que una conversación terminó
    public void NotifyConversationEnded(NPC npc)
    {
        Debug.Log($"[NPCEventBridge] Notificando fin de conversación para NPC: {npc.name} (ID: {npc.npcId})");

        // Invocar evento global
        OnAnyNPCConversationEnded?.Invoke(npc);

        // Invocar callbacks específicos
        if (npcCallbacks.ContainsKey(npc.npcId))
        {
            foreach (var callback in npcCallbacks[npc.npcId])
            {
                callback?.Invoke();
            }
        }
    }

    // Limpiar callbacks de un NPC
    public void UnregisterNPCCallbacks(int npcId)
    {
        if (npcCallbacks.ContainsKey(npcId))
        {
            npcCallbacks.Remove(npcId);
        }
    }
}