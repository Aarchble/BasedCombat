using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlightControlInput : MonoBehaviour
{
    public float PitchInceptor { get; private set; } = 0f;
    public float RollInceptor { get; private set; } = 0f;
    public float YawInceptor { get; private set; } = 0f;
    public float ThrottleInceptor { get; private set; } = 0f;

    PlayerInputActions _input;

    private void OnEnable()
    {
        _input = new PlayerInputActions();
        _input.FlightControls.Enable();

        _input.FlightControls.Pitch.performed += SetPitch;

        _input.FlightControls.Roll.performed += SetRoll;

        _input.FlightControls.Yaw.performed += SetYaw;

        _input.FlightControls.Throttle.performed += SetThrottle;
        _input.FlightControls.Throttle.canceled += ZeroThrottle;
    }

    private void OnDisable()
    {
        _input.FlightControls.Pitch.performed -= SetPitch;

        _input.FlightControls.Roll.performed -= SetRoll;

        _input.FlightControls.Yaw.performed -= SetYaw;

        _input.FlightControls.Throttle.performed -= SetThrottle;
        _input.FlightControls.Throttle.canceled -= ZeroThrottle;

        _input.FlightControls.Disable();
    }

    private void SetPitch(InputAction.CallbackContext context)
    {
        PitchInceptor = context.ReadValue<float>();
    }

    private void SetRoll(InputAction.CallbackContext context)
    {
        RollInceptor = context.ReadValue<float>();
    }

    private void SetYaw(InputAction.CallbackContext context)
    {
        YawInceptor = context.ReadValue<float>();
    }

    private void SetThrottle(InputAction.CallbackContext context)
    {
        ThrottleInceptor = context.ReadValue<float>();
    }

    private void ZeroThrottle(InputAction.CallbackContext context)
    {
        ThrottleInceptor = 0f;
    }

}
