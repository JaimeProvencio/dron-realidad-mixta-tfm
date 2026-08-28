using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * ========================================================================
 * SCRIPT: RouteManager
 * FUNCION: Almacena la secuencia de waypoints y gestiona su representacion.
 *
 * ABSORCION DE RUIDO DE YAW: al crear un waypoint, si su orientacion difiere
 * menos de umbralYawGrados respecto al waypoint anterior (o el origen si es
 * el primero), hereda el yaw anterior. Asi el usuario puede mover el dron
 * con la muneca sin acumular giros minimos que el Tello no deberia ejecutar.
 * ========================================================================
 */
[RequireComponent(typeof(LineRenderer))]
public class RouteManager : MonoBehaviour
{
    [Header("Referencias Fundamentales")]
    public OriginPoint anclaOrigen;
    [SerializeField] private GameObject waypointPrefab;

    [Tooltip("El objeto padre (DronFantasmas) que define la POSICION")]
    [SerializeField] private Transform dronFantasma;

    [Tooltip("El objeto hijo (ModeloTello) que gira hacia la bola")]
    public Transform chasisVisualDron;

    [Header("Validacion de Seguridad y UI")]
    public WaypointValidator validadorSeguridad;
    [SerializeField] private Button btnCrearWaypoint;

    [Tooltip("Diferencia de yaw minima (grados) para registrar un giro nuevo. Por debajo, el waypoint hereda el yaw del anterior y se absorbe el ruido de rotacion.")]
    [SerializeField] private float umbralYawGrados = 5f;

    [Tooltip("El objeto raiz del Canvas que muestra la altura sobre el dron")]
    [SerializeField] private GameObject canvasAlturaFlotante;

    [Header("Configuracion Visual (Lineas de Ruta)")]
    [Tooltip("Grosor de la linea durante la fase de PLANIFICACION.")]
    [SerializeField] private float anchoLineaPlanificacion = 0.02f;
    [Tooltip("Grosor de la linea durante la fase de VUELO (disimulada).")]
    [SerializeField] private float anchoLineaVuelo = 0.005f;

    [Tooltip("Material opaco para usar mientras se disena la ruta.")]
    [SerializeField] private Material matLineaPlanificacion;
    [Tooltip("Material transparente y sutil para usar cuando el dron esta volando.")]
    [SerializeField] private Material matLineaVuelo;

    private List<GameObject> listaWaypoints = new List<GameObject>();
    private List<float> listaYaw = new List<float>();
    private LineRenderer trazadorRuta;

    void Start()
    {
        trazadorRuta = GetComponent<LineRenderer>();

        trazadorRuta.startWidth = anchoLineaPlanificacion;
        trazadorRuta.endWidth = anchoLineaPlanificacion;

        if (matLineaPlanificacion != null)
            trazadorRuta.material = matLineaPlanificacion;

        ActualizarLineaVisual();
    }

    void Update()
    {
        if (validadorSeguridad != null && btnCrearWaypoint != null)
            btnCrearWaypoint.interactable = validadorSeguridad.rutaValida;
    }

    public void RegistrarWaypointInteractivo()
    {
        if (dronFantasma == null || chasisVisualDron == null)
        {
            Debug.LogError("[RouteManager] Faltan referencias del dron o su chasis visual.");
            return;
        }

        if (validadorSeguridad != null && !validadorSeguridad.rutaValida)
        {
            Debug.LogWarning("[RouteManager] Accion denegada.");
            return;
        }

        AgregarWaypoint(dronFantasma.position);
    }

    private void AgregarWaypoint(Vector3 posicionFijada)
    {
        if (waypointPrefab == null) return;

        // Yaw del tirador en el momento de fijar el waypoint
        float yawDeseado = chasisVisualDron.eulerAngles.y;

        // Yaw de referencia: el del waypoint anterior, o el del origen si es el primero
        float yawAnterior = (listaYaw.Count > 0)
            ? listaYaw[listaYaw.Count - 1]
            : anclaOrigen.ObtenerRotacionDespegueY();

        // Si la diferencia es menor que el umbral, se hereda el yaw anterior
        // (se absorbe el ruido y no se acumula error ni se manda un giro minimo)
        float yawFinal = (Mathf.Abs(Mathf.DeltaAngle(yawAnterior, yawDeseado)) < umbralYawGrados)
            ? yawAnterior
            : yawDeseado;

        Quaternion rotacion = Quaternion.Euler(0f, yawFinal, 0f);

        GameObject nuevoWP = Instantiate(waypointPrefab, posicionFijada, rotacion);
        nuevoWP.name = $"Waypoint_{listaWaypoints.Count + 1}";
        nuevoWP.transform.SetParent(this.transform);

        listaWaypoints.Add(nuevoWP);
        listaYaw.Add(yawFinal);
        ActualizarLineaVisual();
    }

