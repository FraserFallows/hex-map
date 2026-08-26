using UnityEditor;
using UnityEngine;

namespace _Scripts.Core
{
    [CustomEditor(typeof(SunController))]
    public class SunControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var sunController = (SunController)target;

            if (GUILayout.Button("Randomise Sun"))
            {
                sunController.PositionSun(sunController.RandomTimeOfYear());
            }
        }
    }
}
