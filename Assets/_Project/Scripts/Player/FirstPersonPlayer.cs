using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticCircus
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        public Camera View;
        public float Speed = 4f;
        public float SprintMultiplier = 1.55f;
        public float Reach = 4f;
        public Interactable Focus { get; private set; }
        // Set by cutscenes. Stops control without tearing down the component.
        public bool Frozen { get; set; }
        // Parent carried objects here to hold them in view.
        public Transform Hand { get; private set; }
        float pitch;
        float roll;
        float vertical;
        float stepDistance;
        Vector3 spawn;
        Quaternion spawnRotation;
        CharacterController controller;
        Interactable highlighted;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            View.cullingMask &= ~(1 << CircusLayers.MirrorOnly);
            Hand = new GameObject("Hand").transform;
            Hand.SetParent(View.transform, false);
            Hand.localPosition = new Vector3(0.42f, -0.34f, 0.8f);
        }

        void Start() => LockCursor(true);

        void Update()
        {
            ApplyView();
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (Frozen || InputGate.Blocked || ScreenFader.Busy) { SetFocus(null); return; }
            if (keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
            if (Cursor.lockState != CursorLockMode.Locked) { SetFocus(null); return; }
            Vector2 look = mouse.delta.ReadValue() * (GameSettings.Sensitivity * 0.08f);
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -82, 82);
            ApplyView();
            Move(keyboard);
            UpdateFocus();
            if (Focus != null && keyboard.eKey.wasPressedThisFrame) Focus.Use();
        }

        void ApplyView() => View.transform.localRotation = Quaternion.Euler(pitch, 0, roll);

        void Move(Keyboard keyboard)
        {
            if (controller.isGrounded && vertical < 0) vertical = -2;
            vertical += Physics.gravity.y * Time.deltaTime;
            float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float forward = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            var direction = Vector3.ClampMagnitude(transform.right * horizontal + transform.forward * forward, 1);
            float speed = Speed * (keyboard.leftShiftKey.isPressed ? SprintMultiplier : 1);
            controller.Move((direction * speed + Vector3.up * vertical) * Time.deltaTime);
            stepDistance += direction.magnitude * speed * Time.deltaTime;
            if (stepDistance > 2f && controller.isGrounded)
            {
                stepDistance = 0;
                Sfx.Play(SfxKind.Step, 0.35f);
            }
        }

        void UpdateFocus()
        {
            Interactable found = null;
            if (Physics.Raycast(View.transform.position, View.transform.forward, out var hit, Reach))
                found = hit.collider.GetComponentInParent<Interactable>();
            SetFocus(found);
        }

        void SetFocus(Interactable next)
        {
            Focus = next;
            if (highlighted == next) return;
            if (highlighted != null) highlighted.SetFocused(false);
            highlighted = next;
            if (highlighted != null) highlighted.SetFocused(true);
        }

        public void Warp(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            vertical = 0;
        }

        public void WarpToSpawn() => Warp(spawn, spawnRotation);

        // Rolls the view through a full turn, moving the player back to the spawn point at the halfway mark.
        public void Tumble(float seconds = 1.5f) => StartCoroutine(TumbleRoutine(seconds));

        IEnumerator TumbleRoutine(float seconds)
        {
            Frozen = true;
            bool warped = false;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                roll = Mathf.SmoothStep(0, 360, k);
                if (k > 0.5f && !warped) { warped = true; WarpToSpawn(); }
                yield return null;
            }
            roll = 0;
            pitch = 0;
            Frozen = false;
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnDisable() => LockCursor(false);
    }
}
