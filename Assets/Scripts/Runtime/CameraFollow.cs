using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Camera in terza persona dall'alto. Non è attaccata al tuk-tuk: segue una posizione smussata
    /// che lo insegue con un ritardo morbido. Il ritardo viene compensato in avanti, così il tuk-tuk
    /// resta fermo nell'inquadratura anche a velocità alta. Il cambio corsia e le oscillazioni
    /// del tuk-tuk arrivano alla camera ammorbiditi, senza scatti.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 4.2f, -6.5f);
        [Tooltip("Ritardo laterale: più alto = più morbido")]
        [SerializeField] private float followTimeX = 0.22f;
        [Tooltip("Ritardo in altezza")]
        [SerializeField] private float followTimeY = 0.3f;
        [Tooltip("Ritardo in avanti (compensato, il tuk-tuk resta nella stessa posizione sullo schermo)")]
        [SerializeField] private float followTimeZ = 0.12f;
        [SerializeField] private float lookAhead = 4f;

        private Vector3 smoothed;
        private Vector3 velocity;
        private Vector3 lastTargetPos;
        private bool started;

        private void LateUpdate()
        {
            if (target == null) return;

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 tp = target.position;
            if (!started)
            {
                smoothed = tp + offset;
                lastTargetPos = tp;
                started = true;
            }

            // Velocità in avanti del tuk-tuk: la camera anticipa di quanto è in ritardo.
            float forwardSpeed = (tp.z - lastTargetPos.z) / dt;
            lastTargetPos = tp;

            Vector3 desired = tp + offset;
            desired.z += forwardSpeed * followTimeZ;

            smoothed.x = Mathf.SmoothDamp(smoothed.x, desired.x, ref velocity.x, followTimeX);
            smoothed.y = Mathf.SmoothDamp(smoothed.y, desired.y, ref velocity.y, followTimeY);
            smoothed.z = Mathf.SmoothDamp(smoothed.z, desired.z, ref velocity.z, followTimeZ);
            transform.position = smoothed;

            Vector3 lookPoint = tp + Vector3.forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position);
        }
    }
}
