using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Transform secondaryTarget;

    public float minX, maxX;
    public float followY = 0f;

    public bool followPlayer = true;

    void LateUpdate()
    {
        if (!followPlayer || player == null) return;

        float targetX = player.position.x;

        if (secondaryTarget != null)
        {
            targetX = (targetX + secondaryTarget.position.x) * 0.5f;
        }

        if (maxX > minX)
        {
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }

        transform.position = new Vector3(
            targetX,
            followY,
            -10
        );
    }
}


