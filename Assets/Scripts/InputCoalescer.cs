using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InControl;
using UnityEngine;

static class InputCoalescer
{
    public static float StickDeadzone = 0.1f;

    public class PlayerInput
    {
        public bool IsGamepad;

        public bool AttachPressed;
        public bool AttachHeld;
        public bool DetachPressed;

        public bool MovingLeft;
        public bool MovingRight;
        public float MovementSpeed;

        public bool Restart;

        public void Reset()
        {
            Restart = false;
            IsGamepad = false;
            AttachPressed = AttachHeld = DetachPressed = false;
            MovingLeft = MovingRight = false;
            MovementSpeed = 0.0f;
        }
    }

    public static PlayerInput[] Players = new[] { new PlayerInput(), new PlayerInput() };

    public static void Update(bool coalesceInterPlayer)
    {
        InputManager.Update();

        Players[0].Reset();
        Players[1].Reset();

        if (InputManager.Devices.Count >= 1)
            UpdateGamepadPlayer(InputManager.Devices[0], 0);

        if (InputManager.Devices.Count >= 2)
            UpdateGamepadPlayer(InputManager.Devices[1], 1);

        UpdateFirstPlayerKeyboard();
        UpdateSecondPlayerKeyboard();

        if (Players[0].MovingLeft && Players[0].MovingRight)
            Players[0].MovingLeft = Players[0].MovingRight = false;
        if (Players[1].MovingLeft && Players[1].MovingRight)
            Players[1].MovingLeft = Players[1].MovingRight = false;

        // Title screen coalesces input for all devices
        if (coalesceInterPlayer)
        {
            Players[0].MovingLeft = Players[0].MovingLeft | Players[1].MovingLeft;
            Players[0].MovingRight = Players[0].MovingRight | Players[1].MovingRight;
            Players[0].MovementSpeed = Mathf.Max(Players[0].MovementSpeed, Players[1].MovementSpeed);
        }
    }

    static void UpdateGamepadPlayer(InputDevice controller, int playerIndex)
    {
        Players[playerIndex].AttachPressed = controller.Action1.WasPressed;
        Players[playerIndex].AttachHeld = controller.Action1.IsPressed;
        Players[playerIndex].DetachPressed = controller.Action2.WasPressed;
        Players[playerIndex].MovingLeft = controller.DPadLeft.IsPressed || controller.LeftStickX.Value < -StickDeadzone;
        Players[playerIndex].MovingRight = controller.DPadRight.IsPressed || controller.LeftStickX.Value > StickDeadzone;
        Players[playerIndex].MovementSpeed = Math.Abs(controller.DPadLeft.IsPressed || controller.DPadRight.IsPressed ? 1.0f : controller.LeftStickX.Value);
        Players[playerIndex].Restart = controller.Action4.WasPressed;
        Players[playerIndex].IsGamepad = true;
    }

    static void UpdateFirstPlayerKeyboard()
    {
        Players[0].AttachPressed |= Input.GetKeyDown(KeyCode.W);
        Players[0].AttachHeld |= Input.GetKey(KeyCode.W);
        Players[0].DetachPressed |= Input.GetKeyDown(KeyCode.S);
        Players[0].MovingLeft |= Input.GetKey(KeyCode.A);
        Players[0].MovingRight |= Input.GetKey(KeyCode.D);
        Players[0].MovementSpeed = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ? 1.0f : Players[0].MovementSpeed;
        Players[0].Restart |= Input.GetKeyDown(KeyCode.Escape);
    }

    static void UpdateSecondPlayerKeyboard()
    {
        Players[1].AttachPressed |= Input.GetKeyDown(KeyCode.UpArrow);
        Players[1].AttachHeld |= Input.GetKey(KeyCode.UpArrow);
        Players[1].DetachPressed |= Input.GetKeyDown(KeyCode.DownArrow);
        Players[1].MovingLeft |= Input.GetKey(KeyCode.LeftArrow);
        Players[1].MovingRight |= Input.GetKey(KeyCode.RightArrow);
        Players[1].MovementSpeed = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ? 1.0f : Players[1].MovementSpeed;
        Players[1].Restart |= Input.GetKeyDown(KeyCode.Escape);
    }
}
