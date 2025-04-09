using UnityEngine;

public class TerrainScroller : MonoBehaviour
{
    public Transform player; // Assign the Drone in Unity
    public float tileSize = 50f; // Size of the terrain tile
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float playerZ = player.position.z; // Track only forward movement
        float distanceMoved = playerZ - startPosition.z;

        // When the player moves forward past one tile, shift the terrain
        if (distanceMoved > tileSize)
        {
            startPosition.z += tileSize; // Move start position forward
            transform.position = new Vector3(transform.position.x, transform.position.y, startPosition.z);
        }
    }
}
