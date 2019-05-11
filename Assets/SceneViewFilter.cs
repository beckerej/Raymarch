using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace Assets
{
    public class SceneViewFilter : MonoBehaviour
    {
#if UNITY_EDITOR
        private bool _hasChanged;

        public virtual void OnValidate()
        {
            _hasChanged = true;
        }

        static SceneViewFilter()
        {
            SceneView.onSceneGUIDelegate += Check;
        }

        private static void Check(SceneView sv)
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
            {
                if (!cameraFilters[i]._hasChanged && sceneFilters[i].enabled == cameraFilters[i].enabled)
                    continue;
                EditorUtility.CopySerialized(cameraFilters[i], sceneFilters[i]);
                cameraFilters[i]._hasChanged = false;
            }
        }

        private static void Recreate(SceneView sv)
        {
            var filter = sv.camera.GetComponent<SceneViewFilter>();
            DestroyImmediate(filter);

            foreach (var f in Camera.main.GetComponents<SceneViewFilter>())
            {
                var newFilter = sv.camera.gameObject.AddComponent(f.GetType()) as SceneViewFilter;
                EditorUtility.CopySerialized(f, newFilter);
            }
        }
#endif
    }
}