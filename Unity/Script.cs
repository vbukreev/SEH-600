using System;
using UnityEngine;
using System.IO;  
using System.IO.Ports;
using System.Collections;
using System.Net.Sockets;
using System.Text;

public class SerialController : MonoBehaviour
{
    private SerialPort frdmPort;     // COM3 for FRDM manual control
    
    private Rigidbody rb;
    private int aiPrediction = 0;
    private volatile char lastReceivedCommand = '\0';
    
    private TcpClient aiClient;
    private NetworkStream aiStream;

    private string logFilePath;

    private string currentDirection = "None";

    void Start()
    {
        try
        {
            frdmPort = new SerialPort("COM3", 9600);
            frdmPort.Open();
            Debug.Log("FRDM (COM3) opened!");


            aiClient = new TcpClient("127.0.0.1", 5005);
            aiStream = aiClient.GetStream();
            StartCoroutine(ConnectToAI());

            Debug.Log("Connected to AI via TCP!");

            rb = GetComponent<Rigidbody>();

            frdmPort.DiscardInBuffer();
            Debug.Log("All serial ports opened successfully");

            rb = GetComponent<Rigidbody>();

            logFilePath = Application.dataPath + "/turbulence_log.csv";
            if (!File.Exists(logFilePath))
            {
                File.AppendAllText(logFilePath, "DistanceToObstacle,Velocity,ProximityLeft,ProximityRight,Altitude,Class\n");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to open serial ports: {e.Message}");
        }
    }

    void Update()
    {
        // Receive command from FRDM
        if (frdmPort != null && frdmPort.IsOpen)
        {
            try
            {
                string data = frdmPort.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    foreach (char c in data)
                    {
                        if ("FLRB".Contains(c.ToString()))
                        {
                            lastReceivedCommand = c;
                            Debug.Log($"FRDM Command: {c}");
                        }
                    }
                }
            }
            catch { }
        }

        // Read AI prediction (if available)
        if (aiStream != null && aiStream.DataAvailable)
        {
            byte[] buffer = new byte[128];
            int count = aiStream.Read(buffer, 0, buffer.Length);
            string result = Encoding.ASCII.GetString(buffer, 0, count).Trim();

            if (int.TryParse(result, out int pred))
            {
                aiPrediction = pred;
                Debug.Log($"AI Prediction Received: {aiPrediction}");
            }
        }

        // Send latest flight data to AI
        SendFlightDataToAI();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 move = Vector3.zero;

        // AI: override at turbulence level 3
        if (aiPrediction == 3)
        {
            Vector3 windPush = -transform.forward * UnityEngine.Random.Range(0.03f, 0.07f);
            windPush += Vector3.right * UnityEngine.Random.Range(-0.02f, 0.02f);
            rb.AddForce(windPush, ForceMode.VelocityChange);
            currentDirection = "Evasive";
            Debug.Log("Severe turbulence: Drone pushed backward!");
            return;
        }

        // AI: calm air override
        if (aiPrediction == 0)
        {
            move = Vector3.forward;
            currentDirection = "Forward";
            Debug.Log("Calm air: Returning to forward flight.");
        }
        else
        {
            // Manual command parsing for mild/moderate wind
            switch (lastReceivedCommand)
            {
                case 'F': move = Vector3.forward; currentDirection = "Forward"; break;
                case 'L': move = Vector3.left;    currentDirection = "Left";    break;
                case 'R': move = Vector3.right;   currentDirection = "Right";   break;
                case 'B': move = Vector3.back;    currentDirection = "Backward";break;
                default:
                    rb.linearVelocity *= 0.9f;
                    currentDirection = "None";
                    return;
            }
        }

        // Apply force with AI-based multiplier
        float speed = GetAISpeedMultiplier(aiPrediction);
        rb.AddForce(move * speed, ForceMode.VelocityChange);
    }




    void SendFlightDataToAI()
    {
        if (rb == null) return;

        float v = rb.linearVelocity.magnitude;
        float alt = transform.position.y;
        float d = UnityEngine.Random.Range(30f, 150f);
        float left = UnityEngine.Random.Range(10f, 100f);
        float right = UnityEngine.Random.Range(10f, 100f);

        string msg = $"{d:F2},{v:F2},{left:F2},{right:F2},{alt:F2}\n";

        // Send to AI only if connected
        if (aiStream != null && aiStream.CanWrite)
        {
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(msg);
                aiStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to send data to AI: {e.Message}");
            }
        }

        // Log the flight data even without AI
        if (!File.Exists(logFilePath))
        {
            File.AppendAllText(logFilePath, "DistanceToObstacle,Velocity,ProximityLeft,ProximityRight,Altitude,Class,Direction\n");

        }

        string fullLine = $"{msg.Trim()},{aiPrediction},{currentDirection}";
        File.AppendAllText(logFilePath, fullLine + "\n");
    }


    float GetAISpeedMultiplier(int prediction)
    {
        switch (prediction)
        {
            case 0: return 0.05f;   // Normal
            case 1: return 0.035f;  // Slower
            case 2: return 0.02f;   // Very slow
            default: return 0.05f;
        }
    }

    

    void EvasiveManeuver()
    {
        Vector3 dodge = Vector3.right * UnityEngine.Random.Range(-2f, 2f);
        rb.AddForce(dodge, ForceMode.Impulse);
        Debug.Log("AI override:maneuvering away");
    }

    void OnApplicationQuit()
    {
        if (frdmPort != null && frdmPort.IsOpen) frdmPort.Close();
        if (aiStream != null) aiStream.Close();
        if (aiClient != null) aiClient.Close();
    }

    private IEnumerator ConnectToAI()
    {
        int attempts = 0;
        while (attempts < 5 && (aiClient == null || !aiClient.Connected))
        {
            try
            {
                aiClient = new TcpClient("127.0.0.1", 5005);
                aiStream = aiClient.GetStream();
                Debug.Log("Connected to AI via TCP!");
            }
            catch
            {
                Debug.Log("Waiting for AI model to be ready...");
                attempts++;
            }

            yield return new WaitForSeconds(2f);
        }

        if (aiClient == null || !aiClient.Connected)
            Debug.LogError(" Could not connect to AI server after 5 attempts.");
    }
}