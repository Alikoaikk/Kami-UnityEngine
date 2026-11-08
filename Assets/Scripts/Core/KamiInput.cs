using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kami.Gameplay
{
    /// <summary>
    /// Kami's input actions (GDD controls): move A/D or arrows, jump W/Up/Space, fold 1-4,
    /// ability F or left click, swap belt Tab, restart R, pause Esc. Gamepad bindings included.
    /// </summary>
    public sealed class KamiInput : IDisposable
    {
        public readonly InputAction Move;
        public readonly InputAction Jump;
        public readonly InputAction Ability;
        public readonly InputAction SwapBelt;
        public readonly InputAction Restart;
        public readonly InputAction Pause;
        public readonly InputAction Confirm;
        public readonly InputAction[] FoldSlots = new InputAction[4];

        readonly InputActionMap map = new("Kami");

        public KamiInput()
        {
            Move = map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");
            Move.AddBinding("<Gamepad>/dpad");

            Jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Jump.AddBinding("<Keyboard>/w");
            Jump.AddBinding("<Keyboard>/upArrow");
            Jump.AddBinding("<Gamepad>/buttonSouth");

            string[] padFolds = { "<Gamepad>/leftShoulder", "<Gamepad>/buttonNorth", "<Gamepad>/buttonEast", "<Gamepad>/rightShoulder" };
            for (int i = 0; i < FoldSlots.Length; i++)
            {
                FoldSlots[i] = map.AddAction($"FoldSlot{i + 1}", InputActionType.Button, $"<Keyboard>/{i + 1}");
                FoldSlots[i].AddBinding($"<Keyboard>/numpad{i + 1}");
                FoldSlots[i].AddBinding(padFolds[i]);
            }

            Ability = map.AddAction("Ability", InputActionType.Button, "<Keyboard>/f");
            Ability.AddBinding("<Mouse>/leftButton");
            Ability.AddBinding("<Gamepad>/buttonWest");

            SwapBelt = map.AddAction("SwapBelt", InputActionType.Button, "<Keyboard>/tab");
            SwapBelt.AddBinding("<Gamepad>/select");
            Restart = map.AddAction("Restart", InputActionType.Button, "<Keyboard>/r");
            Pause = map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            Pause.AddBinding("<Gamepad>/start");
            Confirm = map.AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
            Confirm.AddBinding("<Keyboard>/numpadEnter");
            Confirm.AddBinding("<Gamepad>/buttonSouth");

            map.Enable();
        }

        public Vector2 MoveValue => Move.ReadValue<Vector2>();

        public void Dispose()
        {
            map.Disable();
            map.Dispose();
        }
    }
}
