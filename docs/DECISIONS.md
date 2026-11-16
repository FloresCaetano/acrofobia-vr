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
