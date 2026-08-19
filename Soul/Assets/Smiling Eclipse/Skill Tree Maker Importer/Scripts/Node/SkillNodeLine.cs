namespace SmilingEclipse.STMImporter
{
    
    using UnityEngine;
    

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
            if (nodeA == null || nodeB == null || line == null) return;

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }
            if (canvas == null) return;

            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    Camera mainCam = Camera.main;
                    if (mainCam == null) mainCam = Camera.current;
                    if (mainCam == null) return;

                    float zDistance = Mathf.Abs(mainCam.transform.position.z);
                    Vector3 worldA = mainCam.ScreenToWorldPoint(new Vector3(nodeA.transform.position.x, nodeA.transform.position.y, zDistance));
                    Vector3 worldB = mainCam.ScreenToWorldPoint(new Vector3(nodeB.transform.position.x, nodeB.transform.position.y, zDistance));
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