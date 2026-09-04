using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// EjecutorMisionFisica — secuenciador de vuelo waypoint a waypoint. Orquesta
// el ciclo: despegue -> pausa en el waypoint 0 -> navegacion pausada por
// waypoints -> aterrizaje. Tras el despegue el dron se estabiliza en el
// waypoint 0 y entrega el control al usuario. Al terminar la ruta avisa a la
// FSM para desactivar el boton de "siguiente waypoint".
//
// El regreso al origen se elimino: en lazo abierto el trayecto recto es
// propenso a colisiones; la salida segura es el aterrizaje vertical.
public class EjecutorMisionFisica : MonoBehaviour
{
    [Header("Conexiones")]
    [SerializeField] private RouteManager supervisorRuta;
    [SerializeField] private TelloUDP redUDP;
    [SerializeField] private GhostMath moduloMatematico;
    [SerializeField] private GestorEstadoDron gestorEstado;

    [Header("Configuracion de Vuelo")]
    [SerializeField] private float velocidadVuelo = 25f;

    [Header("Tiempos de Estabilizacion Inercial")]
    [Tooltip("Segundos de espera tras cada comando GO para que el dron se detenga.")]
    [SerializeField] private float esperaTrasMovimiento = 1.5f;

    [Tooltip("Segundos de espera tras cada comando de rotacion.")]
    [SerializeField] private float esperaTrasRotacion = 1.5f;

    // Estado interno
    private bool esperandoRespuestaDron = false;
    private bool abortarMision = false;
    private Coroutine latidoCoroutine;

    private List<DatosWaypoint> rutaActiva;
    private int indiceWaypointActual = -1;

    private Vector3 ultimaPosicionReal;
    private float ultimaRotacionReal;

    void Start()
    {
        if (redUDP != null)
            redUDP.OnRespuestaRecibida += ProcesarRespuestaDron;
    }

    // --- API publica ---

    public void IniciarMisionAutomatica()
    {
        abortarMision = false;
        DetenerLatido();
        StartCoroutine(SecuenciadorDeVuelo());
    }

    public void ContinuarSiguienteWaypoint()
    {
        if (rutaActiva == null)
        {
            Debug.LogWarning("[Ejecutor] No hay ruta activa.");
            return;
        }

        DetenerLatido();
        indiceWaypointActual++;

        if (indiceWaypointActual >= rutaActiva.Count)
        {
            Debug.Log("[Ejecutor] Ruta completada. Hover final.");
            latidoCoroutine = StartCoroutine(MantenerLatido());
            if (gestorEstado != null) gestorEstado.NotificarRutaFinalizada();
            return;
        }

        StartCoroutine(ViajarAWaypointActual());
    }

    public void OrdenarAterrizajeVertical()
    {
        DetenerLatido();
        StartCoroutine(SecuenciaFinal("land"));
    }

    public void PararMotoresEmergencia()
    {
        // En SDK 1.3 no hay stop suave: el "go" es bloqueante a bordo y ni "stop"
        // (unknown command) ni "rc 0 0 0 0" lo interrumpen. El unico corte real es
        // "emergency", que APAGA los motores (el dron cae). La mision se aborta.
        StopAllCoroutines();
        DetenerLatido();
        abortarMision = true;
        redUDP.EnviarComando("emergency");
    }

    public Vector3 ObtenerPosicionWaypointActual()
    {
        if (rutaActiva == null || indiceWaypointActual < 0 || indiceWaypointActual >= rutaActiva.Count)
            return ultimaPosicionReal;
        return rutaActiva[indiceWaypointActual].posicion;
    }

    public float ObtenerRotacionWaypointActual()
    {
        if (rutaActiva == null || indiceWaypointActual < 0 || indiceWaypointActual >= rutaActiva.Count)
            return ultimaRotacionReal;
        return rutaActiva[indiceWaypointActual].rotacionY;
    }

    public void EjecutarCorreccionFisica(Vector3 posicionRealSenalada)
    {
        DetenerLatido();
        StartCoroutine(SecuenciaCorreccion(posicionRealSenalada));
    }

    // --- Secuencias privadas ---

