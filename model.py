import socket
import joblib
import numpy as np

# Load your trained AI model
model = joblib.load("turbulence_model.pkl")

# Set up TCP Server (Python listens on port 5005)
HOST = '127.0.0.1'
PORT = 5005

server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.bind((HOST, PORT))
server.listen(1)

print("AI Model server started. Waiting for Unity...")

conn, addr = server.accept()
print(f"Unity connected from {addr}")

while True:
    try:
        data = conn.recv(1024).decode().strip()
        if not data:
            continue

        print(f"Received from Unity: {data}")
        parts = data.split(',')
        if len(parts) != 5:
            print("Invalid input format.")
            continue

        features = np.array([float(x) for x in parts]).reshape(1, -1)
        prediction = int(model.predict(features)[0])
        print(f"Predicted: {prediction}")

        conn.sendall(f"{prediction}\n".encode())

    except Exception as e:
        print(f"Error: {e}")
        break

conn.close()
