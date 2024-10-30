using System.Collections;
using System.Collections.Generic;
using Dojo;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed for smooth movement between tiles
    public Vector2Int gridSize = new Vector2Int(32, 32); // Tile size in pixels
    private bool isMoving = false; // Check if movement is in progress
    private Vector3 targetPosition; // Target position for each tile

    private void Start()
    {
        // Set initial position to nearest grid point
        targetPosition = transform.position;
    }

    private void Update()
    {
        // Move towards the target position
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                // Snap to position and stop moving
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    public void Move(int direction)
    {
        if (isMoving) { return; }
        switch (direction)
        {
            case 0: 
                targetPosition = transform.position + Vector3.up;
                break;
            case 1: 
                targetPosition = transform.position + Vector3.right;
                break;
            case 2: 
                targetPosition = transform.position + Vector3.left;
                break;
            case 3: 
                targetPosition = transform.position + Vector3.down;
                break;
            default:
                break;
        }
        isMoving = true;
    }
}