    public void DeshacerUltimo()
    {
        if (listaWaypoints.Count > 0)
        {
            int indiceUltimo = listaWaypoints.Count - 1;
            GameObject wpABorrar = listaWaypoints[indiceUltimo];
            listaWaypoints.RemoveAt(indiceUltimo);
            listaYaw.RemoveAt(indiceUltimo);
            Destroy(wpABorrar);
            ActualizarLineaVisual();
        }
    }

    public void BorrarRutaTotal()
    {
        foreach (GameObject wp in listaWaypoints) Destroy(wp);
        listaWaypoints.Clear();
        listaYaw.Clear();
        ActualizarLineaVisual();
    }

    private void ActualizarLineaVisual()
    {
        if (anclaOrigen == null) return;
        trazadorRuta.positionCount = listaWaypoints.Count + 1;
        trazadorRuta.SetPosition(0, anclaOrigen.ObtenerPosicionDespegue());

        for (int i = 0; i < listaWaypoints.Count; i++)
            trazadorRuta.SetPosition(i + 1, listaWaypoints[i].transform.position);
    }

    public List<Vector3> ObtenerCoordenadasRuta()
    {
        List<Vector3> coordenadas = new List<Vector3>();
        foreach (GameObject wp in listaWaypoints) coordenadas.Add(wp.transform.position);
        return coordenadas;
    }

    public List<DatosWaypoint> ObtenerRutaCompleta()
    {
        List<DatosWaypoint> rutaCompleta = new List<DatosWaypoint>();
        for (int i = 0; i < listaWaypoints.Count; i++)
            rutaCompleta.Add(new DatosWaypoint(listaWaypoints[i].transform.position, listaYaw[i]));
        return rutaCompleta;
    }

    /*
     * ========================================================================
     * MODULO DE VISIBILIDAD Y ESTADO
     * ========================================================================
     */

    public void NotificarEstadoVuelo(bool enVuelo)
    {
        if (anclaOrigen != null)
        {
            WaypointVuelo visualWP0 = anclaOrigen.GetComponentInChildren<WaypointVuelo>(true);
            if (visualWP0 != null) visualWP0.EstablecerModoVuelo(enVuelo);
        }

        foreach (GameObject wp in listaWaypoints)
        {
            if (wp != null)
            {
                WaypointVuelo visual = wp.GetComponent<WaypointVuelo>();
                if (visual != null) visual.EstablecerModoVuelo(enVuelo);
            }
        }

        if (trazadorRuta != null)
        {
            if (enVuelo)
            {
                trazadorRuta.startWidth = anchoLineaVuelo;
                trazadorRuta.endWidth = anchoLineaVuelo;
                if (matLineaVuelo != null) trazadorRuta.material = matLineaVuelo;
            }
            else
            {
                trazadorRuta.startWidth = anchoLineaPlanificacion;
                trazadorRuta.endWidth = anchoLineaPlanificacion;
                if (matLineaPlanificacion != null) trazadorRuta.material = matLineaPlanificacion;
            }
        }
    }

    public void EstablecerVisibilidadPlanificacion(bool verRuta, bool verHerramientas)
    {
        if (trazadorRuta != null) trazadorRuta.enabled = verRuta;

        foreach (GameObject wp in listaWaypoints)
            if (wp != null) wp.SetActive(verRuta);

        if (anclaOrigen != null) anclaOrigen.EstablecerVisibilidadPuntoDespegue(verRuta);

        if (dronFantasma != null) dronFantasma.gameObject.SetActive(verHerramientas);
        if (canvasAlturaFlotante != null) canvasAlturaFlotante.SetActive(verHerramientas);
        if (validadorSeguridad != null) validadorSeguridad.gameObject.SetActive(verHerramientas);
    }
}