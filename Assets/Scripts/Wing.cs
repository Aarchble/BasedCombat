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
    public float PivotPosition = 0.5f;
    public float TrailFlapChord;
    public float LeadFlapChord;

    // PUBLIC Other Wing Parameters
    public float MaxRotation;
    public float MaxTrailFlapAngle;
    public float MaxLeadFlapAngle;
    public float StallAngle { get; private set; } = 15f * Mathf.Deg2Rad;
    public GameObject FlapPrefab;
    public Vector3 CentreOfPressure { get; private set; }
    public float PitchContribution;
    public float RollContribution;
    public float YawContribution;

    // PRIVATE Calculated Wing Parameters
    private float WingThickness = 0.1f; // fraction of chord length
    private float PlanformArea;
    private float FrontalArea;
    private float AspectRatio;
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
    private float FlapEffectiveness = 0.5f;

    // PRIVATE Wing Dynamics
    private Vector3 Lift;
    private Vector3 Drag;

    public bool _debug;

    private void Awake()
    {
        dynamics = GetComponentInParent<AircraftDynamics>();
        meshFilter = GetComponent<MeshFilter>();


        // -- Wing --
        rotationDatum = transform.localRotation;
        
        // Build Mesh
        WingMesh = new();

        switchLeftRight = Mirror ? -1f : 1f;
        Vector3 chordShift = Vector3.forward * RootChord * PivotPosition;

        Vector3 leadingTip = new Vector3(switchLeftRight * WingSpan, 0f, -WingSpan * Mathf.Tan(SweepAngle * Mathf.Deg2Rad));
        Vector3 trailingRoot = new Vector3(0f, 0f, -RootChord);

        if (TipChord > 0f)
        {
            // 4 Sided Wing
            Vector3 trailingTip = leadingTip + new Vector3(0f, 0f, -TipChord);
            WingMesh.vertices = new Vector3[] { chordShift, leadingTip + chordShift, trailingTip + chordShift, trailingRoot + chordShift };
            WingMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            WingMesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            PlanformArea = (Vector3.Cross(leadingTip, trailingTip).magnitude + Vector3.Cross(trailingTip, trailingRoot).magnitude) / 2f;
            FrontalArea = (RootChord * WingThickness + TipChord * WingThickness) * WingSpan * 0.5f;
            Volume = WingSpan * (RootChord * TipChord * WingThickness + TipChord * RootChord * WingThickness + 2f * (TipChord * TipChord * WingThickness + RootChord * RootChord * WingThickness)) / 6f;
        }
        else
        {
            // Triangular Wing
            WingMesh.vertices = new Vector3[] { chordShift, leadingTip + chordShift, trailingRoot + chordShift };
            WingMesh.triangles = new int[] { 0, 1, 2 };
            WingMesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up };
            PlanformArea = Vector3.Cross(leadingTip, trailingRoot).magnitude / 2f;
            FrontalArea = (RootChord * WingThickness + TipChord * WingThickness) * WingSpan * 0.5f;
            Volume = WingSpan * (RootChord * TipChord * WingThickness + TipChord * RootChord * WingThickness + 2f * (TipChord * TipChord * WingThickness + RootChord * RootChord * WingThickness)) / 6f;
        }

        AspectRatio = Mathf.Abs(SweepAngle) > 0f ? 4f / Mathf.Tan(SweepAngle * Mathf.Deg2Rad) : 8f * WingSpan / (RootChord + TipChord); // Delta wing aspect ratio if swept, otherwise regular aspect ratio with average chord
        meshFilter.mesh = WingMesh;


        // -- Flaps --
        if (TrailFlapChord > 0f)
        {
            // Add trailing edge flap
            TrailFlapMesh = new();
            TrailFlapMesh.vertices = new Vector3[] { Vector3.zero, WingMesh.vertices[^2] - WingMesh.vertices[^1], WingMesh.vertices[^2] - WingMesh.vertices[^1] + new Vector3(0f, 0f, -TrailFlapChord), new Vector3(0f, 0f, -TrailFlapChord) };
            TrailFlapMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            TrailFlapMesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            PlanformArea += TrailFlapChord * WingSpan; // Add control surface to wing area
            TrailFlap = Instantiate(FlapPrefab, transform.TransformPoint(WingMesh.vertices[^1]), transform.rotation, transform);
            TrailFlap.GetComponent<MeshFilter>().mesh = TrailFlapMesh;
            TrailFlapRotationDatum = TrailFlap.transform.localRotation;
        }

        if (LeadFlapChord > 0f)
        {
            // Add leading edge flap
            LeadFlapMesh = new();
            LeadFlapMesh.vertices = new Vector3[] { new Vector3(0f, 0f, LeadFlapChord), WingMesh.vertices[1] - WingMesh.vertices[0] + new Vector3(0f, 0f, LeadFlapChord), WingMesh.vertices[1] - WingMesh.vertices[0], Vector3.zero };
            LeadFlapMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            LeadFlapMesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            PlanformArea += LeadFlapChord * WingSpan; // Add control surface to wing area
            LeadFlap = Instantiate(FlapPrefab, transform.TransformPoint(WingMesh.vertices[0]), transform.rotation, transform);
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

    private void Start()
    {
        // -- Sub-Wing --
        if (transform.parent.TryGetComponent(out Wing parentWing))
        {
            Vector3 parentTip = parentWing.WingMesh.vertices[1];
            transform.localPosition = new Vector3(parentTip.x, 0f, parentTip.z); // Position sub-wing at parent wing tip
        }
    }


    private float[] LiftCoefficient(float angleOfAttack, float trailFlapControl, float leadFlapControl)
    {
        // Get slope and stall bounds
        float liftCurveMagnitude = Mathf.Sqrt(Mathf.Pow(1.5f, 2f) + Mathf.Pow(StallAngle, 2f)); // 1.5 at 15 degrees
        float liftCurveGradient = (-SweepAngle / 1250f + 0.1f) * Mathf.Rad2Deg;
        float stallAngle = liftCurveMagnitude * Mathf.Cos(Mathf.Atan(liftCurveGradient)); // Modified by vortex lift

        // Get effective AoA
        float modAttack = (-ZeroLiftAngle - FlapEffectiveness * (trailFlapControl * MaxTrailFlapAngle - leadFlapControl * MaxLeadFlapAngle)) * Mathf.Deg2Rad; // Flapped control is half to match trends from graphs

        float Cl;
        float Cd;
        float Clmax = (stallAngle + modAttack) * liftCurveGradient;
        if (Mathf.Abs(angleOfAttack - FlapEffectiveness * leadFlapControl * MaxLeadFlapAngle * Mathf.Deg2Rad) > stallAngle) // Check stall modified by leading edge flap deflection
        {
            // Stalled Lift
            Cl = 0f;
        }
        else
        {
            // Nominal Lift
            Cl = (angleOfAttack + modAttack) * liftCurveGradient;
        }
        // Drag
        Cd = Mathf.Clamp(9f * Mathf.Abs(Clmax) / 100f * (20736f / Mathf.Pow(Mathf.PI, 4f)) * Mathf.Pow(angleOfAttack + modAttack, 4f) + (Mathf.Abs(Clmax) / 100f), 0f, 2f);

        return new float[] { Cl, Cd };
    }


    public void Operate(float wingControl, float trailFlapControl, float leadFlapControl, Transform playerTransform) //incidenceControl and zeroLiftControl are in degrees
    {
        // -- Update Control Surface Orientation --
        // Wing
        //Quaternion rotation = new(Mathf.Sin(incidenceControl / 2f), 0f, 0f, Mathf.Cos(incidenceControl / 2f));
        Quaternion rotation = Quaternion.AngleAxis(Mathf.Clamp(switchLeftRight * wingControl * MaxRotation, -MaxRotation, MaxRotation), switchLeftRight * playerTransform.InverseTransformVector(transform.right)); // rotation vector points from root to tip and is normal to chordline
        transform.localRotation = rotation * rotationDatum; // this is doing q*p*q^-1
        
        // Flaps
        if (TrailFlap != null)
        {
            Quaternion trailFlapRotation = Quaternion.AngleAxis(Mathf.Clamp(switchLeftRight * trailFlapControl * MaxTrailFlapAngle, -MaxTrailFlapAngle, MaxTrailFlapAngle), WingMesh.vertices[^2] - WingMesh.vertices[^1]);
            TrailFlap.transform.localRotation = trailFlapRotation * TrailFlapRotationDatum;
        }
        if (LeadFlap != null)
        {
            Quaternion leadFlapRotation = Quaternion.AngleAxis(Mathf.Clamp(switchLeftRight * leadFlapControl * MaxLeadFlapAngle, -MaxLeadFlapAngle, MaxLeadFlapAngle), WingMesh.vertices[1] - WingMesh.vertices[0]);
            LeadFlap.transform.localRotation = leadFlapRotation * LeadFlapRotationDatum;
        }


        // -- Calculate Dynamics --
        float trailFlapCoP = MaxTrailFlapAngle > 0f ? 0.25f * -trailFlapControl : 0f;
        CentreOfPressure = CalculateCoP(0.25f + trailFlapCoP);

        // Localise velocity
        Vector3 localForward = new Vector3(Mathf.Sin(switchLeftRight * SweepAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(switchLeftRight * SweepAngle * Mathf.Deg2Rad));
        Vector3 localVelocity = transform.InverseTransformVector(dynamics.rb.GetPointVelocity(transform.TransformPoint(CentreOfPressure))); // Velocity of the parent's rigidbody at this position
        Vector2 liftingVelocity = new Vector2(Vector3.Dot(localForward, localVelocity), Vector3.Dot(Vector3.up, localVelocity)); // exclude velocity parallel to leading edge
        float angleOfAttack = -Mathf.Atan2(liftingVelocity.y, liftingVelocity.x);

        float[] ClCd = LiftCoefficient(angleOfAttack, trailFlapControl, leadFlapControl); // Lift : [0], Drag : [1]

        // Lift
        Vector3 liftDir = switchLeftRight * -Vector3.Cross(-localVelocity, WingMesh.vertices[1] - WingMesh.vertices[0]).normalized;
        Lift = 0.5f * dynamics.Density * Mathf.Pow(liftingVelocity.magnitude, 2f) * PlanformArea * ClCd[0] * liftDir;

        // Drag
        Vector3 dragDir = -localVelocity.normalized;
        Drag = 0.5f * dynamics.Density * Mathf.Pow(liftingVelocity.magnitude, 2f) * (FrontalArea + PlanformArea * Mathf.Sin(Mathf.Abs(angleOfAttack))) * ClCd[1] * dragDir; // Should this act perpendicular to leading edge? Don't think so


        // -- Apply Force --
        dynamics.rb.AddForceAtPosition(transform.TransformVector(Lift + Drag), transform.TransformPoint(CentreOfPressure)); // drag is causing the simulation to *depart*


        // DEBUG
        if (_debug)
        {
            //Debug.Log("Local Forward = " + localForward);
            //Debug.Log("Lifting Velocity = " + liftingVelocity);
            //Debug.Log("Local Velocity = " + localVelocity);
            //Debug.Log("Angle of Attack = " + angleOfAttack * Mathf.Rad2Deg);
            //Debug.Log("Cl = " + ClCd[0] + ", Cd = " + ClCd[1]);
            //Debug.Log("Lift = " + Lift + ", Drag = " + Drag);
            //Debug.Log("Lift dir = " + liftDir + ", Drag dir = " + dragDir);
            //Debug.DrawRay(transform.TransformPoint(WingMesh.bounds.center), transform.TransformDirection(Vector3.up));
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
            Debug.DrawRay(transform.TransformPoint(CentreOfPressure), transform.TransformVector(Lift / 10f), Color.green);
            Debug.DrawRay(transform.TransformPoint(CentreOfPressure), transform.TransformVector(Drag / 10f), Color.red);
            Debug.DrawRay(transform.TransformPoint(CentreOfPressure), transform.TransformVector((Lift + Drag) / 10f), Color.yellow);
            Debug.DrawRay(transform.TransformPoint(CentreOfPressure), transform.TransformVector(transform.InverseTransformVector(dynamics.rb.GetPointVelocity(transform.TransformPoint(CentreOfPressure)))), Color.cyan);
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
