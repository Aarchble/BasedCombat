using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AircraftDynamics : MonoBehaviour
{
    public Rigidbody rb;
    public FlightControlInput fcs;

    private List<GameObject> Wings;
    private List<GameObject> Engines;

    private float Length = 8f; // FIX ME
    private float WingSpan = 7f; // FIX ME
    private float Height = 3f; // FIX ME
    private float Throttle = 0f;

    public float Density = 1.225f;
    public float GravitationAcceleration = 9.81f;
    public float BackMaxMomentArm { get; private set; } = 0f;
    public float RightMaxMomentArm { get; private set; } = 0f;

    public float _velocity;

    private Vector3 Force;
    private Vector3 Moment;

    private void Start()
    {
        // Wings
        Wings = new();
        foreach (Wing wing in GetComponentsInChildren<Wing>()) // Get all wings attached to this aircraft
        {
            Wings.Add(wing.gameObject);
        }

        // Engines
        Engines = new();
        foreach (Engine engine in GetComponentsInChildren<Engine>()) // Get all wings attached to this aircraft
        {
            Engines.Add(engine.gameObject);
        }

        rb.inertiaTensor = new Vector3((Height * Height + Length * Length) * rb.mass / 12f, (WingSpan * WingSpan + Length * Length) * rb.mass / 12f, (Height * Height + WingSpan * WingSpan) * rb.mass / 12f);
        rb.velocity = new Vector3(0f, 0f, _velocity);
        rb.angularVelocity = new Vector3(0f, 0f, 0.1f);

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

        rb.centerOfMass = Vector3.forward * -0.5f;
    }

    private void FixedUpdate()
    {
        //Debug.Log("Pitch = " + fcs.pitchInceptor + ", Roll = " + fcs.rollInceptor + ", Yaw = " + fcs.yawInceptor);
        Vector3 Weight = rb.mass * GravitationAcceleration * transform.InverseTransformVector(Vector3.down);

        Force = Vector3.zero;
        Moment = Vector3.zero;

        // -- Thrust --
        float maxThrust = 15f * rb.mass;
        Throttle = Mathf.Clamp(Throttle + 0.5f * fcs.ThrottleInceptor * Time.fixedDeltaTime, 0f, 1f);
        Vector3 Thrust = maxThrust * Throttle * Vector3.forward;

        Force += Thrust + Weight;


        // -- Wings --
        for (int i = 0; i < Wings.Count; i++)
        {
            Wing wing = Wings[i].GetComponent<Wing>();

            //float inverterPitch = Vector3.Dot(wing.transform.localPosition + wing.CentreOfPressure, Vector3.back) > 0f ? 1f : -1f;
            //float inverterRoll = Vector3.Dot(wing.transform.localPosition + wing.CentreOfPressure, Vector3.right) > 0f ? 1f : -1f;
            //float inverterYaw = Vector3.Dot(wing.transform.localPosition + wing.CentreOfPressure, Vector3.back) > 0f ? 1f : -1f;

            //float resultantPitchInput = fcs.PitchInceptor * wing.PitchContribution * inverterPitch; //Vector3.Dot(Vector3.right, transform.InverseTransformVector(wing.transform.right))
            //float resultantRollInput = fcs.RollInceptor * wing.RollContribution * inverterRoll;
            //float resultantYawInput = fcs.YawInceptor * wing.YawContribution * inverterYaw; //Vector3.Dot(Vector3.up, transform.InverseTransformVector(wing.transform.right))

            float resultantPitchInput = fcs.PitchInceptor * wing.PitchContribution; //Vector3.Dot(Vector3.right, transform.InverseTransformVector(wing.transform.right))
            float resultantRollInput = fcs.RollInceptor * wing.RollContribution;
            float resultantYawInput = fcs.YawInceptor * wing.YawContribution; //Vector3.Dot(Vector3.up, transform.InverseTransformVector(wing.transform.right))

            float InputSum = resultantPitchInput + resultantRollInput + resultantYawInput;

            float antiStall = Mathf.Abs(wing.MaxRotation) > 0f ? -wing.GetAngleOfAttack() / wing.MaxRotation : 0f;
            float antiStallFlap = Mathf.Abs(wing.MaxTrailFlapAngle) > 0f ? -wing.GetAngleOfAttack() / wing.MaxTrailFlapAngle : 0f;

            wing.Operate(InputSum, InputSum, 0f, transform);
            Force += wing.Force;
            Moment += wing.Moment;
        }


        rb.AddRelativeForce(Force);
        rb.AddRelativeTorque(Moment);
        //Debug.Log(Force + "N, " + Moment + "Nm");
        Debug.Log(Force.magnitude / rb.mass / GravitationAcceleration);
    }

    private void Update()
    {
        for (int i = 0; i < Engines.Count; i++)
        {
            Engine engine = Engines[i].GetComponent<Engine>();
            engine.UpdateNozzle(Throttle);
        }

        // Debug
        for (int i = 0; i < Wings.Count; i++)
        {
            Wing wing = Wings[i].GetComponent<Wing>();
            wing.DebugForces();

            //Debug.Log(transform.InverseTransformVector(rb.angularVelocity));
        }

        // Centre of Mass
        Debug.DrawRay(transform.TransformPoint(rb.centerOfMass), Vector3.up, Color.yellow);
        Debug.DrawRay(transform.TransformPoint(rb.centerOfMass), Vector3.down, Color.black);
        Debug.DrawRay(transform.TransformPoint(rb.centerOfMass), Vector3.left, Color.black);
        Debug.DrawRay(transform.TransformPoint(rb.centerOfMass), Vector3.right, Color.yellow);
        Debug.DrawRay(transform.TransformPoint(rb.centerOfMass), Vector3.forward, Color.yellow);
        Debug.DrawRay(transform.TransformPoint(rb.centerOfMass), Vector3.back, Color.black);

        //Debug.Log(Throttle);
    }
}
