using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wing : MonoBehaviour
{
    // Assume leading edge is aligned with gameobject

    AircraftDynamics dynamics;
    MeshFilter meshFilter;

    // PUBLIC Wing Geometry
    public float ZeroLiftAngle;
    public float WingSpan;
    public float RootChord;
    public float TipChord;
    public float SweepAngle;
    public bool Mirror;
    public float TrailFlapChord;
    public float LeadFlapChord;

    // PUBLIC Other Wing Parameters
    public float MaxRotation;
    public float MaxTrailFlapAngle;
    public float MaxLeadFlapAngle;
    public float StallAngle { get; private set; } = 15f * Mathf.Deg2Rad;
    public GameObject FlapPrefab;

    // PRIVATE Calculated Wing Parameters
    private float WingThickness = 0.1f; // fraction of chord length
    private float PlanformArea;
    private float FrontalArea;
    private float Volume;
    private Vector3 CentreOfPressure;
    private Mesh WingMesh;
    private Mesh TrailFlapMesh;
    private Mesh LeadFlapMesh;
    private float switchLeftRight;
    private Quaternion rotationDatum;
    private GameObject TrailFlap;
    private GameObject LeadFlap;

    public bool _debug;

    private void Start()
    {
        dynamics = GetComponentInParent<AircraftDynamics>();
        meshFilter = GetComponent<MeshFilter>();

        // -- Wing --
        rotationDatum = transform.localRotation;
        
        // Build Mesh
        WingMesh = new();

        switchLeftRight = Mirror ? -1f : 1f;
        Vector3 chordShift = Vector3.forward * RootChord * 0.5f;

        Vector3 leadingTip = new Vector3(switchLeftRight * WingSpan, 0f, -WingSpan * Mathf.Tan(switchLeftRight * SweepAngle * Mathf.Deg2Rad));
        Vector3 trailingRoot = new Vector3(0f, 0f, -RootChord);

        if (TipChord > 0f)
        {
            // 4 Sided Wing
            Vector3 trailingTip = leadingTip + new Vector3(0f, 0f, -TipChord);
            WingMesh.vertices = new Vector3[] { chordShift, leadingTip + chordShift, trailingTip + chordShift, trailingRoot + chordShift };
            WingMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            PlanformArea = (Vector3.Cross(leadingTip, trailingTip).magnitude + Vector3.Cross(trailingTip, trailingRoot).magnitude) / 2f;
            FrontalArea = (RootChord * WingThickness + TipChord * WingThickness) * WingSpan * 0.5f;
            Volume = WingSpan * (RootChord * TipChord * WingThickness + TipChord * RootChord * WingThickness + 2f * (TipChord * TipChord * WingThickness + RootChord * RootChord * WingThickness)) / 6f;
        }
        else
        {
            // Triangular Wing
            WingMesh.vertices = new Vector3[] { chordShift, leadingTip + chordShift, trailingRoot + chordShift };
            WingMesh.triangles = new int[] { 0, 1, 2 };
            PlanformArea = Vector3.Cross(leadingTip, trailingRoot).magnitude / 2f;
            FrontalArea = (RootChord * WingThickness + TipChord * WingThickness) * WingSpan * 0.5f;
            Volume = WingSpan * (RootChord * TipChord * WingThickness + TipChord * RootChord * WingThickness + 2f * (TipChord * TipChord * WingThickness + RootChord * RootChord * WingThickness)) / 6f;
        }

        meshFilter.mesh = WingMesh;
        CentreOfPressure = WingMesh.bounds.center;


        // -- Flaps --
        if (TrailFlapChord > 0f)
        {
            // Add trailing edge flap
            TrailFlapMesh = new();
            TrailFlapMesh.vertices = new Vector3[] { Vector3.zero, WingMesh.vertices[^2] - WingMesh.vertices[^1], WingMesh.vertices[^2] - WingMesh.vertices[^1] + new Vector3(0f, 0f, -TrailFlapChord), new Vector3(0f, 0f, -TrailFlapChord) };
            TrailFlapMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            PlanformArea += TrailFlapChord * WingSpan;
            TrailFlap = Instantiate(FlapPrefab, transform.position + WingMesh.vertices[^1], transform.parent.rotation, transform);
            TrailFlap.GetComponent<MeshFilter>().mesh = TrailFlapMesh;
        }

        if (LeadFlapChord > 0f)
        {
            // Add leading edge flap
            LeadFlapMesh = new();
            LeadFlapMesh.vertices = new Vector3[] { new Vector3(0f, 0f, LeadFlapChord), WingMesh.vertices[1] - WingMesh.vertices[0] + new Vector3(0f, 0f, LeadFlapChord), WingMesh.vertices[1] - WingMesh.vertices[0], Vector3.zero };
            LeadFlapMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            PlanformArea += LeadFlapChord * WingSpan;
            LeadFlap = Instantiate(FlapPrefab, transform.position + WingMesh.vertices[0], transform.parent.rotation, transform);
            LeadFlap.GetComponent<MeshFilter>().mesh = LeadFlapMesh;
        }

        if (_debug)
        {
            //Debug.Log(leadingTip);
            //Debug.Log(trailingRoot);
        }
    }

    public void Operate(float incidenceControl, float zeroLiftControl) //incidenceControl and zeroLiftControl are in degrees
    {
        // Update Control Surface Orientation
        //Quaternion rotation = new(Mathf.Sin(incidenceControl / 2f), 0f, 0f, Mathf.Cos(incidenceControl / 2f));
        Quaternion rotation = Quaternion.AngleAxis(Mathf.Clamp(incidenceControl, -MaxRotation, MaxRotation), transform.right);
        transform.localRotation = rotation * rotationDatum; // this is doing q*p*q^-1
        //Debug.Log(rotation + ", " + (rotation * rotationDatum * Quaternion.Inverse(rotation)));


        // Calculate Dynamics
        Vector3 localForward = new Vector3(Mathf.Sin(switchLeftRight * SweepAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(switchLeftRight * SweepAngle * Mathf.Deg2Rad));

        Vector3 localVelocity = transform.InverseTransformVector(dynamics.rb.GetRelativePointVelocity(transform.localPosition)); // Velocity of the parent's rigidbody at this position
        Vector2 liftingVelocity = new Vector2(Vector3.Dot(localForward, localVelocity), Vector3.Dot(Vector3.up, localVelocity)); // exclude velocity parallel to leading edge
        float angleOfAttack = -Mathf.Atan2(liftingVelocity.y, liftingVelocity.x);
        float netAttack = angleOfAttack - ZeroLiftAngle * Mathf.Deg2Rad - zeroLiftControl * Mathf.Deg2Rad / 2f; // Flapped control is half to match trends from graphs


        // Lift
        float Cl = 18f / Mathf.PI * netAttack;
        float Clmax = 18f / Mathf.PI * StallAngle;
        Vector3 liftDir = new(0f, Mathf.Cos(angleOfAttack), Mathf.Sin(angleOfAttack));
        Vector3 lift = Mathf.Abs(angleOfAttack) > StallAngle ? Vector3.zero : 0.5f * dynamics.Density * Mathf.Pow(liftingVelocity.magnitude, 2f) * PlanformArea * Cl * liftDir;

        // Drag
        float Cd = (Mathf.Abs(Cl) / 10f - Mathf.Abs(Cl) / 100f) * (Mathf.Pow(12f, 4f) / Mathf.Pow(Mathf.PI, 4f)) * Mathf.Pow(netAttack, 4f) + (Mathf.Abs(Clmax) / 100f);
        Vector3 dragDir = new(0f, Mathf.Sin(angleOfAttack), -Mathf.Cos(angleOfAttack));
        Vector3 drag = 0.5f * dynamics.Density * Mathf.Pow(liftingVelocity.magnitude, 2f) * FrontalArea * Cd * dragDir; // Should this act perpendicular to leading edge? Don't think so


        // Apply Force
        dynamics.rb.AddForceAtPosition(transform.TransformVector(lift + drag), transform.TransformPoint(CentreOfPressure)); // drag is causing the simulation to *depart*
        if (_debug)
        {
            Debug.DrawRay(transform.TransformPoint(CentreOfPressure), transform.TransformVector(lift));
        }


        // DEBUG
        if (_debug)
        {
            //Debug.Log("Local Forward = " + localForward);
            //Debug.Log("Lifting Velocity = " + liftingVelocity);
            //Debug.Log("Angle of Attack = " + angleOfAttack);
            //Debug.Log("Cl = " + Cl + ", Cd = " + Cd);
            //Debug.Log("Lift = " + lift + ", Drag = " + drag);
            //Debug.Log("Lift dir = " + liftDir + ", Drag dir = " + dragDir);
            //Debug.DrawRay(transform.TransformPoint(WingMesh.bounds.center), transform.TransformDirection(Vector3.up));
            //Debug.Log("Pitch allocation = " + Vector3.Dot(Vector3.right, transform.parent.InverseTransformVector(transform.right)) + ", Yaw allocation = " + Vector3.Dot(Vector3.up, transform.parent.InverseTransformVector(transform.right)) + ", Vector = " + transform.parent.InverseTransformVector(transform.right));
        }
    }

    public float GetAngleOfAttack()
    {
        // Calculate Dynamics
        Vector3 localForward = new Vector3(Mathf.Sin(switchLeftRight * SweepAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(switchLeftRight * SweepAngle * Mathf.Deg2Rad));

        Vector3 localVelocity = transform.InverseTransformVector(dynamics.rb.GetRelativePointVelocity(transform.localPosition)); // Velocity of the parent's rigidbody at this position
        Vector2 liftingVelocity = new Vector2(Vector3.Dot(localForward, localVelocity), Vector3.Dot(Vector3.up, localVelocity)); // exclude velocity parallel to leading edge
        float angleOfAttack = -Mathf.Atan2(liftingVelocity.y, liftingVelocity.x);

        return angleOfAttack;
    }
}
