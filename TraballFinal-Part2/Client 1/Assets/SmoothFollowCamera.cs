using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public List<Transform> targets;
    public float minZoom = 2f;
    public float maxZoom = 5f;
    public float zoomLerpSpeed = 5f;

    private Camera mainCamera;
    private Vector3 offset;
    private Vector3 velocity;
    public float smoothTime = 0.5f;

    void Start()
    {
        mainCamera = Camera.main;
        StartCoroutine(LateStart(0.5f));
    }

    IEnumerator LateStart(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        AddTargets();
        // Calculate the initial offset as the average position of the targets
        offset = transform.position - GetCenterPoint();
    }

    void AddTargets()
    {
        targets.Add(GameObject.Find("Martial Hero(Clone)").transform);
        targets.Add(GameObject.Find("Hero Knight(Clone)").transform);
    }

    void LateUpdate()
    {
        if (targets.Count == 0)
            return;

        // Calcular el punto medio entre todos los personajes
        Vector3 centerPoint = GetCenterPoint();

        // Mover la cámara suavemente hacia el punto medio
        Vector3 targetPosition = centerPoint + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    Vector3 GetCenterPoint()
    {
        if (targets.Count == 1)
        {
            return targets[0].position;
        }

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        foreach (var target in targets)
        {
            bounds.Encapsulate(target.position);
        }

        return bounds.center;
    }
}
