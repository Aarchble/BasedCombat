using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlightControlInput : MonoBehaviour
{
    public float pitchInceptor { get; private set; }
    public float rollInceptor { get; private set; }
    public float yawInceptor { get; private set; }

    PlayerInputActions _input;

    private void OnEnable()
    {
        _input = new PlayerInputActions();
        _input.FlightControls.Enable();

        _input.FlightControls.Pitch.performed += SetPitch;
        _input.FlightControls.Pitch.canceled += SetPitch;

        _input.FlightControls.Roll.performed += SetRoll;
        _input.FlightControls.Roll.canceled += SetRoll;

        _input.FlightControls.Yaw.performed += SetYaw;
        _input.FlightControls.Yaw.canceled += SetYaw;
    }

    private void OnDisable()
    {
        _input.FlightControls.Pitch.performed -= SetPitch;
        _input.FlightControls.Pitch.canceled -= SetPitch;

        _input.FlightControls.Roll.performed -= SetRoll;
        _input.FlightControls.Roll.canceled -= SetRoll;

        _input.FlightControls.Yaw.performed -= SetYaw;
        _input.FlightControls.Yaw.canceled -= SetYaw;

        _input.FlightControls.Disable();
    }

    private void SetPitch(InputAction.CallbackContext context)
    {
        pitchInceptor = context.ReadValue<float>();
    }

    private void SetRoll(InputAction.CallbackContext context)
    {
        rollInceptor = context.ReadValue<float>();
    }

    private void SetYaw(InputAction.CallbackContext context)
    {
        yawInceptor = context.ReadValue<float>();
    }

}
