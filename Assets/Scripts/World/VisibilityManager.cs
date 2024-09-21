using System.Collections.Generic;
using UnityEngine;

public class VisibilityManager : MonoBehaviour
{
    public float checkInterval = 0.5f;  // Time interval between visibility checks

    private List<GameObject> objectsToTrack = new List<GameObject>();
    private Camera _mainCamera;
    private float _timeSinceLastCheck = 0f;

    private void Start()
    {
        // Automatically find the main camera in the scene
        _mainCamera = Camera.main;

        if (_mainCamera == null)
        {
            Debug.LogWarning("No camera tagged as 'MainCamera' found. Please tag the main camera.");
        }
    }

    private void Update()
    {
        if (_mainCamera == null) return;  // Do nothing if no camera is found

        _timeSinceLastCheck += Time.deltaTime;

        if (_timeSinceLastCheck >= checkInterval)
        {
            _timeSinceLastCheck = 0f;

            // Check visibility of each object
            foreach (var obj in objectsToTrack)
            {
                MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                {
                    if (IsVisibleFrom(meshRenderer, _mainCamera))
                    {
                        if (!obj.activeSelf)
                        {
                            obj.SetActive(true);
                        }
                    }
                    else
                    {
                        if (obj.activeSelf)
                        {
                            obj.SetActive(false);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"GameObject '{obj.name}' does not have a MeshRenderer component.");
                }
            }
        }
    }

    /// <summary>
    /// Add object to track for visibility.
    /// </summary>
    public void AddObjectToTrack(GameObject obj)
    {
        if (!objectsToTrack.Contains(obj))
        {
            objectsToTrack.Add(obj);
        }
    }

    /// <summary>
    /// Check if the MeshRenderer is visible from the camera.
    /// </summary>
    private bool IsVisibleFrom(MeshRenderer renderer, Camera cam)
    {
        return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), renderer.bounds);
    }
}
