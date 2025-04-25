using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PointFloatersBuoyantObject))]
public class PointFloatersBuoyantObjectEditor : Editor
{
    void OnSceneGUI()
    {
        var comp = (PointFloatersBuoyantObject)target;
        var tf = comp.transform;
        var points = comp.localFloatPoints;

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < points.Length; i++)
        {
            // calculate world position
            var worldPos = tf.TransformPoint(points[i]);
            // draw a position handle
            var newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(comp, "Move Float Point");
                // convert back to local space
                points[i] = tf.InverseTransformPoint(newWorldPos);
                comp.localFloatPoints = points;
                EditorUtility.SetDirty(comp);
            }
        }
    }
}