    private IEnumerator SecuenciadorDeVuelo()
    {
        rutaActiva = supervisorRuta.ObtenerRutaCompleta();
        if (rutaActiva == null || rutaActiva.Count == 0)
        {
            Debug.LogWarning("[Ejecutor] Ruta vacia. Abortando mision.");
            yield break;
        }

        yield return StartCoroutine(EnviarComandoYEsperar("takeoff"));
        yield return new WaitForSeconds(esperaTrasMovimiento);

        if (abortarMision) yield break;

        ultimaPosicionReal = supervisorRuta.anclaOrigen.ObtenerPosicionDespegue();
        ultimaRotacionReal = supervisorRuta.anclaOrigen.ObtenerRotacionDespegueY();

        indiceWaypointActual = -1;

        latidoCoroutine = StartCoroutine(MantenerLatido());
        if (gestorEstado != null) gestorEstado.MostrarMenuWaypoint();
    }

    private IEnumerator ViajarAWaypointActual()
    {
        if (abortarMision) yield break;

        DatosWaypoint puntoActual = rutaActiva[indiceWaypointActual];

        string cmdTraslacion = moduloMatematico.CalcularComandoHaciaPunto(
            ultimaPosicionReal,
            puntoActual.posicion,
            ultimaRotacionReal,
            velocidadVuelo
        );
        yield return StartCoroutine(EnviarComandoYEsperar(cmdTraslacion));
        yield return new WaitForSeconds(esperaTrasMovimiento);

        if (abortarMision) yield break;

        string cmdRotacion = moduloMatematico.CalcularComandoRotacion(
            ultimaRotacionReal,
            puntoActual.rotacionY
        );
        if (!string.IsNullOrEmpty(cmdRotacion))
        {
            yield return StartCoroutine(EnviarComandoYEsperar(cmdRotacion));
            yield return new WaitForSeconds(esperaTrasRotacion);
        }

        if (abortarMision) yield break;

        ultimaPosicionReal = puntoActual.posicion;
        ultimaRotacionReal = puntoActual.rotacionY;

        latidoCoroutine = StartCoroutine(MantenerLatido());

        if (gestorEstado != null) gestorEstado.MostrarMenuWaypoint();
        else Debug.LogError("[Ejecutor] GestorEstadoDron no asignado.");
    }

    private IEnumerator SecuenciaCorreccion(Vector3 posicionRealSenalada)
    {
        Vector3 posicionObjetivo = ObtenerPosicionWaypointActual();

        string cmdCorreccion = moduloMatematico.CalcularComandoHaciaPunto(
            posicionRealSenalada,
            posicionObjetivo,
            ultimaRotacionReal,
            velocidadVuelo
        );

        Debug.Log($"[Ejecutor] Correccion fisica: {posicionRealSenalada} -> {posicionObjetivo}");

        yield return StartCoroutine(EnviarComandoYEsperar(cmdCorreccion));
        yield return new WaitForSeconds(esperaTrasMovimiento);

        ultimaPosicionReal = posicionObjetivo;

        latidoCoroutine = StartCoroutine(MantenerLatido());

        if (gestorEstado != null) gestorEstado.MostrarMenuWaypoint();
    }

    private IEnumerator SecuenciaFinal(string comandoAterrizaje)
    {
        redUDP.EnviarComando(comandoAterrizaje);
        yield return new WaitForSeconds(5f);

        if (gestorEstado != null)
        {
            gestorEstado.FinalizarOperacionYRegresar();
            Debug.Log("[Ejecutor] Aterrizaje completado. Interfaz reseteada a Tierra.");
        }
    }

    // --- Utilidades internas ---

    private IEnumerator EnviarComandoYEsperar(string comando)
    {
        esperandoRespuestaDron = true;
        redUDP.EnviarComando(comando);

        float tiempoInicio = Time.time;
        while (esperandoRespuestaDron && Time.time - tiempoInicio < 15f)
            yield return null;

        esperandoRespuestaDron = false;
    }

    private void ProcesarRespuestaDron(string respuesta)
    {
        if (respuesta == "ok")
            esperandoRespuestaDron = false;
    }

    private IEnumerator MantenerLatido()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            redUDP.EnviarComando("command");
        }
    }

    private void DetenerLatido()
    {
        if (latidoCoroutine != null)
        {
            StopCoroutine(latidoCoroutine);
            latidoCoroutine = null;
        }
    }
}