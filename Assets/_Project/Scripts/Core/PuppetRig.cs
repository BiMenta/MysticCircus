using UnityEngine;

namespace MysticCircus
{
    // Pose driver for jointed characters. The model's front is local -Z.
    public sealed class PuppetRig : MonoBehaviour
    {
        public Transform Torso, ArmLeft, ArmRight;
        public float Speed = 7f;

        // Lean: positive tips the head toward local -X. Bow: positive folds toward the model's front.
        // Arm angles: 0 hangs down, 90 points straight ahead, 180 points up.
        public float Lean, Bow, ArmLeftAngle, ArmRightAngle;
        // Procedural wobble added on top of the pose, for marionettes and idling.
        public float SwayAmount;
        float lean, bow, armLeft, armRight;

        public void Pose(float leanAngle = 0, float bowAngle = 0, float armLeftAngle = 0, float armRightAngle = 0)
        {
            Lean = leanAngle;
            Bow = bowAngle;
            ArmLeftAngle = armLeftAngle;
            ArmRightAngle = armRightAngle;
        }

        void Update()
        {
            float k = 1 - Mathf.Exp(-Speed * Time.deltaTime);
            lean = Mathf.Lerp(lean, Lean, k);
            bow = Mathf.Lerp(bow, Bow, k);
            armLeft = Mathf.Lerp(armLeft, ArmLeftAngle, k);
            armRight = Mathf.Lerp(armRight, ArmRightAngle, k);
            float sway = SwayAmount == 0 ? 0 : Mathf.Sin(Time.time * 2.1f) * SwayAmount;
            // Euler +X tips the head toward +Z, which is behind this model, so bowing uses -X.
            if (Torso != null) Torso.localRotation = Quaternion.Euler(-bow, 0, lean + sway);
            if (ArmLeft != null) ArmLeft.localRotation = Quaternion.Euler(armLeft + sway * 1.5f, 0, 0);
            if (ArmRight != null) ArmRight.localRotation = Quaternion.Euler(armRight - sway * 1.5f, 0, 0);
        }
    }
}
