# Acrofobia VR — prototipo de desensibilización sistemática

Prototipo académico (ESPE, *Desarrollo de Software Global Aplicado a la Interculturalidad*) para la exposición gradual a las alturas.

> **Prototipo académico. No es una herramienta clínica validada.**

El sistema tiene tres piezas:

1. **Aplicación inmersiva (Unity 6, URP)**: un puente colgante sobre un cañón, con cuatro niveles de intensidad. Por ahora se controla con mouse y teclado; más adelante se usará con Meta Quest 3.
2. **Consola web del terapeuta**: controla los estímulos en tiempo real y grafica las respuestas SUDS.
3. **Servidor Node.js**: conecta ambas partes por WebSockets y guarda las sesiones en SQLite.

## Estructura

```
unity/AcrofobiaVR/   Proyecto Unity (URP)
server/              Servidor Node.js (WebSockets, API, SQLite) y consola en server/public
tools/               Scripts auxiliares (Unity en modo batch, cliente VR falso)
docs/                Protocolo, arquitectura, decisiones y créditos
```

## Requisitos

- Unity **6000.6.0f1** (Unity Hub) con *Windows Build Support*
- Node.js **20 o superior** (probado con 24.16)

## Reconstruir las escenas de Unity

Todas las escenas, materiales y configuraciones se generan por código. Con el editor **cerrado**:

```powershell
./tools/unity-batch.ps1
```

O, con el editor abierto, desde el menú **Acrofobia > Construir escenas**.

*(Este README se completa en el hito H7 con el guion de la demo.)*
