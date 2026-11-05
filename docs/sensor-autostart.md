# Sensoren beim Windows-Start

Das Panel startet über die Option **Gemeinsame Sensoren & Start → Gemeinsames Panel mit Windows starten** minimiert. Für vollständige Sensorwerte und FPS benötigt der getrennte Sensorprozess Administratorrechte.

Mit `Install-SensorAutostart.ps1` aus dem vollständig entpackten Windows-Paket lässt sich dafür einmalig eine Windows-Aufgabe einrichten. Das Skript als Administrator mit dem eigenen Windows-Administratorkonto ausführen. Anschließend das Panel neu starten. Die Windows-Bestätigung wird nur für diese Einrichtung benötigt; danach startet der Sensorprozess beim Start des Panels ohne erneute Abfrage. Es werden keine Kennwörter gespeichert.

Die Aufgabe hat keine eigene Anmeldeauslösung: Die normal gestartete Oberfläche fordert ihren Sensorprozess an. Dieser beendet sich mit der Panelsitzung. Seine Dateien liegen geschützt unter `C:\Program Files\MiraRight Panel\Sensors`. UAC bleibt aktiv. Die Aufgabe erlaubt ausschließlich den fest eingetragenen Sensorprozess; die Verbindung ist an die laufende Oberfläche gebunden.

Der Sensorprozess zeigt kein Konsolenfenster. Bei einem Verbindungsabbruch oder ausbleibenden Messwerten kann die Oberfläche den Sensorprozess über die eingerichtete Aufgabe erneut starten. Für diesen automatischen Neustart wird niemals auf eine neue UAC-Abfrage ausgewichen. Verbindungsereignisse und Datenalter stehen im lokalen `debug.log`, erreichbar über **Gemeinsame Sensoren & Start → Debug-Log öffnen**. Ein vollständiger Hardware- oder Treiberstillstand kann trotzdem einen manuellen Neustart erfordern.

Nach einem Update des Sensorprozesses das Skript aus dem neuen Paket erneut als Administrator ausführen. Die vorherige geschützte Sensorinstallation wird als `Sensors-Backup-…` aufbewahrt. Ein vorhandener Sensorprozess muss zuvor über **Beenden** im Infobereich geschlossen werden.

Zum Entfernen der Aufgabe das Skript als Administrator mit `-Remove` ausführen. Die geschützten Programmdateien bleiben zur manuellen Sicherung erhalten. Ohne eingerichtete Aufgabe fragt das Panel beim Start mit aktivierten Sensorrechten wieder nach der normalen Windows-Bestätigung. Für Windows-Standardkonten ist diese Einrichtung nicht vorgesehen.

Technische Grundlage: [Microsoft: Task Scheduler und Laufzeitparameter](https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nf-taskschd-iregisteredtask-runex).
