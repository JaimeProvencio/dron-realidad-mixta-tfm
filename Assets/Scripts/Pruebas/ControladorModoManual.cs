using System.Collections;
using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: ControladorModoManual
 *
 * Gestiona el vuelo manual del Tello con los mandos de las Quest.
 *
 * ESQUEMA RC ESTANDAR:
 *   Joystick izquierdo Y -> throttle  (subir / bajar)
 *   Joystick izquierdo X -> yaw       (girar)
 *   Joystick derecho   Y -> pitch     (adelante / atras)
 *   Joystick derecho   X -> roll      (strafe izquierda / derecha)
 *
 * DESPEGUE:   boton A del mando derecho (Button.One).
 * ATERRIZAJE: boton B del mando derecho (Button.Two).
 *
 * VUELO FLUIDO: el comando "rc" se envia a frecuencia fija mientras el
 * dron esta en vuelo, replicando el stream continuo del SDK Tello.
 *
 * VOLVER AL MENU: al pulsar el boton de retorno, si el dron sigue en vuelo se
 * envia "land" antes de cambiar de modo, para no dejarlo volando sin control.
 * El boton se DESHABILITA mientras el dron despega o aterriza (ocupadoComando):
 * en esa ventana el Tello no acepta bien un aterrizaje y el dron quedaria
 * colgado. Al terminar la maniobra se vuelve a habilitar.
 * ========================================================================
 */
public class ControladorModoManual : MonoBehaviour
{
    [Header("Conexiones")]
    [SerializeField] private TelloUDP redUDP;
    [SerializeField] private GestorEstadoDron gestorEstado;

    [Header("UI del Canvas Manual")]
    [Tooltip("Boton de volver al modo ruta. Se usa SOLO para deshabilitarlo mientras el dron despega/aterriza; su onClick se cablea por Inspector.")]
    [SerializeField] private UnityEngine.UI.Button btnVolverModoRuta;

    [Header("Configuracion de Input")]
    [Range(0.05f, 0.3f)]
    [Tooltip("Umbral minimo de eje para ignorar ruido del joystick.")]
    [SerializeField] private float umbralDeadzone = 0.1f;

    [Range(10, 100)]
    [Tooltip("Velocidad maxima de traslacion y throttle (0-100).")]
    [SerializeField] private int velocidadMaxima = 50;

    [Range(10, 100)]
    [Tooltip("Velocidad maxima de yaw (giro). Mas alta = giro mas rapido.")]
    [SerializeField] private int velocidadYaw = 80;

    [Tooltip("Intervalo de envio del comando RC (0.08 s, ~12 Hz). Flujo continuo.")]
    [SerializeField] private float intervaloEnvioRC = 0.08f;

    [Tooltip("Segundos de espera tras el despegue antes de habilitar el control RC.")]
    [SerializeField] private float tiempoEstabilizacionDespegue = 3f;

    // ── Estado interno ───────────────────────────────────────────────────
    private bool activo = false;
    private bool enVuelo = false;
    private bool esperandoOk = false;
    private bool ocupadoComando = false;

    private float timerRC = 0f;

    // ════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════════════════

    void Start()
    {
        if (redUDP != null)
            redUDP.OnRespuestaRecibida += ProcesarRespuesta;

        // El boton "volver al modo ruta" se cablea por Inspector (onClick ->
        // Click_VolverModoRuta), igual que el resto de botones. No se anade aqui
        // por codigo para evitar el doble disparo (que abortaba el aterrizaje
        // de seguridad y salia al menu de inmediato).
    }

    void OnDestroy()
    {
        if (redUDP != null)
            redUDP.OnRespuestaRecibida -= ProcesarRespuesta;
    }

