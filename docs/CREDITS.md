# Créditos y licencias de recursos externos

Las texturas y los HDRI son **CC0 (dominio público)**: se pueden usar, modificar y redistribuir sin atribución obligatoria. La fuente usa la licencia **SIL Open Font License 1.1**, que permite incluirla en el proyecto si se acompaña de su licencia. Igual registramos todo aquí por transparencia.

| Recurso | Uso en el proyecto | Fuente | Licencia |
|---|---|---|---|
| `kloofendal_48d_partly_cloudy_puresky` (HDRI 4k) | Cielo y reflejos de la escena del cañón | [Poly Haven](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky) — Greg Zaal, Jarod Guest | CC0 |
| `kloppenheim_06_puresky` (HDRI 4k) | Cielo de la escena neutral (prado) | [Poly Haven](https://polyhaven.com/a/kloppenheim_06_puresky) — Greg Zaal | CC0 |
| `rock_face_03` (difuso + normal, 2k) | Paredes del cañón y rocas | [Poly Haven](https://polyhaven.com/a/rock_face_03) | CC0 |
| `aerial_grass_rock` (difuso + normal, 2k) | Suelo de la meseta | [Poly Haven](https://polyhaven.com/a/aerial_grass_rock) | CC0 |
| `leafy_grass` (difuso + normal, 2k) | Suelo del prado | [Poly Haven](https://polyhaven.com/a/leafy_grass) | CC0 |
| `dry_riverbed_rock` (difuso + normal, 2k) | Fondo del cañón (lecho del río) | [Poly Haven](https://polyhaven.com/a/dry_riverbed_rock) | CC0 |
| `rough_wood` (difuso + normal, 1k) | Tablas y postes del puente | [Poly Haven](https://polyhaven.com/a/rough_wood) | CC0 |
| **Atkinson Hyperlegible** (Regular y Bold) | Toda la UI de Unity y de la consola web | [Braille Institute](https://www.brailleinstitute.org/freefont/) vía [Google Fonts](https://github.com/google/fonts/tree/main/ofl/atkinsonhyperlegible) | OFL 1.1 (`OFL.txt` junto a cada copia) |
| Chart.js 4 | Gráfica de la consola | [chartjs.org](https://www.chartjs.org/) (npm) | MIT |
| TextMesh Pro Essential Resources | Shaders y ajustes de TMP | Paquete `com.unity.ugui` de Unity | Unity Companion License |

## Recursos generados por código

No tienen licencia de terceros: los genera el builder del proyecto.

- Terreno del cañón y del prado, río, rocas y pinos (mallas procedurales).
- Textura del agua (mapa normal de ondas), textura de cuerda y partícula de polvo.
- Audio de viento y ambiente (síntesis en tiempo real con `OnAudioFilterRead`).
