using UnityEngine;
using System.Collections;

public class NavigationFixer : MonoBehaviour
{
    private UINavigationManager[] navigationManager;
    private float checkInterval = 20f;

    [SerializeField] private KeyCode Fix_Navigation_Key;

    void Start()
    {
        StartCoroutine(CheckNavigationManagersRoutine());
    }

    void Update()
    {
        if (Input.GetKeyDown(Fix_Navigation_Key))
        {
            ActivateAllNavigationManagers();
        }
    }

    private IEnumerator CheckNavigationManagersRoutine()
    {
        while (true)
        {
            CheckNavigationManagers();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void CheckNavigationManagers()
    {
        navigationManager = Object.FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);

        int totalCount = navigationManager.Length;
        int activeCount = 0;
        int inactiveCount = 0;

        foreach (UINavigationManager manager in navigationManager)
        {
            if (manager.gameObject.activeInHierarchy)
                activeCount++;
            else
                inactiveCount++;
        }

        Debug.Log($"[NavigationFixer] Total UINavigationManager encontrados: {totalCount}");
        Debug.Log($"[NavigationFixer] Activos: {activeCount} | Inactivos: {inactiveCount}");

        ProcessNavigationManagers();
    }

    private void ProcessNavigationManagers()
    {
        if (navigationManager.Length == 0)
        {
            Debug.LogWarning("[NavigationFixer] No se encontraron UINavigationManager en las escenas.");
        }
    }

    private void ActivateAllNavigationManagers()
    {
        navigationManager = Object.FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);

        int activatedCount = 0;
        foreach (UINavigationManager manager in navigationManager)
        {
            if (!manager.gameObject.activeInHierarchy)
            {
                manager.gameObject.SetActive(true);
                activatedCount++;
            }
        }

        Debug.Log($"[NavigationFixer] Se activaron {activatedCount} UINavigationManagers.");
    }

    public void SetCheckInterval(float newInterval)
    {
        checkInterval = newInterval;
        Debug.Log($"[NavigationFixer] Intervalo de verificación cambiado a {newInterval} segundos.");
    }

    public void ForceCheck()
    {
        CheckNavigationManagers();
    }
}