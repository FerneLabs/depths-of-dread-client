using System.Collections;
using System.Collections.Generic;
using Dojo;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed for smooth movement between tiles
    Vector3 initialPositionOffset;
    Vector3 targetPosition; // Target position for each tile
    bool isMoving = false; // Check if movement is in progress

    private void Start()
    {
        initialPositionOffset = transform.localPosition;
    }

    private void Update()
    {
        // Move towards the target position
        if (isMoving)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, targetPosition) < 0.01f)
            {
                // Snap to position and stop moving
                transform.localPosition = targetPosition;
                isMoving = false;
            }
        }
    }

    public void Move(Vector3 position)
    {
        if (isMoving) { return; }
        targetPosition = position + initialPositionOffset;
        isMoving = true;
    }
}

