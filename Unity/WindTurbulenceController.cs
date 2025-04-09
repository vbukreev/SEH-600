using UnityEngine;

public class WindTurbulenceController : MonoBehaviour
{
    public WindZone windZone;             // Visual wind
    public Rigidbody droneRb;            // Drone's Rigidbody
    public Transform droneTransform;     // Drone’s forward direction

    public int maxLevels = 3;            // Total levels (e.g., 0 → 3)
    public float maxWindMain = 5f;       // Max wind visual strength
    public float maxTurbulence = 3f;     // Max visual turbulence
    public float maxWindForce = 12f;     // Max physics push force

    private int currentLevel = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Cycle through wind levels
            currentLevel = (currentLevel + 1) % (maxLevels + 1);

            float windMain = Mathf.Lerp(0f, maxWindMain, currentLevel / (float)maxLevels);
            float turbulence = Mathf.Lerp(0f, maxTurbulence, currentLevel / (float)maxLevels);

            windZone.windMain = windMain;
            windZone.windTurbulence = turbulence;

            Debug.Log($"🌬️ Wind Level: {currentLevel} | Main: {windMain:F1}, Turbulence: {turbulence:F1}");
        }
    }

    void FixedUpdate()
    {
        if (currentLevel > 0)
        {
            // Apply wind force pushing against the drone's forward direction
            Vector3 windDir = -droneTransform.forward;
            float windStrength = Mathf.Lerp(0f, maxWindForce, currentLevel / (float)maxLevels);
            droneRb.AddForce(windDir * windStrength, ForceMode.Force);
        }
    }
}
