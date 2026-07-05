namespace SmilingEclipse.STMImporter
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.Device;

    public class SkillNodeLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;
        private SkillNode nodeA;
        private SkillNode nodeB;
        private Canvas canvas;
        public void Initialize(SkillNode nodeA, SkillNode nodeB)
        {
            this.nodeA = nodeA;
            this.nodeB = nodeB;
            canvas = transform.parent.parent.parent.GetComponent<Canvas>();
            UpdateLine();
        }
        private void Update()
        {
            UpdateLine();
        }

        public void UpdateLine()
        {
            if (nodeA == null || nodeB == null) return;

            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    float zDistance = Mathf.Abs(Camera.main.transform.position.z);
                    Vector3 worldA = Camera.main.ScreenToWorldPoint(new Vector3(nodeA.transform.position.x, nodeA.transform.position.y, zDistance));
                    Vector3 worldB = Camera.main.ScreenToWorldPoint(new Vector3(nodeB.transform.position.x, nodeB.transform.position.y, zDistance));
                    line.SetPosition(0, worldA); //space overlay
                    line.SetPosition(1, worldB); //space overlay
                    break;
                case RenderMode.ScreenSpaceCamera:
                    line.SetPosition(0, nodeA.transform.position); //space camera
                    line.SetPosition(1, nodeB.transform.position); //space camera
                    break;
                case RenderMode.WorldSpace:
                    break;
                default:
                    break;
            }






        }
        public void SetColor(Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }
    }
}