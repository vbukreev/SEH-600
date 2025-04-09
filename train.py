import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import classification_report
import joblib

# Load and clean data
df = pd.read_csv("turbulence_log.csv", on_bad_lines='skip')  # Skip malformed lines

# Only drop "Class" column; ignore "Direction"
X = df.drop(columns=["Class"])
y = df["Class"]

# Split data
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2)

# Train model
model = RandomForestClassifier(n_estimators=100)
model.fit(X_train, y_train)

# Evaluate model
print(classification_report(y_test, model.predict(X_test)))

# Save trained model
joblib.dump(model, "turbulence_model.pkl")
print("Model trained successfully and saved as turbulence_model.pkl")
