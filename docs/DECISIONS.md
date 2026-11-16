# Registro de decisiones

Decisiones tomadas durante el desarrollo del prototipo, con su motivo. Las más recientes van al final.

| # | Decisión | Motivo |
|---|---|---|
| 1 | El proyecto de Unity se creó en `C:\Users\caeta\AcrofobiaVR` y se **copió** (sin `Temp/` ni `Logs/`) a `unity/AcrofobiaVR`. La carpeta original no se tocó. | El repositorio exige esa ruta y copiar es reversible. La copia original se puede borrar cuando se quiera. |
| 2 | Versión de Unity: **6000.6.0f1** con URP 17.6 e Input System 1.20 (solo Input System activo). | Es la versión instalada y la plantilla Universal 3D la trae configurada. |
| 3 | Se eliminaron los recursos de ejemplo de la plantilla (`TutorialInfo`, `Readme`, `SampleScene`, `HubForceResolve.cs`). | Evitan ruido en el repositorio. El builder genera todas las escenas. |
| 4 | Todas las escenas y assets se generan desde `Acrofobia > Construir escenas` (`ProjectBuilder`). Los assets existentes se actualizan en sitio para conservar su GUID. | El builder es idempotente y las referencias entre assets no se rompen al reconstruir. |
| 5 | `tools/unity-batch.ps1` envuelve la línea de comandos de Unity en modo batch. | Un único comando documentado para reconstruir, compilar y probar. |
| 6 | Servidor con Node 24 (compatible con Node 20+), Express 5, `ws` y `better-sqlite3`. La instalación nativa de SQLite funcionó en Windows, así que no hace falta el respaldo con JSON. | Lo pide el enunciado y se verificó en esta máquina. |
| 7 | `tools/fake-vr.js` usa el `WebSocket` nativo de Node (disponible desde Node 22) y no tiene dependencias. | Se puede ejecutar desde cualquier carpeta sin `npm install`. |
| 8 | Texturas y HDRI CC0 de Poly Haven, guardados en `Assets/Art/External/PolyHaven` y registrados en `docs/CREDITS.md`. Se versionan en Git normal, sin LFS (~70 MB). | Quien clone el repositorio no necesita configurar LFS ni gastar su cuota de ancho de banda. |
| 9 | Las escenas de entorno (`Neutral`, `Acrophobia`) no tienen cámara. El jugador, la cámara y los managers viven en `Boot`, y cada entorno expone un `EnvironmentRoot` con su `IWalkPath`. | Un solo rig persistente, y el futuro XR Origin reemplaza solo ese rig. Las escenas no dependen de los managers. |
| 10 | El movimiento es un "riel": el jugador avanza en una dimensión sobre el camino del nivel (orilla, puente, orilla). No hay colisiones ni física. | Es estable para la demo y cumple las reglas de comodidad de VR (sin correr, sin saltar, rango limitado por nivel). |
| 11 | El puente es cinemático: el balanceo usa un seno más ruido de baja frecuencia, y la catenaria se resuelve por bisección. Las tablas y cuerdas se reposicionan cada frame. | Sin física no hay explosiones de joints. El paciente "viaja" sobre la misma función que dibuja el puente, así que nunca se desincroniza. |
| 12 | El fondo del cañón es una franja de malla que sigue el cauce y es un poco más ancha que la garganta. La meseta nunca baja de 320 m. | Así, aunque el estímulo de altura suba el fondo, este nunca atraviesa el suelo por fuera del cañón. |
| 13 | Iluminación 100 % en tiempo real: luz ambiental Trilight y reflejos con el cubemap del HDRI (`customReflectionTexture`). No hay bake. | El builder es rápido e idempotente, y no hay datos de iluminación binarios que versionar. |
| 14 | Los ScriptableObjects de datos (`StimulusSettings` y luego los niveles) se crean con valores por defecto **solo si no existen**. | Lo que se ajuste en el Inspector sobrevive a cada reconstrucción, y agregar o ajustar niveles no requiere tocar código. |
| 15 | El builder obtiene las librerías de assets dentro de cada escena. | Al abrir una escena Unity descarga los assets sin uso, y las referencias obtenidas antes quedarían apuntando a objetos destruidos. |
| 16 | `Acrofobia > Capturas de referencia` (y `SceneCapture.BuildAndCaptureBatch` en batch) renderiza capturas a `docs/screenshots` sin entrar en Play. | Permite revisar la parte visual desde la línea de comandos y deja material para la presentación. |
