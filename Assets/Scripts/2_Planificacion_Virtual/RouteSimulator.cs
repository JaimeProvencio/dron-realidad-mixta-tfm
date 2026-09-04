using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

// RouteSimulator — previsualiza la ruta desplazando el holograma del dron
// por los waypoints (traslacion y luego giro en cada tramo). No usa la red:
// es una comprobacion visual previa al vuelo real.
public class RouteSimulator : MonoBehaviour
{
    [Header("Conexiones")]
    [Tooltip("Referencia al almacén de la ruta para obtener las coordenadas.")]
    [SerializeField] private RouteManager supervisorRuta;

    [Tooltip("El modelo 3D (DronFantasma) que se moverá visualmente.")]
    [SerializeField] private Transform hologramaDron;

    [Header("Configuración de Simulación")]
    [Tooltip("Velocidad visual en metros por segundo a la que se moverá el holograma.")]
    [SerializeField] private float velocidadSimulacion = 0.5f;

    [Tooltip("Velocidad de giro en grados por segundo.")]
    [SerializeField] private float velocidadRotacion = 90f;

    // Estado interno
    private bool simulacionEnCurso = false;

    // Se conecta al boton "Simular Ruta" de la UI.
    public async void EjecutarSimulacion()
    {
        if (simulacionEnCurso) return;

        if (hologramaDron == null || supervisorRuta == null || supervisorRuta.anclaOrigen == null)
        {
            Debug.LogError("[Simulador] Faltan referencias en el Inspector.");
            return;
        }

        List<DatosWaypoint> puntos = supervisorRuta.ObtenerRutaCompleta();
        if (puntos.Count == 0)
        {
            Debug.LogWarning("[Simulador] No hay ruta para simular.");
            return;
        }

        simulacionEnCurso = true;
        Debug.Log("[Simulador] Iniciando previsualización visual estricta (Traslación -> Rotación)...");

        if (supervisorRuta.validadorSeguridad != null)
        {
            supervisorRuta.validadorSeguridad.gameObject.SetActive(false);
        }

        // Se desactiva el GameObject completo para ocultar malla y colisiones.
        TiradorRotacionY tiradorBola = hologramaDron.GetComponentInChildren<TiradorRotacionY>();
        if (tiradorBola != null) tiradorBola.gameObject.SetActive(false);

        List<DatosWaypoint> secuenciaVuelo = new List<DatosWaypoint>();
        secuenciaVuelo.Add(new DatosWaypoint(supervisorRuta.anclaOrigen.ObtenerPosicionDespegue(), supervisorRuta.anclaOrigen.ObtenerRotacionDespegueY()));
        secuenciaVuelo.AddRange(puntos);

        // Teletransporte inmediato de la posición al origen.
        hologramaDron.position = supervisorRuta.anclaOrigen.transform.position;
        hologramaDron.rotation = Quaternion.identity;

        Transform chasis = supervisorRuta.chasisVisualDron;
        chasis.rotation = Quaternion.Euler(0, supervisorRuta.anclaOrigen.ObtenerRotacionDespegueY(), 0);

        await Task.Delay(1000);

        foreach (DatosWaypoint punto in secuenciaVuelo)
        {
            // Fase 1: traslacion hasta el waypoint
            float distancia = Vector3.Distance(hologramaDron.position, punto.posicion);

            if (distancia > 0.01f)
            {
                float tiempoVuelo = distancia / velocidadSimulacion;
                float transcurrido = 0;
                Vector3 inicio = hologramaDron.position;

                while (transcurrido < tiempoVuelo)
                {
                    transcurrido += Time.deltaTime;
                    float porcentajeCarga = transcurrido / tiempoVuelo;

                    hologramaDron.position = Vector3.Lerp(inicio, punto.posicion, porcentajeCarga);
                    await System.Threading.Tasks.Task.Yield();
                }
                hologramaDron.position = punto.posicion;
            }

            // Fase 2: giro (yaw) hasta la orientacion del waypoint
            float diferenciaAngulo = Mathf.Abs(Mathf.DeltaAngle(chasis.eulerAngles.y, punto.rotacionY));

            if (diferenciaAngulo > 0.5f)
            {
                float tiempoGiro = diferenciaAngulo / velocidadRotacion;
                float transcurridoGiro = 0;
                Quaternion rotacionInicio = chasis.rotation;
                Quaternion rotacionDestino = Quaternion.Euler(0, punto.rotacionY, 0);

                while (transcurridoGiro < tiempoGiro)
                {
                    transcurridoGiro += Time.deltaTime;
                    float porcentajeGiro = transcurridoGiro / tiempoGiro;

                    chasis.rotation = Quaternion.Slerp(rotacionInicio, rotacionDestino, porcentajeGiro);
                    await System.Threading.Tasks.Task.Yield();
                }
                chasis.rotation = rotacionDestino;
            }

            // Fase 3: pausa de estabilizacion
            await Task.Delay(1000);
        }

        Debug.Log("[Simulador] Simulación finalizada. El dron permanece en el último Waypoint.");

        if (supervisorRuta.validadorSeguridad != null)
        {
            supervisorRuta.validadorSeguridad.gameObject.SetActive(true);
        }

        // Se reactiva el GameObject completo.
        if (tiradorBola != null) tiradorBola.gameObject.SetActive(true);

        simulacionEnCurso = false;
    }
}