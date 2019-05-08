using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneViewFilter : MonoBehaviour
{
#if UNITY_EDITOR
    bool hasChanged = false;

    public virtual void OnValidate()
    {
        hasChanged = true;
    }

    static SceneViewFilter()
    {
        SceneView.onSceneGUIDelegate += CheckMe;
    }

    static void CheckMe(SceneView sv)
    {
        if (Event.current.type != EventType.Layout || !Camera.main)
            return;
        
        var cameraFilters = Camera.main.GetComponents<SceneViewFilter>();
        var sceneFilters = sv.camera.GetComponents<SceneViewFilter>();

        if (cameraFilters.Length != sceneFilters.Length)
        {
            Recreate(sv);
            return;
        }
        if (cameraFilters.Where((t, i) => t.GetType() != sceneFilters[i].GetType()).Any())
        {
            Recreate(sv);
            return;
        }

        for (var i = 0; i < cameraFilters.Length; i++)
            if (cameraFilters[i].hasChanged || sceneFilters[i].enabled != cameraFilters[i].enabled)
            {
                EditorUtility.CopySerialized(cameraFilters[i], sceneFilters[i]);
                cameraFilters[i].hasChanged = false;
            }
    }

    private static void Recreate(SceneView sv)
    {
        SceneViewFilter filter;
        filter = sv.camera.GetComponent<SceneViewFilter>();
        DestroyImmediate(filter);

        foreach (var f in Camera.main.GetComponents<SceneViewFilter>())
        {
            var newFilter = sv.camera.gameObject.AddComponent(f.GetType()) as SceneViewFilter;
            EditorUtility.CopySerialized(f, newFilter);
        }
    }
#endif
}