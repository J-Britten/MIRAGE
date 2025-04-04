using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VideoProjectionSurface : MonoBehaviour
{
    [SerializeField] private float fieldOfViewDegrees = 78f;
    [SerializeField] private int segments = 32;
    [SerializeField] private float aspectRatio = 16f/9f;
    [SerializeField] private float viewingDistanceMeters = 1f; // Distance from viewer to projection center
    [SerializeField] private Transform viewerTransform;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Material projectionMaterial;

    private Mesh projectionMesh;

    private float compensation;
    private void Start()
    {
        if (projectionMesh == null)
        {
            GenerateProjectionMesh();
        }
        SetupMaterial();
        UpdateProjectionTransform();
    }

    private void OnValidate()
    {
        if (!gameObject.activeInHierarchy) return;
        
        GenerateProjectionMesh();
        UpdateProjectionTransform();
        
        // Update material in editor
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SetupMaterial();
        }
        #endif
    }

    private void UpdateProjectionTransform()
    {
        if (viewerTransform == null) return;

        compensation = CalculateDistanceCompensation();
        if (fieldOfViewDegrees == 180f) Debug.LogError("Field of view cannot be exactly 180 degrees, use 179.9 instead");
        bool flipDirection = fieldOfViewDegrees > 180f;
        
        // Position the surface viewingDistanceMeters in front of the viewer
        // Add compensation distance to maintain correct FOV at edges
        Vector3 offset = viewerTransform.forward * (viewingDistanceMeters + compensation);
        transform.position = viewerTransform.position + (flipDirection ? -offset : offset);
        
        // Match the viewer's rotation, flip if FOV > 180
        transform.rotation = flipDirection ? 
            viewerTransform.rotation * Quaternion.Euler(0, 180f, 0) : 
            viewerTransform.rotation;
    }

    private void GenerateProjectionMesh()
    {
        float fovRadians = fieldOfViewDegrees * Mathf.Deg2Rad;
        float sphereRadius = CalculateSphereRadius();
        
        // Calculate vertical FOV based on horizontal FOV and aspect ratio
        float verticalFov = 2f * Mathf.Atan(Mathf.Tan(fovRadians * 0.5f) / aspectRatio);
        
        if (projectionMesh == null)
        {
            projectionMesh = new Mesh();
            projectionMesh.name = "ProjectionMesh";
        }
        else
        {
            projectionMesh.Clear();
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        int verticalSegments = segments;
        int horizontalSegments = segments;

        Vector3[] vertices = new Vector3[(verticalSegments + 1) * (horizontalSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[verticalSegments * horizontalSegments * 6];

        float halfHorizontalFov = fovRadians * 0.5f;
        float halfVerticalFov = verticalFov * 0.5f;
        float segmentHorizontalAngle = fovRadians / horizontalSegments;
        
        for (int v = 0; v <= verticalSegments; v++)
        {
            float verticalPercent = v / (float)verticalSegments;
            float verticalAngle = -halfVerticalFov + verticalFov * verticalPercent;
            
            for (int h = 0; h <= horizontalSegments; h++)
            {
                int index = v * (horizontalSegments + 1) + h;
                float horizontalAngle = -halfHorizontalFov + h * segmentHorizontalAngle;
                
                // Create vertex on sphere surface without additional radius scaling
                Vector3 vertex = new Vector3(
                    Mathf.Sin(horizontalAngle) * Mathf.Cos(verticalAngle) * sphereRadius,
                    Mathf.Sin(verticalAngle) * sphereRadius,
                    Mathf.Cos(horizontalAngle) * Mathf.Cos(verticalAngle) * sphereRadius
                );

                vertices[index] = vertex;
                uvs[index] = new Vector2(h / (float)horizontalSegments, verticalPercent);
            }
        }

        int triIndex = 0;
        for (int v = 0; v < verticalSegments; v++)
        {
            for (int h = 0; h < horizontalSegments; h++)
            {
                int current = v * (horizontalSegments + 1) + h;
                int next = current + horizontalSegments + 1;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
                triangles[triIndex++] = current + 1;
            }
        }

        // Modify vertices to be centered at local zero
        Vector3 centerOffset = new Vector3(0, 0, sphereRadius);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= centerOffset;
        }

        projectionMesh.vertices = vertices;
        projectionMesh.triangles = triangles;
        projectionMesh.uv = uvs;
        projectionMesh.RecalculateNormals();

        meshFilter.sharedMesh = projectionMesh;
    }

    private float CalculateSphereRadius()
    {
        float fovRadians = fieldOfViewDegrees * Mathf.Deg2Rad;
        
        // Calculate sphere radius based on viewing distance and FOV
        // Using the formula: R = d / cos(FOV/2)
        // This ensures the surface curves properly while maintaining the correct viewing distance
        return viewingDistanceMeters / Mathf.Cos(fovRadians * 0.5f);
    }

    private float CalculateDistanceCompensation()
    {
        float fovRadians = fieldOfViewDegrees * Mathf.Deg2Rad;
        float sphereRadius = CalculateSphereRadius();
        
        // Calculate the actual distance to the edge point on the curved surface
        float edgeAngle = fovRadians * 0.5f;
        float curvedEdgeDistance = sphereRadius * Mathf.Cos(edgeAngle);
        
        // Calculate the desired distance based on the target FOV
        float desiredEdgeDistance = viewingDistanceMeters / Mathf.Cos(edgeAngle);
        
        // Return the additional distance needed
        return desiredEdgeDistance - curvedEdgeDistance;
    }

    private Vector3 CalculateViewerPosition()
    {
        if (viewerTransform == null)
            return new Vector3(0, 0, -viewingDistanceMeters);
            
        return viewerTransform.position;
    }

    private Quaternion CalculateViewerRotation()
    {
        if (viewerTransform == null)
            return Quaternion.identity;
            
        return viewerTransform.rotation;
    }

    public void OnDrawGizmos()
    {
        Vector3 viewerPos = CalculateViewerPosition();
        Quaternion viewerRot = CalculateViewerRotation();
        bool flipDirection = fieldOfViewDegrees > 180f;

        // Draw viewer position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(viewerPos, 0.1f);
        
        float halfFov = fieldOfViewDegrees * 0.5f;
        float totalDistance = viewingDistanceMeters + compensation;

        // Apply the flip rotation if needed
        Quaternion finalRotation = flipDirection ? 
            viewerRot * Quaternion.Euler(0, 180f, 0) : 
            viewerRot;

        // Draw uncompensated viewing angles (yellow)
        Gizmos.color = Color.yellow;
        Vector3 leftRay = finalRotation * Quaternion.Euler(0, -halfFov, 0) * Vector3.forward * viewingDistanceMeters;
        Vector3 rightRay = finalRotation * Quaternion.Euler(0, halfFov, 0) * Vector3.forward * viewingDistanceMeters;
        Vector3 centerRay = finalRotation * Vector3.forward * viewingDistanceMeters;
        
        // Draw compensated viewing angles (cyan)
        Gizmos.color = Color.cyan;
        Vector3 leftRayComp = finalRotation * Quaternion.Euler(0, -halfFov, 0) * Vector3.forward * totalDistance;
        Vector3 rightRayComp = finalRotation * Quaternion.Euler(0, halfFov, 0) * Vector3.forward * totalDistance;
        Vector3 centerRayComp = finalRotation * Vector3.forward * totalDistance;
        
        Gizmos.DrawRay(viewerPos, leftRayComp);
        Gizmos.DrawRay(viewerPos, rightRayComp);
        Gizmos.DrawRay(viewerPos, centerRayComp);
        
        // Draw uncompensated intersection points (red)
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(viewerPos + leftRay, 0.05f);
        Gizmos.DrawSphere(viewerPos + rightRay, 0.05f);
        Gizmos.DrawSphere(viewerPos + centerRay, 0.05f);

        // Draw compensated intersection points (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(viewerPos + leftRayComp, 0.05f);
        Gizmos.DrawSphere(viewerPos + rightRayComp, 0.05f);
        Gizmos.DrawSphere(viewerPos + centerRayComp, 0.05f);
    }

    private void SetupMaterial()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null || renderTexture == null || projectionMaterial == null) return;

        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // In editor, use the projection material directly
            renderer.sharedMaterial = projectionMaterial;
            renderer.sharedMaterial.mainTexture = renderTexture;
            return;
        }
        #endif

        // In play mode, create a unique instance
        Material materialInstance = new Material(projectionMaterial);
        materialInstance.mainTexture = renderTexture;
        renderer.material = materialInstance;
    }
}
