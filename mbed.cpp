#include "mbed.h"
#include "LCDi2c.h"

// UART from Unity
BufferedSerial pc(USBTX, USBRX, 9600);  // K66 USB virtual COM

// Motor control pins - H-bridge
DigitalOut in1(PTD0);     // IN1
DigitalOut in2(PTB19);    // IN2

// I2C LCD
LCDi2c lcd(PTC11, PTC10, LCD16x2, 0x27);  // SDA, SCL, Type, Address

char recv_char;
int turbulence_level = 0;

void lcd_write_line(int row, const char* text) {
    lcd.locate(0, row);
    lcd.puts(text);
}

void control_motor(int level) {
    lcd.cls();

    // Show turbulence level
    char level_str[16];
    snprintf(level_str, sizeof(level_str), "Turbulence: %d", level);
    lcd_write_line(0, level_str);

    if (level == 1) {
        // Forward spin
        lcd_write_line(1, "Fan: FORWARD");
        in1 = 1;
        in2 = 0;
    }
    else if (level == 2) {
        // Reverse spin
        lcd_write_line(1, "Fan: REVERSE");
        in1 = 0;
        in2 = 1;
    }
    else {
        // Stop
        lcd_write_line(1, "Fan: OFF");
        in1 = 0;
        in2 = 0;
    }
}

int main() {
    pc.set_format(8, BufferedSerial::None, 1);
    lcd.cls();
    lcd_write_line(0, "Awaiting Unity");
    lcd_write_line(1, "Turbulence lvl");

    while (true) {
        if (pc.readable()) {
            pc.read(&recv_char, 1);

            if (recv_char >= '0' && recv_char <= '3') {
                turbulence_level = recv_char - '0';
                control_motor(turbulence_level);
            }
            else {
                lcd.cls();
                lcd_write_line(0, "Invalid input:");
                char buf[16];
                snprintf(buf, sizeof(buf), "Char: %c", recv_char);
                lcd_write_line(1, buf);
                in1 = 0;
                in2 = 0;
            }
        }

        thread_sleep_for(100);
    }
}