    void Update()
    {
        // El boton "volver" se deshabilita mientras hay un comando en curso
        // (despegue/aterrizaje): en esa ventana el Tello no acepta bien un "land"
        // y el dron quedaria colgado si se saliera del modo en ese momento.
        if (btnVolverModoRuta != null)
            btnVolverModoRuta.interactable = !ocupadoComando;

        if (!activo) return;

        if (!ocupadoComando)
        {
            if (!enVuelo && OVRInput.GetDown(OVRInput.Button.One))
                StartCoroutine(SecuenciaDespegue());

            if (enVuelo && OVRInput.GetDown(OVRInput.Button.Two))
                StartCoroutine(SecuenciaAterrizaje());
        }

        if (!enVuelo || ocupadoComando) return;

        timerRC += Time.deltaTime;
        if (timerRC >= intervaloEnvioRC)
        {
            timerRC = 0f;

            Vector2 izq = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            Vector2 der = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

            int roll = Escalar(der.x, velocidadMaxima);
            int pitch = Escalar(der.y, velocidadMaxima);
            int throttle = Escalar(izq.y, velocidadMaxima);
            int yaw = Escalar(izq.x, velocidadYaw);

            redUDP.EnviarComando($"rc {roll} {pitch} {throttle} {yaw}");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // DESPEGUE / ATERRIZAJE
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator SecuenciaDespegue()
    {
        ocupadoComando = true;

        esperandoOk = true;
        redUDP.EnviarComando("takeoff");

        float timeout = Time.time + 10f;
        while (esperandoOk && Time.time < timeout) yield return null;

        yield return new WaitForSeconds(tiempoEstabilizacionDespegue);

        enVuelo = true;
        ocupadoComando = false;
        timerRC = 0f;
    }

    private IEnumerator SecuenciaAterrizaje()
    {
        ocupadoComando = true;

        redUDP.EnviarComando("rc 0 0 0 0");
        yield return new WaitForSeconds(0.3f);

        esperandoOk = true;
        redUDP.EnviarComando("land");

        float timeout = Time.time + 10f;
        while (esperandoOk && Time.time < timeout) yield return null;

        enVuelo = false;
        ocupadoComando = false;
    }

    // ════════════════════════════════════════════════════════════════════
    // API PUBLICA
    // ════════════════════════════════════════════════════════════════════

    public void Activar()
    {
        activo = true;
        enVuelo = false;
        ocupadoComando = false;
        timerRC = 0f;

        redUDP?.EnviarComando("command");
    }

    public void Desactivar()
    {
        activo = false;
        enVuelo = false;
        redUDP?.EnviarComando("rc 0 0 0 0");
    }

    /// <summary>
    /// Volver al modo ruta. Siempre disponible. Si el dron sigue en vuelo,
    /// se ordena un aterrizaje seguro antes de cambiar de modo.
    /// </summary>
    public void Click_VolverModoRuta()
    {
        if (enVuelo && !ocupadoComando)
            StartCoroutine(SecuenciaVolverConAterrizaje());
        else
            SalirAlMenuRuta();
    }

    private IEnumerator SecuenciaVolverConAterrizaje()
    {
        ocupadoComando = true;

        redUDP.EnviarComando("rc 0 0 0 0");
        yield return new WaitForSeconds(0.3f);

        esperandoOk = true;
        redUDP.EnviarComando("land");

        float timeout = Time.time + 10f;
        while (esperandoOk && Time.time < timeout) yield return null;

        enVuelo = false;
        ocupadoComando = false;

        SalirAlMenuRuta();
    }

    private void SalirAlMenuRuta()
    {
        if (gestorEstado != null)
            gestorEstado.Click_VolverModoRutaDesdeModoManual();
    }

    // ════════════════════════════════════════════════════════════════════
    // RESPUESTAS DEL TELLO
    // ════════════════════════════════════════════════════════════════════

    private void ProcesarRespuesta(string respuesta)
    {
        if (respuesta == "ok" && esperandoOk)
            esperandoOk = false;
    }

    // ════════════════════════════════════════════════════════════════════
    // UTILIDADES
    // ════════════════════════════════════════════════════════════════════

    private int Escalar(float valor, int velocidad)
    {
        if (Mathf.Abs(valor) < umbralDeadzone) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(valor * velocidad), -100, 100);
    }
}