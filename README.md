# Interfaz de Realidad Mixta para la teleoperación de drones por usuarios sin experiencia en pilotaje

Aplicación de Realidad Mixta para **Meta Quest** que permite planificar, simular y
supervisar el vuelo de un dron **DJI Tello** dentro del espacio físico real del
usuario. El operador coloca puntos de ruta (*waypoints*) en su propia habitación,
valida que la trayectoria es segura y, después, ejecuta la misión sobre el dron
físico mientras recibe telemetría en directo.

> Trabajo Fin de Máster · Universidad Carlos III de Madrid (UC3M) · Autor: **Jaime Provencio Solís** · 2026

---

## ¿Qué hace?

- **Calibración espacial.** Se ancla el origen de coordenadas del sistema a un
  marcador físico, de modo que el mundo virtual y el mundo real comparten un mismo
  sistema de referencia.
- **Planificación de ruta.** El usuario coloca waypoints en el aire con los mandos
  de la Quest. Cada waypoint se valida contra un *geofence* (radio y altura máximos)
  antes de aceptarse.
- **Simulación previa.** Un "dron fantasma" recorre la ruta en virtual para revisar
  la trayectoria sin riesgo antes de volar de verdad.
- **Ejecución física.** La misión se traduce a comandos del Tello y se envía por
  UDP. El sistema registra la telemetría (posición, batería, tiempos) durante el vuelo.
- **Corrección asistida.** Tras cada tramo, el operador puede reposicionar el dron
  con ayuda visual que respeta la resolución mínima de movimiento del Tello (20 cm por eje).

---

## Arquitectura

El código se organiza en capas con responsabilidad única. Cada carpeta de
`Assets/Scripts/` es una capa:

| Capa | Carpeta | Responsabilidad |
|------|---------|-----------------|
| 1 | `1_Core_Espacial` | Calibración del origen y anclaje marcador → dron. |
| 2 | `2_Planificacion_Virtual` | Waypoints, validación (geofence) y simulación de ruta. |
| 3 | `3_Interaccion_HMI` | Interacción en VR: canvas, altura, rotación, ayudas visuales. |
| 4 | `4_Maquina_Estados` | Máquina de estados del dron e internacionalización (idioma). |
| 5 | `5_Comunicaciones_Fisicas` | Enlace UDP con el Tello, telemetría, grabación y corrección. |
| 6 | `6_Entorno_MRUK` | Integración con el entorno de la Quest (Meta MR Utility Kit). |
| — | `Pruebas` | Montaje de la instalación de pruebas y modo de vuelo manual. |

El flujo general es: **calibrar → planificar y validar → simular → ejecutar sobre el dron → registrar**.

---

## Requisitos

**Software**

- [Unity **6000.4.3f1**](https://unity.com/releases/editor/archive) (Unity 6). Otras
  versiones menores pueden funcionar, pero el proyecto se ha desarrollado y probado con esta.
- Paquetes principales (se instalan solos al abrir el proyecto, vía *Package Manager*):
  - Meta XR SDK (`com.meta.xr.sdk.all` 85.0.0)
  - Universal Render Pipeline (URP 17.4)
  - XR Interaction Toolkit 3.4 · OpenXR · XR Management
  - ProBuilder

**Hardware**

- Visor **Meta Quest** (desarrollado sobre Quest con paso de vídeo / Mixed Reality).
- Dron **DJI Tello** conectado por Wi-Fi para la ejecución física (opcional: la
  planificación y la simulación funcionan sin el dron).

---

## Cómo abrir el proyecto

1. Clona el repositorio:
   ```bash
   git clone <url-del-repositorio>
   ```
2. Ábrelo desde **Unity Hub** con la versión `6000.4.3f1`. La primera vez, Unity
   regenerará la carpeta `Library/` (no está en el repositorio, es normal y puede tardar).
3. Abre la escena principal: `Assets/Scenes/Simulador_VR_Quest.unity`.
4. Para desplegar en la Quest, usa *Build & Run* con la plataforma **Android** y el visor conectado.

> **Nota:** este repositorio contiene el **código y los recursos fuente** del
> proyecto. No incluye las builds compiladas (`.apk`), la caché de Unity (`Library/`)
> ni ficheros generados por el editor; todo eso se reconstruye al abrir el proyecto.

---

## Uso básico

Con el proyecto en marcha sobre la Quest, el flujo de operación es:

1. **Calibrar el origen.** Se escanea el marcador impreso con las gafas. El sistema
   fija el origen de coordenadas común al espacio real y virtual, y coloca el dron
   holográfico en el punto de despegue.
2. **Definir la ruta.** Desde el menú se colocan los puntos de paso sobre el espacio
   real. Cada punto se valida en el momento: se marca en rojo si sale del *geofence*,
   si el trayecto choca con la malla del entorno o si queda demasiado cerca del punto
   anterior (por debajo del mínimo que admite el dron).
3. **Simular (opcional).** Un dron fantasma recorre la ruta en virtual para revisarla
   antes de volar de verdad.
4. **Ejecutar.** Tras confirmar, el dron real despega y recorre la ruta tramo a tramo,
   deteniéndose en cada punto a la espera de una decisión: continuar al siguiente,
   corregir la posición o aterrizar en la vertical.
5. **Modo manual** (condición de comparación del estudio). Permite pilotar el dron
   directamente con los mandos: el *joystick* izquierdo gobierna altura y giro, el
   derecho el avance y el desplazamiento lateral, y los botones del mando derecho el
   despegue y el aterrizaje.
6. **Modo de prueba.** Genera escenarios virtuales de inspección (una estantería o
   una estructura de vigas) para practicar o evaluar la interfaz.

> El funcionamiento detallado, el diseño del sistema y los resultados se describen en
> la memoria del Trabajo Fin de Máster.

---

## Estado del proyecto

Proyecto desarrollado como Trabajo Fin de Máster. Es un **prototipo de investigación**,
no un producto acabado. La aplicación es funcional (planificación, simulación y vuelo
real verificados en pruebas con hardware), pero mantiene las limitaciones propias de
un TFM. Las limitaciones y el trabajo futuro se detallan en la memoria del proyecto.

---

## Licencia

Publicado bajo la licencia **MIT**. Ver el fichero [LICENSE](LICENSE).

---

## Autor

**Jaime Provencio Solís**. Trabajo Fin de Máster, 2026.
