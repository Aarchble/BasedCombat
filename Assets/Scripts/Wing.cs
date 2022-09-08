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
    public Vector3 CentreOfPressure { get; private set; }

    // PRIVATE Calculated Wing Parameters
    private float WingThickness = 0.1f; // fraction of chord length
    private float PlanformArea;
    private float FrontalArea;
    private float Volume;
    private Mesh WingMesh;
    private Mesh TrailFlapMesh;
    private Mesh LeadFlapMesh;
    private float switchLeftRight;
    private Quaternion rotationDatum;
    private GameObject TrailFlap;
    private GameObject LeadFlap;
    private Quaternion TrailFlapRotationDatum;
    private Quaternion LeadFlapRotationDatum;
    private float _combinedFlapChord;
    private float _spanFractionCoP;

    // PRIVATE Wing Dynamics
    private Vector3 Lift;
    private Vector3 Drag;

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

        Vector3 leadingTip = new Vector3(switchLeftRight * WingSpan, 0f, -WingSpan * Mathf.Tan(SweepAngle * Mathf.Deg2Rad));
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


        // -- Flaps --
        if (TrailFlapChord > 0f)
        {
            // Add trailing edge flap
            TrailFlapMesh = new();
            TrailFlapMesh.vertices = new Vector3[] { Vector3.zero, WingMesh.vertices[^2] - WingMesh.vertices[^1], WingMesh.vertices[^2] - WingMesh.vertices[^1] + new Vector3(0f, 0f, -TrailFlapChord), new Vector3(0f, 0f, -TrailFlapChord) };
            TrailFlapMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            PlanformArea += TrailFlapChord * WingSpan; // Add control surface to wing area
            TrailFlap = Instantiate(FlapPrefab, transform.position + WingMesh.vertices[^1], transform.parent.rotation, transform);
            TrailFlap.GetComponent<MeshFilter>().mesh = TrailFlapMesh;
            TrailFlapRotationDatum = TrailFlap.transform.localRotation;
        }

        if (LeadFlapChord > 0f)
        {
            // Add leading edge flap
            LeadFlapMesh = new();
            LeadFlapMesh.vertices = new Vector3[] { new Vector3(0f, 0f, LeadFlapChord), WingMesh.vertices[1] - WingMesh.vertices[0] + new Vector3(0f, 0f, LeadFlapChord), WingMesh.vertices[1] - WingMesh.vertices[0], Vector3.zero };
            LeadFlapMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            PlanformArea += LeadFlapChord * WingSpan; // Add control surface to wing area
            LeadFlap = Instantiate(FlapPrefab, transform.position + WingMesh.vertices[0], transform.parent.rotation, transform);
            LeadFlap.GetComponent<MeshFilter>().mesh = LeadFlapMesh;
            LeadFlapRotationDatum = LeadFlap.transform.localRotation;
        }


        // -- Centre of Pressure --
        _combinedFlapChord = TrailFlapChord + LeadFlapChord;
        _spanFractionCoP = (2f * (TipChord + _combinedFlapChord) + RootChord + _combinedFlapChord) / (3f * (TipChord + RootChord + 2f * _combinedFlapChord)); // Uncluttered Formula: (2f * TipChord + RootChord) / (3f * (TipChord + RootChord))


        if (_debug)
        {
            //Debug.Log(leadingTip);
            //Debug.Log(trailingRoot);
        }
    }

    public void Operate(float wingControl, float trailFlapControl, float leadFlapControl) //incidenceControl and zeroLiftControl are in degrees
    {
        // -- Update Control Surface Orientation --
        // Wing
        //Quaternion rotation = new(Mathf.Sin(incidenceControl / 2f), 0f, 0f, Mathf.Cos(incidenceControl / 2f));
        Quaternion rotation = Quaternion.AngleAxis(Mathf.Clamp(switchLeftRight * wingControl * MaxRotation, -MaxRotation, MaxRotation), switchLeftRight * Vector3.right); // rotation vector points from root to tip and is normal to chordline
        transform.localRotation = rotation * rotationDatum; // this is doing q*p*q^-1
        
        // Flaps
        if (TrailFlap != null)
        {
            Quaternion trailFlapRotation = Quaternion.AngleAxis(Mathf.Clamp(switchLeftRight * trailFlapControl * MaxTrailFlapAngle, -MaxTrailFlapAngle, MaxTrailFlapAngle), (WingMesh.vertices[^2] - WingMesh.vertices[^1]));
            TrailFlap.transform.localRotation = trailFlapRotation * TrailFlapRotationDatum;
        }
        if (LeadFlap != null)
        {
            Quaternion leadFlapRotation = Quaternion.AngleAxis(Mathf.Clamp(switchLeftRight * leadFlapControl * MaxLeadFlapAngle, -MaxLeadFlapAngle, MaxLeadFlapAngle), (WingMesh.vertices[1] - WingMesh.vertices[0]));
            LeadFlap.transform.localRotation = leadFlapRotation * LeadFlapRotationDatum;
        }


        // -- Calculate Dynamics --
        Vector3 localForward = new Vector3(Mathf.Sin(switchLeftRight * SweepAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(switchLeftRight * SweepAngle * Mathf.Deg2Rad));

        Vector3 localVelocity = transform.InverseTransformVector(dynamics.rb.GetRelativePointVelocity(transform.localPosition)); // Velocity of the parent's rigidbody at this position
        Vector2 liftingVelocity = new Vector2(Vector3.Dot(localForward, localVelocity), Vector3.Dot(Vector3.up, localVelocity)); // exclude velocity parallel to leading edge
        float angleOfAttack = -Mathf.Atan2(liftingVelocity.y, liftingVelocity.x);
        float modAttack = -ZeroLiftAngle * Mathf.Deg2Rad - trailFlapControl * MaxTrailFlapAngle * Mathf.Deg2Rad / 2f; // Flapped control is half to match trends from graphs

        float Cl;
        float Cd;
        float Clmax = 18f / Mathf.PI * (StallAngle + modAttack);

        if (Mathf.Abs(angleOfAttack) > StallAngle)
        {
            // Stalled
            Cl = 0f;
            Cd = 9f * Mathf.Abs(Cl) / 100f * (20736f / Mathf.Pow(Mathf.PI, 4f)) * Mathf.Pow(StallAngle + modAttack, 4f) + (Mathf.Abs(Clmax) / 100f);
        }
        else
        {
            // Nominal
            Cl = 18f / Mathf.PI * (angleOfAttack + modAttack);
            Cd = 9f * Mathf.Abs(Cl) / 100f * (20736f / Mathf.Pow(Mathf.PI, 4f)) * Mathf.Pow(angleOfAttack + modAttack, 4f) + (Mathf.Abs(Clmax) / 100f);
        }

        // Lift
        Vector3 liftDir = new(0f, Mathf.Cos(angleOfAttack), Mathf.Sin(angleOfAttack));
        Lift = 0.5f * dynamics.Density * Mathf.Pow(liftingVelocity.magnitude, 2f) * PlanformArea * Cl * liftDir;

        // Drag
        Vector3 dragDir = new(0f, Mathf.Sin(angleOfAttack), -Mathf.Cos(angleOfAttack));
        Drag = 0.5f * dynamics.Density * Mathf.Pow(liftingVelocity.magnitude, 2f) * FrontalArea * Cd * dragDir; // Should this act perpendicular to leading edge? Don't think so


        // -- Apply Force --
        CentreOfPressure = CalculateCoP(0.25f);
        dynamics.rb.AddForceAtPosition(transform.TransformVector(Lift + Drag), transform.TransformPoint(CentreOfPressure)); // drag is causing the simulation to *depart*


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
            //Debug.Log(rotation + ", " + (rotation * rotationDatum * Quaternion.Inverse(rotation)));
            //Debug.Log(leadFlapControl);
            //Debug.Log(Mathf.Abs(angleOfAttack - leadFlapControl * Mathf.Deg2Rad / 2f) * Mathf.Rad2Deg);
        }
    }

    private Vector3 CalculateCoP(float chordFractionCoP)
    {
        return Vector3.Lerp(WingMesh.vertices[0] - new Vector3(0f, 0f, chordFractionCoP * (RootChord + _combinedFlapChord) - LeadFlapChord), WingMesh.vertices[1] - new Vector3(0f, 0f, chordFractionCoP * (TipChord + _combinedFlapChord) - LeadFlapChord), _spanFractionCoP);
    }

    public void DebugForces()
    {
        if (_debug)
        {
            Debug.DrawRay(transform.TransformPoint(CentreOfPressure), transform.TransformVector(Lift), Color.green);
            Debug.DrawRay(transform.TransformPoint(CentreOfPressure), transform.TransformVector(Drag), Color.red);
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
