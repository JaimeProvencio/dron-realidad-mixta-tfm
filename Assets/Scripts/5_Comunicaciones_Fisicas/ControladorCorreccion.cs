using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

// ControladorCorreccion — orquesta la correccion de posicion (opcion del menu
// de waypoint): coloca el dron de correccion en la posicion teorica del
// waypoint e instancia un volumen de error.
//
// Regla del Tello: el "go" es invalido si los tres ejes estan por debajo de
// 20 cm, asi que el volumen de error es un cubo de 40 cm de lado. Dentro del
// cubo el boton de aceptar se deshabilita y el cubo se muestra rojo (correccion
// imposible); fuera, verde y habilitado.
//
// La rotacion se fija ANTES de activar el dron para que FijarEjes capture ese
// angulo en su OnEnable y no lo machaque.
public class ControladorCorreccion : MonoBehaviour
{
    [Header("Conexiones")]
    [SerializeField] private EjecutorMisionFisica ejecutorMision;

    [Header("Dron de Correccion")]
    [Tooltip("GameObject del dron de correccion (inactivo por defecto).")]
    [SerializeField] private GameObject dronCorreccion;

    [Header("Volumen de Error Aceptable")]
    [Tooltip("Prefab del volumen de error (cubo de 40cm de lado). Se instancia al corregir.")]
    [FormerlySerializedAs("prefabEsferaError")]
    [SerializeField] private GameObject prefabCuboError;

    [Tooltip("Material cuando el dron esta DENTRO del rango: ROJO (correccion imposible).")]
    [SerializeField] private Material materialDentro;

    [Tooltip("Material cuando el dron esta FUERA del rango: VERDE (correccion posible).")]
    [SerializeField] private Material materialFuera;

    [Header("Boton de Aceptar Correccion")]
    [Tooltip("Boton 'Si' del panel de confirmar correccion.")]
    [SerializeField] private Button btnAceptarCorreccion;

    // Minimo por eje del comando go del Tello (20 cm)
    private const float DistanciaMinimaTello = 0.20f;

    private GameObject cuboInstancia;
    private Renderer cuboRenderer;
    private Vector3 posicionObjetivo;
    private bool corrigiendo = false;

    void Update()
    {
        if (!corrigiendo || dronCorreccion == null) return;

        // Regla del Tello: el comando go es invalido si los tres ejes estan
        // por debajo del minimo. El dron esta "dentro" (correccion imposible)
        // si la mayor de las tres componentes no supera DistanciaMinimaTello.
        Vector3 delta = dronCorreccion.transform.position - posicionObjetivo;
        float mayorComponente = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
        bool dentroDelRango = mayorComponente < DistanciaMinimaTello;

        if (btnAceptarCorreccion != null)
            btnAceptarCorreccion.interactable = !dentroDelRango;

        if (cuboRenderer != null)
            cuboRenderer.material = dentroDelRango ? materialDentro : materialFuera;
    }

    public void IniciarCorreccion()
    {
        if (ejecutorMision == null || dronCorreccion == null)
        {
            Debug.LogError("[Correccion] Faltan referencias en el Inspector.");
            return;
        }

        posicionObjetivo = ejecutorMision.ObtenerPosicionWaypointActual();
        float rotacionTeorica = ejecutorMision.ObtenerRotacionWaypointActual();

        // Posicion y rotacion ANTES de activar: el FijarEjes captura el angulo en OnEnable
        dronCorreccion.transform.position = posicionObjetivo;
        dronCorreccion.transform.rotation = Quaternion.Euler(0f, rotacionTeorica, 0f);
        dronCorreccion.SetActive(true);

        if (prefabCuboError != null)
        {
            cuboInstancia = Instantiate(prefabCuboError, posicionObjetivo, Quaternion.identity);
            cuboRenderer = cuboInstancia.GetComponentInChildren<Renderer>();
        }

        corrigiendo = true;
    }

    public void ConfirmarCorreccion()
    {
        if (ejecutorMision == null || dronCorreccion == null) return;

        Vector3 posicionRealSenalada = dronCorreccion.transform.position;

        FinalizarVisuales();
        ejecutorMision.EjecutarCorreccionFisica(posicionRealSenalada);
    }

    public void CancelarCorreccion()
    {
        FinalizarVisuales();
    }

    private void FinalizarVisuales()
    {
        corrigiendo = false;
        if (dronCorreccion != null) dronCorreccion.SetActive(false);
        if (cuboInstancia != null) Destroy(cuboInstancia);
        cuboInstancia = null;
        cuboRenderer = null;
    }
}