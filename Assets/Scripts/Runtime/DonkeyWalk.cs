using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Passo dell'asino: le zampe si alternano in diagonale e il cavaliere ondeggia,
    /// in base alla velocità del carretto (più è lento, più il passo è piccolo).
    /// Usa i pivot "Zampe_FL/FR/BL/BR" e "Cavaliere" creati dal generatore.
    /// </summary>
    public class DonkeyWalk : MonoBehaviour
    {
        private const float StridePerMeter = 3.2f;
        private const float MaxSwing = 28f;
        private const float FullSwingSpeed = 2.5f;

        private TrafficCar owner;
        private Transform legFL, legFR, legBL, legBR, seat;
        private Vector3 seatRest;
        private float phase;
        private bool found;

        private void Update()
        {
            if (!found) Find();
            if (owner == null) return;

            float speed = owner.CurrentSpeed;
            phase += speed * StridePerMeter * Time.deltaTime;
            float swing = Mathf.Clamp01(speed / FullSwingSpeed) * MaxSwing;
            float s = Mathf.Sin(phase) * swing;

            // Diagonali opposte: anteriore sinistra con posteriore destra, e viceversa.
            Swing(legFL, s);
            Swing(legBR, s);
            Swing(legFR, -s);
            Swing(legBL, -s);

            if (seat != null)
                seat.localPosition = seatRest + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * 0.04f * Mathf.Clamp01(speed / FullSwingSpeed);
        }

        private void Find()
        {
            owner = GetComponentInParent<TrafficCar>();
            if (owner == null) return;
            legFL = transform.Find("Zampe_FL");
            legFR = transform.Find("Zampe_FR");
            legBL = transform.Find("Zampe_BL");
            legBR = transform.Find("Zampe_BR");
            seat = transform.Find("Cavaliere");
            if (seat != null) seatRest = seat.localPosition;
            found = true;
        }

        private static void Swing(Transform hip, float angle)
        {
            if (hip != null) hip.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
