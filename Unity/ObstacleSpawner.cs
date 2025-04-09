using UnityEngine;
using System.Collections.Generic;

public class TreeSpawner : MonoBehaviour
{
    public GameObject treePrefab;
    public Transform player; // Assign the Drone in Unity

    public int maxTrees = 100;          // Total trees in the scene
    public float spawnRadius = 100f;    // How far ahead trees spawn
    public float despawnDistance = 60f; // Distance behind drone to remove trees
    public float spawnCooldown = 0.5f;  // Cooldown between spawns

    public float corridorWidth = 10f;   // Width of the fly-through path
    public float sideOffset = 8f;      // Distance from center to left/right trees

    private List<GameObject> activeTrees = new List<GameObject>();
    private float timeSinceLastSpawn = 0f;

    void Start()
    {
        SpawnTreeCorridor();
    }

    void Update()
    {
        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= spawnCooldown)
        {
            SpawnTreeCorridor();
            timeSinceLastSpawn = 0f;
        }

        DespawnTrees();
    }

    void SpawnTreeCorridor()
    {
        float yPos = 12.5f;  // Terrain (10) + half tree height (2.5)
        Vector3 forwardOffset = player.forward * (spawnRadius + 5f); 
        Vector3 spawnCenter = player.position + forwardOffset;

        // Left tree
        Vector3 leftPos = spawnCenter - player.right * sideOffset;
        leftPos.y = yPos;

        GameObject leftTree = Instantiate(treePrefab, leftPos, Quaternion.identity);
        leftTree.tag = "Tree";
        activeTrees.Add(leftTree);

        // Right tree
        Vector3 rightPos = spawnCenter + player.right * sideOffset;
        rightPos.y = yPos;

        GameObject rightTree = Instantiate(treePrefab, rightPos, Quaternion.identity);
        rightTree.tag = "Tree";
        activeTrees.Add(rightTree);

        Debug.Log($"🌲 Spawning at center: {spawnCenter}, L: {leftPos}, R: {rightPos}");
    }


    void DespawnTrees()
    {
        for (int i = activeTrees.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(activeTrees[i].transform.position, player.position) > despawnDistance)
            {
                Destroy(activeTrees[i]);
                activeTrees.RemoveAt(i);
            }
        }
    }
}
