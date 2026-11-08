using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// Background layer that follows the camera by a fraction: 1 = fixed to the sky (sun),
    /// 0 = part of the world. Runs after the camera has moved.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float followX = 0.5f;
        [Range(0f, 1f)] public float followY = 0.4f;

        Transform cam;
        Vector3 start, camStart;

        void Start()
        {
            if (Camera.main == null)
                return;
            cam = Camera.main.transform;
            start = transform.position;
            camStart = cam.position;
        }

        void LateUpdate()
        {
            if (cam == null)
                return;
            var d = cam.position - camStart;
            transform.position = start + new Vector3(d.x * followX, d.y * followY, 0f);
        }
    }
}
