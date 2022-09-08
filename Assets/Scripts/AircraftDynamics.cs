using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AircraftDynamics : MonoBehaviour
{
    public Rigidbody rb;
    public FlightControlInput fcs;

    private List<GameObject> Wings;

    private float Length = 8f;
    private float WingSpan = 7f;
    private float Height = 3f;

    public float Density = 1.225f;
    public float _deflection = 0f;
    public float BackMaxMomentArm { get; private set; } = 0f;
    public float RightMaxMomentArm { get; private set; } = 0f;

    private void Start()
    {
        Wings = new();
        foreach (Wing wing in GetComponentsInChildren<Wing>()) // Get all wings attached to this aircraft
        {
            Wings.Add(wing.gameObject);
        }

        rb.inertiaTensor = new Vector3((Height * Height + Length * Length) * rb.mass / 12f, (WingSpan * WingSpan + Length * Length) * rb.mass / 12f, (Height * Height + WingSpan * WingSpan) * rb.mass / 12f);
        rb.velocity = new Vector3(0f, 0f, 100f);

        for (int i = 0; i < Wings.Count; i++)
        {
            float backMomentArm = Mathf.Abs(Vector3.Dot(Wings[i].transform.localPosition, Vector3.back));
            if (backMomentArm > BackMaxMomentArm)
            {
                BackMaxMomentArm = backMomentArm;
            }

            float rightMomentArm = Mathf.Abs(Vector3.Dot(Wings[i].transform.localPosition, Vector3.right));
            if (rightMomentArm > RightMaxMomentArm)
            {
                RightMaxMomentArm = rightMomentArm;
            }
        }
    }

    private void FixedUpdate()
    {
        //Debug.Log("Pitch = " + fcs.pitchInceptor + ", Roll = " + fcs.rollInceptor + ", Yaw = " + fcs.yawInceptor);

        for (int i = 0; i < Wings.Count; i++)
        {
            Wing wing = Wings[i].GetComponent<Wing>();

            float inverterPitch = Vector3.Dot(wing.transform.localPosition + wing.CentreOfPressure, Vector3.back) > 0f ? 1f : -1f;
            float inverterRoll = Vector3.Dot(wing.transform.localPosition + wing.CentreOfPressure, Vector3.right) > 0f ? 1f : -1f;
            float inverterYaw = Vector3.Dot(wing.transform.localPosition + wing.CentreOfPressure, Vector3.back) > 0f ? 1f : -1f;
            Debug.Log(wing.transform.localPosition + wing.CentreOfPressure);

            float resultantPitchInput = fcs.pitchInceptor * inverterPitch;
            float resultantRollInput = fcs.rollInceptor * inverterRoll;
            float resultantYawInput = fcs.yawInceptor * inverterYaw;

            float InputSum = resultantPitchInput + resultantRollInput + resultantYawInput;
            wing.Operate(InputSum, InputSum, 0f);
        }
    }

    private void Update()
    {
        for (int i = 0; i < Wings.Count; i++)
        {
            Wing wing = Wings[i].GetComponent<Wing>();
            wing.DebugForces();
        }
    }
}
