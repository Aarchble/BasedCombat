using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AircraftDynamics : MonoBehaviour
{
    public Rigidbody rb;
    public FlightControlInput fcs;

    public List<GameObject> Wings;

    private float Length = 8f;
    private float WingSpan = 7f;
    private float Height = 3f;

    public float Density = 1.225f;
    public float _deflection = 0f;
    public float BackMaxMomentArm { get; private set; } = 0f;
    public float RightMaxMomentArm { get; private set; } = 0f;

    private void Start()
    {
        rb.inertiaTensor = new Vector3((Height * Height + Length * Length) * rb.mass / 12f, (WingSpan * WingSpan + Length * Length) * rb.mass / 12f, (Height * Height + WingSpan * WingSpan) * rb.mass / 12f);
        rb.velocity = new Vector3(0f, 0f, 10f);

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
        Debug.Log("Max moment arms: forward = " + BackMaxMomentArm + ", right = " + RightMaxMomentArm);
    }

    private void FixedUpdate()
    {
        Debug.Log("Pitch = " + fcs.pitchInceptor + ", Roll = " + fcs.rollInceptor + ", Yaw = " + fcs.yawInceptor);

        for (int i = 0; i < Wings.Count; i++)
        {
            Wing wing = Wings[i].GetComponent<Wing>();
            wing.Operate((fcs.pitchInceptor * Vector3.Dot(wing.transform.localPosition, Vector3.back) / BackMaxMomentArm + fcs.rollInceptor * Vector3.Dot(wing.transform.localPosition, Vector3.right) / RightMaxMomentArm) * wing.MaxRotation, 0f);
        }
    }
}
