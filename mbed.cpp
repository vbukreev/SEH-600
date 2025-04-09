#include "mbed.h"
#include "rtos.h"
#include "FXOS8700Q.h"
#include "LCDi2c.h"  // Make sure this library is included in your project

// Accelerometer (FXOS8700Q) over I2C
I2C i2cAcc(PTD9, PTD8);
FXOS8700QAccelerometer acc(i2cAcc, FXOS8700CQ_SLAVE_ADDR1);

// Serial to Unity (USB)
UnbufferedSerial pc(USBTX, USBRX, 9600);  // USB serial for Unity

// Serial from Unity AI (UART1)
UnbufferedSerial aiUART(PTC14, PTC15, 9600);  // RX, TX from AI

// LCD over I2C
LCDi2c lcd(PTC11, PTC10, LCD16x2, 0x27); // SDA, SCL, Type, Address

// Propeller via transistor
DigitalOut motorControl(PTD0);

// Shared state
char movement = '\0';
int aiPrediction = -1;
Mutex serialMutex;

void ReadAccelerometer()
{
    motion_data_units_t acc_data;

    while (true)
    {
        acc.getAxis(acc_data);
        float ax = acc_data.x;
        float ay = acc_data.y;

        serialMutex.lock();
        movement = '\0';
        if (ax < -0.3) movement = 'L';
        else if (ax > 0.3) movement = 'R';
        else if (ay < -0.3) movement = 'F';
        else if (ay > 0.3) movement = 'B';
        serialMutex.unlock();

        ThisThread::sleep_for(250ms);
    }
}

void SendToUnity()
{
    while (true)
    {
        serialMutex.lock();
        if (movement != '\0')
        {
            pc.write(&movement, 1);
            pc.write("\n", 1);
            movement = '\0';
        }
        serialMutex.unlock();
        ThisThread::sleep_for(300ms);
    }
}

void ReceiveFromAI()
{
    char c;
    static char buffer[4];
    static int index = 0;

    while (true)
    {
        if (aiUART.readable())
        {
            aiUART.read(&c, 1);
            if (c == '\n' || c == '\r')
            {
                buffer[index] = '\0';
                aiPrediction = atoi(buffer);
                index = 0;

                lcd.cls();
                lcd.locate(0, 0);
                switch (aiPrediction)
                {
                    case 0: lcd.printf("Turb: Calm"); break;
                    case 1: lcd.printf("Turb: Mild"); break;
                    case 2: lcd.printf("Turb: Mod"); break;
                    case 3: lcd.printf("Turb: Severe!"); break;
                    default: lcd.printf("Turb: Unknown"); break;
                }
            }
            else if (index < 3)
            {
                buffer[index++] = c;
            }
        }
        ThisThread::sleep_for(100ms);
    }
}

void ControlPropeller()
{
    while (true)
    {
        
        if (aiPrediction == 3) {
            motorControl = 0; 
        } else {
            motorControl = 1;
        }
    }
}

int main()
{
    acc.enable();
    lcd.cls();
    lcd.locate(0, 0);
    lcd.printf("System Booting...");
    motorControl = 1;

    Thread t1, t2, t3, t4;
    t1.start(ReadAccelerometer);
    t2.start(SendToUnity);
    t3.start(ReceiveFromAI);
    t4.start(ControlPropeller);

    while (true)
    {
        ThisThread::sleep_for(1000ms);  // Keep main thread alive
    }
}