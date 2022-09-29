using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Engine : MonoBehaviour
{
    MeshFilter meshFilter;
    VisualEffect exhaust;

    CylinderMesh NacelleMesh;
    CylinderMesh NozzleExternalMesh;
    CylinderMesh NozzleInternalLeadMesh;
    CylinderMesh NozzleInternalTrailMesh;

    GameObject NozzleExternal;
    GameObject NozzleInternalLead;
    GameObject NozzleInternalTrail;

    public float EngineLength;
    public float NozzleLength;
    public float Diameter;
    public float MinNozDiameter;
    public float NozzleThroatPos;
    public int CircleSegments;
    public GameObject NozzlePrefab;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        exhaust = GetComponent<VisualEffect>();

        // Nacelle
        NacelleMesh = new(Diameter, Diameter, 0f, EngineLength, CircleSegments);

        // Nozzle
        NozzleExternalMesh = new(Diameter, MinNozDiameter, 0f, -NozzleLength, CircleSegments);
        NozzleInternalLeadMesh = new(Diameter, MinNozDiameter, 0f, -NozzleLength * NozzleThroatPos, CircleSegments);
        NozzleInternalTrailMesh = new(MinNozDiameter, MinNozDiameter, -NozzleLength * NozzleThroatPos, -NozzleLength, CircleSegments);

        NozzleExternal = Instantiate(NozzlePrefab, transform.TransformPoint(Vector3.zero), transform.rotation, transform);
        NozzleInternalLead = Instantiate(NozzlePrefab, transform.TransformPoint(Vector3.zero), transform.rotation, transform);
        NozzleInternalTrail = Instantiate(NozzlePrefab, transform.TransformPoint(Vector3.zero), transform.rotation, transform);

        meshFilter.mesh = NacelleMesh.mesh;
        NozzleExternal.GetComponent<MeshFilter>().mesh = NozzleExternalMesh.mesh;
        NozzleInternalLead.GetComponent<MeshFilter>().mesh = NozzleInternalLeadMesh.mesh;
        NozzleInternalTrail.GetComponent<MeshFilter>().mesh = NozzleInternalTrailMesh.mesh;
    }

    public void UpdateNozzle(float throttle)
    {
        float threshold = 0.7f;
        float nozDiameter = MinNozDiameter;

        if (throttle > threshold)
        {
            // Afterburner
            nozDiameter = Mathf.Lerp(MinNozDiameter, Diameter, (throttle - threshold) / (1f - threshold));
        }

        NozzleExternalMesh.Resize(Diameter, nozDiameter, 0f, -NozzleLength);
        NozzleInternalLeadMesh.Resize(Diameter, MinNozDiameter, 0f, -NozzleLength / 2f);
        NozzleInternalTrailMesh.Resize(MinNozDiameter, nozDiameter, -NozzleLength / 2f, -NozzleLength);

        exhaust.SetFloat("Size", nozDiameter);
        exhaust.SetVector3("Position", new Vector3(0f, 0f, -NozzleLength * NozzleThroatPos));
    }

}
