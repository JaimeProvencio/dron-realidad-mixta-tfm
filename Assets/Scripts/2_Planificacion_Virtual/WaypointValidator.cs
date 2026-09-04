using UnityEngine;

// WaypointValidator — valida la seguridad de cada waypoint en tiempo real.
// El rayo evaluador nace del ultimo waypoint (o del origen si la ruta esta
// vacia). Comprueba tres reglas: comando Tello valido (algun eje supera los
// 20 cm) con altura minima, geofence cilindrico (radio y altura maximos), y
// colision contra la malla MRUK por proyeccion volumetrica (BoxCast).
[RequireComponent(typeof(LineRenderer))]
public class WaypointValidator : MonoBehaviour
{
    [Header("Conexiones Arquitectonicas")]
    [SerializeField] private OriginPoint anclaOrigen;
    [SerializeField] private RouteManager gestorRutas;

    [Header("Fisicas del Dron")]
    [Tooltip("El colisionador del dron para calcular su volumen real en el espacio.")]
    [SerializeField] private BoxCollider colisionadorDron;

    [Header("Configuracion de Escaneo MRUK")]
    [Tooltip("Capa asignada a la malla de Meta (ej. EntornoReal).")]
    [SerializeField] private LayerMask capaEntornoFisico;

    [Header("Geofencing (Limites de Seguridad)")]
    [SerializeField] private float radioMaximoVuelo = 5.0f;
    [SerializeField] private float alturaMaximaVuelo = 5.5f;

    [Header("Restriccion de Movimiento (SDK Tello)")]
    [Tooltip("Desplazamiento minimo en al menos un eje para que el comando go sea valido (metros).")]
    [SerializeField] private float desplazamientoMinimoEje = 0.20f;
    [Tooltip("Altura minima de seguridad sobre el suelo (metros).")]
    [SerializeField] private float alturaMinimaSuelo = 0.20f;

    [Header("Diagnostico Visual (Materiales URP)")]
    public Material matSeguro;       // OK
    public Material matColision;     // Choque fisico con la malla MRUK
    public Material matGeofence;     // Fuera del radio o altura maxima
    public Material matDistancia;    // Comando invalido (ningun eje > 20cm) o demasiado bajo

    public bool rutaValida { get; private set; }

    private enum TipoError { Ninguno, ColisionFisica, LimiteGeofence, RestriccionDistancia }
    private TipoError estadoActual;

    private LineRenderer linea;

    void Start()
    {
        linea = GetComponent<LineRenderer>();
        linea.startWidth = 0.02f;
        linea.endWidth = 0.02f;

        if (colisionadorDron == null)
            Debug.LogWarning("[Validador] No se ha asignado el BoxCollider del dron. Se usara un volumen por defecto.");
    }

    void Update()
    {
        if (anclaOrigen == null || gestorRutas == null) return;

        Vector3 inicioLinea;
        var rutaActual = gestorRutas.ObtenerCoordenadasRuta();

        if (rutaActual != null && rutaActual.Count > 0)
            inicioLinea = rutaActual[rutaActual.Count - 1];
        else
            inicioLinea = anclaOrigen.ObtenerPosicionDespegue();

        Vector3 destinoLinea = transform.position;

        linea.SetPosition(0, inicioLinea);
        linea.SetPosition(1, destinoLinea);

        estadoActual = ObtenerDiagnosticoTrayectoria(inicioLinea, destinoLinea);
        rutaValida = (estadoActual == TipoError.Ninguno);

        AplicarMaterial();
    }

    private TipoError ObtenerDiagnosticoTrayectoria(Vector3 origen, Vector3 destino)
    {
        // REGLA 1: COMANDO VALIDO (Tello) + ALTURA MINIMA
        // El comando go se rechaza si ningun eje supera el minimo. Validamos
        // que la mayor de las tres componentes del desplazamiento lo supere.
        Vector3 delta = destino - origen;
        float mayorComponente = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
        float alturaSobreSuelo = destino.y - anclaOrigen.transform.position.y;

        if (mayorComponente < desplazamientoMinimoEje || alturaSobreSuelo < alturaMinimaSuelo)
            return TipoError.RestriccionDistancia; // ROJO (matDistancia)

        // REGLA 2: GEOFENCING (Cilindro de Seguridad)
        Vector3 desplazamiento = destino - anclaOrigen.transform.position;
        float distHorizontal = new Vector2(desplazamiento.x, desplazamiento.z).magnitude;
        float altura = desplazamiento.y;

        if (distHorizontal > radioMaximoVuelo || altura > alturaMaximaVuelo)
            return TipoError.LimiteGeofence;

        // REGLA 3: OBSTACULOS FISICOS (MRUK) por proyeccion volumetrica
        Vector3 direccion = (destino - origen).normalized;
        float distancia = Vector3.Distance(origen, destino);

        Vector3 mitadTamanio = colisionadorDron != null ?
            new Vector3(
                colisionadorDron.size.x * transform.lossyScale.x,
                colisionadorDron.size.y * transform.lossyScale.y,
                colisionadorDron.size.z * transform.lossyScale.z
            ) * 0.5f
            : new Vector3(0.15f, 0.15f, 0.15f);

        if (Physics.BoxCast(origen, mitadTamanio, direccion, out RaycastHit hit, transform.rotation, distancia, capaEntornoFisico))
        {
            Debug.Log($"[Validador] COLISION FISICA VOLUMETRICA: Interseccion con -> {hit.collider.gameObject.name}");
            return TipoError.ColisionFisica;
        }

        return TipoError.Ninguno;
    }

    private void AplicarMaterial()
    {
        if (linea.material == null) return;

        switch (estadoActual)
        {
            case TipoError.Ninguno:
                if (matSeguro != null) linea.material = matSeguro;
                break;
            case TipoError.ColisionFisica:
                if (matColision != null) linea.material = matColision;
                break;
            case TipoError.LimiteGeofence:
                if (matGeofence != null) linea.material = matGeofence;
                break;
            case TipoError.RestriccionDistancia:
                if (matDistancia != null) linea.material = matDistancia;
                break;
        }
    }
}