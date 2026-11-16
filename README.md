# Acrofobia VR: prototipo de desensibilización sistemática

Prototipo académico de la ESPE (*Desarrollo de Software Global Aplicado a la Interculturalidad*) para exponer **gradualmente** a una persona con miedo a las alturas, y avanzar solo cuando su ansiedad baja. La ansiedad se mide con la **escala SUDS** (0 = totalmente tranquilo, 100 = el peor miedo de su vida).

> **Prototipo académico. No es una herramienta clínica validada.**

**¿Solo quieres probarlo?** Mira [Cómo iniciar la aplicación](#cómo-iniciar-la-aplicación).

![Puente colgante sobre el cañón](docs/screenshots/canyon_center.jpg)

| Prado (lugar seguro) | Mirar abajo (nivel 4) |
|---|---|
| ![Prado](docs/screenshots/meadow_pond.jpg) | ![Tabla faltante](docs/screenshots/canyon_look_down.jpg) |
| **Escala SUDS** | **Panel del operador (F1)** |
| ![Panel SUDS](docs/screenshots/ui_suds.jpg) | ![Panel F1](docs/screenshots/ui_operator_panel.jpg) |

## Qué incluye

| Pieza | Descripción |
|---|---|
| **App inmersiva** (`unity/AcrofobiaVR`) | Unity 6 (URP). Un puente colgante sobre un cañón de ~200 m con 4 niveles, 5 estímulos ajustables en vivo, escala SUDS, botón de calma, prado de descanso y tutorial. Por ahora se usa con mouse y teclado; está preparada para pasar a Meta Quest 3. |
| **Consola del terapeuta** (`server/public`) | Web, sin framework: niveles, estímulos, botón de calma, gráfica SUDS en vivo con el nivel de fondo, sugerencias, registro de eventos, resumen y exportación CSV. |
| **Servidor** (`server`) | Node.js + Express + WebSockets + SQLite. Une ambas partes y guarda las sesiones. |

Todo lo de Unity (escenas, terreno, puente, materiales, UI y configuración) **se genera por código**: no hay pasos manuales en el editor.

## Cómo iniciar la aplicación

Hay dos partes que se inician por separado: el **servidor con la consola del terapeuta** y la **app del paciente**. El orden no importa: si la app abre primero, se conecta sola cuando el servidor arranca.

### Requisitos

| Para… | Necesitas |
|---|---|
| La app del paciente (opción rápida) | Windows 10/11 de 64 bits. **No hace falta Unity.** |
| El servidor y la consola | [Node.js](https://nodejs.org/) 20 o superior (recomendado: la versión LTS más reciente) y Git |
| Abrir o modificar el proyecto de Unity | Unity Hub con **Unity 6000.6.0f1** y *Windows Build Support* |

### Paso 1. Descargar el proyecto

```bash
git clone https://github.com/FloresCaetano/acrofobia-vr.git
cd acrofobia-vr
```

### Paso 2. Iniciar el servidor y la consola del terapeuta

```bash
cd server
npm install
npm start
```

- `npm install` solo hace falta la primera vez.
- En la terminal debe aparecer `Acrofobia VR · servidor listo en el puerto 8080`.
- Abre **http://localhost:8080** en el navegador: esa es la consola del terapeuta.
- La terminal también muestra la dirección para entrar desde otra PC de la misma red (por ejemplo, `http://192.168.1.20:8080`).
- Para detener el servidor, pulsa `Ctrl + C`. Las sesiones quedan guardadas en `server/data/acrofobia.db`.

### Paso 3. Iniciar la app del paciente

**Opción A: ejecutable listo (recomendada para la demo)**

1. Descarga **`AcrofobiaVR-Windows.zip`** desde la sección [Releases](https://github.com/FloresCaetano/acrofobia-vr/releases/latest) del repositorio.
2. Descomprímelo en cualquier carpeta y ejecuta **`AcrofobiaVR.exe`**.
3. Si Windows SmartScreen muestra un aviso, elige **Más información → Ejecutar de todas formas**: el ejecutable no está firmado.

**Opción B: desde el editor de Unity**

1. En Unity Hub: **Add → Add project from disk** y elige la carpeta `unity/AcrofobiaVR`.
2. Abre la escena `Assets/Scenes/Boot.unity` y pulsa **Play**.
   (También puedes dar Play en `Neutral` o `Acrophobia`: la app carga primero `Boot` sola.)

**Opción C: generar tu propio ejecutable**, con el editor de Unity cerrado:

```powershell
./tools/unity-batch.ps1 -Method Acrofobia.Editor.Build.PlayerBuilder.BuildWindowsBatch
```

El resultado queda en `builds/Windows/AcrofobiaVR.exe`. Desde el editor, es el menú **Acrofobia > Build de Windows**.

### Paso 4. Comprobar que todo está conectado

1. En la consola, la insignia de arriba a la derecha debe decir **"VR conectada"**. En la app, la esquina superior derecha dice **"Conectado"**.
2. Escribe un alias para el paciente (nunca el nombre real) y pulsa **Iniciar sesión**.
3. Pulsa **Nivel 1**: la app pasa del prado al cañón con un fundido.

### Si el servidor está en otra computadora

Inicia la app indicando la dirección del servidor:

```powershell
AcrofobiaVR.exe -server ws://192.168.1.20:8080
```

En la PC del servidor, permite a Node.js en el Firewall de Windows (redes privadas) la primera vez que lo pida.

### Sin servidor

La app funciona igual sin servidor. Pulsa **F1** para abrir el panel del operador, donde puedes cambiar de nivel (teclas 1–4), mover los estímulos y pedir la escala SUDS. Cuando el servidor vuelve, la app se reconecta sola y envía las respuestas SUDS que quedaron pendientes.

### Probar la consola sin abrir Unity

Con el servidor ya iniciado, en otra terminal:

```bash
cd server
npm run fake-vr
```

`tools/fake-vr.js` simula la app: acepta todos los comandos y responde SUDS que bajan con el tiempo. Necesita Node.js 22 o superior.

### Problemas comunes

| Síntoma | Solución |
|---|---|
| `EADDRINUSE` al ejecutar `npm start` | El puerto 8080 está ocupado. Copia `server/.env.example` como `server/.env` y cambia `PORT`. Luego inicia la app con `-server ws://localhost:NUEVO_PUERTO`. |
| La consola dice "VR desconectada" | Revisa que la app esté abierta y que la dirección de `-server` sea correcta. Si hay dos copias de la app abiertas, solo una puede estar conectada a la vez. |
| `npm install` falla al compilar `better-sqlite3` | Usa una versión LTS de Node.js (20, 22 o 24): para esas versiones hay binarios ya compilados. |
| La consola no carga la gráfica | Ejecuta `npm install` dentro de `server`: Chart.js se sirve desde `node_modules`. |

### Regenerar las escenas de Unity (solo si modificas el builder)

Todas las escenas, materiales y configuraciones se generan por código. Usa el menú **Acrofobia > Construir escenas** o, con el editor cerrado:

```powershell
./tools/unity-batch.ps1
```

## Controles (paciente)

| Entrada | Acción |
|---|---|
| Mouse | Mirar |
| W / S | Avanzar o retroceder, despacio, solo dentro de la zona del nivel |
| **C** | **Botón de calma**: vuelve al prado desde cualquier estado |
| Rueda / flechas · clic / Enter | Ajustar y confirmar la escala SUDS |
| Esc | Pausa: balanceo de cámara, sensibilidad y salir |
| **F1** | Panel del operador: niveles (teclas 1–4 con el panel abierto), estímulos, pedir SUDS y estado de la conexión |

## Guion de la demo (3 minutos)

| Tiempo | Qué hacer | Qué mostrar |
|---|---|---|
| 0:00 | `npm start`, abrir la consola y luego la app. | Insignia **"VR conectada"**. En la app, el aviso académico en el prado. |
| 0:20 | En la app, pulsar Enter para recorrer el tutorial y practicar la escala SUDS. | Panel SUDS con número, palabra, color y carita. |
| 0:45 | En la consola, escribir el alias "Demo" y pulsar **Iniciar sesión**. Luego, **Nivel 1**. | Fundido al cañón: el paciente está en tierra firme, mirando el puente. |
| 1:00 | **Nivel 2** y luego **Nivel 3**. Subir **Viento** y **Balanceo**. | El puente se mece y el viento sopla; el cambio es suave y tarda menos de 1 s en empezar. |
| 1:30 | Esperar la pregunta automática (30 s) o pulsar **Pedir SUDS ahora**. Responder en la app. | El punto aparece en la gráfica, con el nivel de fondo. |
| 2:00 | **Nivel 4** y mirar hacia abajo. | Viento y balanceo fuertes, y la **tabla faltante** bajo los pies. Si el SUDS es ≥ 70, la consola sugiere bajar la intensidad. |
| 2:20 | Pulsar **C** en la app (o **Botón de calma** en la consola). | Fundido al prado, mensaje "Estás a salvo" y guía de respiración. |
| 2:40 | Cerrar el servidor (Ctrl+C) y mostrar que la app sigue funcionando con **F1**. Volver a ejecutar `npm start`. | La app se reconecta sola. |
| 2:50 | **Terminar sesión** y luego **Exportar CSV**. | Resumen por nivel y archivo CSV con SUDS y eventos. |

## Pruebas

```powershell
cd server; npm test                                   # servidor: 4 pruebas de integración
./tools/unity-batch.ps1 -Tests                        # Unity EditMode: 28 pruebas
./tools/unity-batch.ps1 -Tests -Platform PlayMode     # Unity PlayMode: 5 pruebas del flujo completo
```

Las capturas de `docs/screenshots` también se generan por código: `Acrofobia > Capturas de referencia` para los entornos y la prueba explícita `UiCaptureTests` para la UI en ejecución.

## Documentación

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): arquitectura, diagramas y cómo pasar a VR.
- [`docs/PROTOCOL.md`](docs/PROTOCOL.md): protocolo de mensajes y API.
- [`docs/DECISIONS.md`](docs/DECISIONS.md): decisiones tomadas y sus motivos.
- [`docs/CREDITS.md`](docs/CREDITS.md): recursos externos y sus licencias (todos CC0 u OFL).

## Privacidad

La consola pide solo un **alias** del paciente: nunca escribas el nombre real. Los datos quedan en `server/data/acrofobia.db`, que Git ignora.

## Referencias

- Rothbaum, B. O., Hodges, L. F., Kooper, R., Opdyke, D., Williford, J. S., & North, M. (1995). Effectiveness of computer-generated (virtual reality) graded exposure in the treatment of acrophobia. *American Journal of Psychiatry, 152*(4), 626–628.
- Wolpe, J. (1969). *The Practice of Behavior Therapy*. Pergamon Press (escala SUDS).
