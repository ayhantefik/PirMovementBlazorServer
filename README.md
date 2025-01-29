<h1>PirMovementBlazorServer</h1>

The project integrates a motion system prototype that notifies users in real time when a motion is detected by an infrared sensor. Users have the option to activate sound or turn on a light in response to the motion. The time of each detected motion is recorded in a database and users can list a history of recorded movements.

<h2>Hardware Requirements</h2>

1 - Arduino UNO R4 Wifi
</br>
1 - HC-SR501 Pir Motion Sensor
</br>
1 - Buzzer 100K
</br>
1 - Led

<h2>Web Application</h2>

Blazor Server

<h2>Messaging protocol between Arduino and Web</h2>

MQTT

<h2>Setup Instructions</h2>

<b>PirMovementArduino.ino</b>
</br>
Row 14: Type your IPV4
</br>
Row 15: Your MQTT broker host
</br>
</br>
<b>appsettings.json</b>
</br>
Row 10: Database connection string
</br>
Row 13: Your host url
</br>
Row 16: Your MQTT broker host
</br>
Row 17: MQTT Port
