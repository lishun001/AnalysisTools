using UnityEditor;
using UnityEngine;

namespace Sample.Scripts.Editor
{
    public class MenuEditor
    {
        [MenuItem("Tools/Persistent")]
        public static void OpenPersistent()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}