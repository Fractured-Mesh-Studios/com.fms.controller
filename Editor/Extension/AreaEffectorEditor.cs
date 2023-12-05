using Codice.CM.WorkspaceServer;
using GameEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(AreaEffector))]
    public class AreaEffectorEditor : BaseEditor
    {
        private AreaEffector m_target;
        private Collider m_collider;

        private Collider[] m_colliders;

        private void OnEnable()
        {
            m_target = (AreaEffector)target;
            m_title = "Area Effector";

            m_collider = m_target.GetComponent<Collider>();
            m_colliders = m_target.GetComponents<Collider>();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!m_collider)
            {
                EditorGUILayout.HelpBox("No Collider Detected For Area Effector", MessageType.Warning);
            }

            if (m_collider && !m_collider.isTrigger)
            {
                EditorGUILayout.HelpBox("Collider Must Be a Trigger", MessageType.Warning);
            }
        }

        private void OnSceneGUI()
        {
            Vector3 local = Vector3.zero;
            Vector3 world = Vector3.zero;

            foreach(var collider in m_colliders)
            {
                var box = collider as BoxCollider;
                if (box)
                {
                    local = m_target.transform.TransformPoint(box.center);
                    world = Handles.PositionHandle(local, box.transform.rotation);
                    local = m_target.transform.InverseTransformPoint(world);
                    box.center = local;
                }

                var sphere = collider as SphereCollider;
                if(sphere)
                {
                    local = m_target.transform.TransformPoint(sphere.center);
                    world = Handles.PositionHandle(local, sphere.transform.rotation);
                    local = m_target.transform.InverseTransformPoint(world);
                    sphere.center = local;
                }
            }
        }
    }
}
