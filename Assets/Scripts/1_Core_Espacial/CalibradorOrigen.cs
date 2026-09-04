using UnityEngine;
using TMPro;

// CalibradorOrigen — puente entre el MRUK (hardware) y Unity: recibe la
// coordenada del marcador fisico y coloca el dron aplicando un offset.
// El texto del boton es dinamico ("escaneando"/"escaneado"); se guarda la
// clave del estado para repintarlo si el usuario cambia de idioma.
public class CalibradorOrigen : MonoBehaviour
{
    [Header("Referencias Principales")]
    [SerializeField] private OriginPoint scriptOrigen;
    [SerializeField] private Transform dronFantasma;

    [Header("Configuracion Fisica del Folio")]
    [SerializeField] private Vector3 offsetMarcadorDron = new Vector3(0.322f, 0, 0);

    [Header("Interfaz de Usuario")]
    [SerializeField] private TextMeshProUGUI textoBoton;

    private bool calibracionBloqueada = false;
    private Transform marcadorFisicoGuardado;

    // Clave de traduccion del estado actual del boton, para repintar al cambiar idioma
    private string claveTextoActual = "calibrador_escaneando";
    private Color colorTextoActual = Color.black;

    void Start()
    {
        ActualizarUI("calibrador_escaneando", Color.black);
    }

    void OnEnable()
    {
        if (GestorIdioma.Instancia != null)
            GestorIdioma.Instancia.OnIdiomaCambiado += RefrescarIdioma;
    }

    void OnDisable()
    {
        if (GestorIdioma.Instancia != null)
            GestorIdioma.Instancia.OnIdiomaCambiado -= RefrescarIdioma;
    }

    public void HabilitarRecalibracion()
    {
        calibracionBloqueada = false;
        ActualizarUI("calibrador_escaneando", Color.black);

        // Si ya se escaneo el marcador antes, se recalibra directamente con el
        if (marcadorFisicoGuardado != null)
            CalibrarDesdeMarcador(marcadorFisicoGuardado);
    }

    public void RecibirTrackableMRUK(Meta.XR.MRUtilityKit.MRUKTrackable trackableDetectado)
    {
        if (trackableDetectado == null)
        {
            Debug.LogError("[Calibrador] Trackable vacio. Usa 'Dynamic MRUKTrackable' en el Inspector.");
            return;
        }

        marcadorFisicoGuardado = trackableDetectado.transform;
        CalibrarDesdeMarcador(marcadorFisicoGuardado);
    }

    public void CalibrarDesdeMarcador(Transform marcadorFisico)
    {
        if (calibracionBloqueada || scriptOrigen == null) return;

        // 1. Aplicar offset relativo al papel para obtener la coordenada global
        Vector3 posicionRealDron = marcadorFisico.TransformPoint(offsetMarcadorDron);
        scriptOrigen.transform.position = posicionRealDron;

        // 2. Alinear la proa del dron hacia el eje X negativo del marcador
        Vector3 direccionEjeXNegativo = -marcadorFisico.right;
        direccionEjeXNegativo.y = 0;

        if (direccionEjeXNegativo != Vector3.zero)
            scriptOrigen.transform.rotation = Quaternion.LookRotation(direccionEjeXNegativo);

        // 3. Posicionar el holograma
        if (dronFantasma != null)
        {
            dronFantasma.position = scriptOrigen.ObtenerPosicionDespegue();
            dronFantasma.rotation = scriptOrigen.transform.rotation;
        }

        // 4. Bloquear el sistema y notificar
        calibracionBloqueada = true;
        ActualizarUI("calibrador_escaneado", Color.green);
        Debug.Log("[Calibrador] Origen calibrado.");
    }

    /// <summary>Guarda la clave y el color, y pinta el texto en el idioma actual.</summary>
    private void ActualizarUI(string clave, Color colorTexto)
    {
        claveTextoActual = clave;
        colorTextoActual = colorTexto;
        RefrescarIdioma();
    }

    private void RefrescarIdioma()
    {
        if (textoBoton == null) return;

        textoBoton.text = (GestorIdioma.Instancia != null)
            ? GestorIdioma.Instancia.Traducir(claveTextoActual)
            : claveTextoActual;
        textoBoton.color = colorTextoActual;
    }
}