using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * ========================================================================
 * SCRIPT: GestorEstadoDron
 *
 * Maquina de Estados Finitos (FSM) central del sistema. Controla la
 * transicion entre modos y sincroniza paneles, hologramas y canvases.
 *
 * FLUJO DE VUELO (cierre de bucle manual):
 *   EnVuelo                     -> dron en transito. Boton de parada de emergencia.
 *   ConfirmandoParadaEmergencia -> confirmacion "parar motores?". "Si" corta motores
 *                                  (comando emergency, el dron CAE); "No" vuelve al
 *                                  estado anterior.
 *   EsperandoDecision           -> dron quieto en waypoint. Menu de tres opciones
 *                                  (siguiente, corregir, aterrizar aqui).
 *   CorrigiendoPosicion         -> usuario arrastrando el dron de correccion.
 *
 * PARADA DE EMERGENCIA: el Tello es SDK 1.3, donde no hay stop suave (el "go" es
 * bloqueante a bordo y ni "stop" ni "rc 0 0 0 0" lo interrumpen). El unico corte
 * real es "emergency", que APAGA los motores y el dron cae; por eso se pide
 * confirmacion. El boton se DESHABILITA en modo prueba (con protectores y poca
 * altura no hay peligro y se evita danar el dron).
 *
 * MODO TEST: muestra el escenario de prueba y un boton para alternar entre
 * el escenario 1 (rack) y el escenario 2 (techo de vigas). El boton de
 * alternancia esta oculto por defecto y solo aparece con el modo test activo.
 *
 * MODO MANUAL: oculta hologramas; el canvas del mando izquierdo muestra
 * bateria, despegue y retorno al modo ruta.
 *
 * IDIOMA: los textos generados por codigo (toggles y escenario) se obtienen
 * de GestorIdioma por clave y se repintan al cambiar idioma. La bateria
 * (numero con sufijo) no se traduce.
 * ========================================================================
 */
public class GestorEstadoDron : MonoBehaviour
{
    public enum EstadoDron
    {
        Desconectado,
        Tierra,
        EditandoRutas,
        ConfirmandoEjecucion,
        EnVuelo,
        ConfirmandoParadaEmergencia,
        EsperandoDecisionWaypoint,
        CorrigiendoPosicion,
        ConfirmandoAterrizajeVertical,
        Aterrizando,
        ModoManual
    }

    [Header("Estado del Sistema")]
    [SerializeField] private EstadoDron estadoActual = EstadoDron.Desconectado;

    [Header("Conexiones Arquitectonicas")]
    [SerializeField] private EjecutorMisionFisica ejecutorMision;
    [SerializeField] private ControladorCorreccion controladorCorreccion;
    [SerializeField] private ControladorModoManual controladorManual;

    [Header("Control Visual de Hologramas")]
    [SerializeField] private RouteManager gestorRutas;

    [Header("Modo Test")]
    [SerializeField] private ConstructorInstalacion constructorInstalacion;
    [SerializeField] private TextMeshProUGUI textoBotonModoTest;
    [Tooltip("Boton para alternar entre escenario 1 y 2. Oculto por defecto; aparece con el modo test.")]
    [SerializeField] private GameObject botonAlternarEscenario;
    [Tooltip("Texto del boton de alternancia de escenario.")]
    [SerializeField] private TextMeshProUGUI textoBotonEscenario;

    [Header("Canvas Principal (Estatico)")]
    [SerializeField] private GameObject canvasPrincipal;
    [SerializeField] private GameObject objetoAgarraderaMenu;
    [SerializeField] private GameObject panelPrincipal;
    [SerializeField] private GameObject panelRutas;
    [SerializeField] private GameObject panelConfirmaEjecucion;

    [Header("Canvas Muneca (Dinamico)")]
    [SerializeField] private GameObject canvasMuneca;
    [SerializeField] private GameObject panelVuelo;
    [Tooltip("Panel de confirmacion 'parar motores?' (Si/No). Se muestra al pulsar la parada de emergencia.")]
    [SerializeField] private GameObject panelEmergencia;
    [SerializeField] private GameObject panelWaypoint;
    [SerializeField] private GameObject panelCorreccion;
    [SerializeField] private GameObject panelAterrizarAqui;

    [Header("Canvas Modo Manual")]
    [Tooltip("Canvas raiz del modo manual. Se activa/desactiva con el modo.")]
    [SerializeField] private GameObject canvasModoManual;
    [Tooltip("Texto del boton de modo manual en el panel principal.")]
    [SerializeField] private TextMeshProUGUI textoBotonModoManual;

    [Header("Elementos de UI Dinamica")]
    [SerializeField] private Button btnParadaEmergencia;
    [Tooltip("Boton 'Siguiente waypoint' del panel waypoint. Se desactiva al acabar la ruta.")]
    [SerializeField] private Button btnSiguienteWaypoint;

    private EstadoDron estadoAnterior = EstadoDron.EnVuelo;
    private bool modoManualActivo = false;

    // Confirmacion de parada de emergencia: si el "go" termina mientras el usuario
    // decide, se deja pendiente el menu de waypoint para que "No" lleve alli.
    private bool menuWaypointPendiente = false;
    private bool siguienteHabilitadoPendiente = false;

    private static readonly EstadoDron[] estadosCanvasPrincipal =
    {
        EstadoDron.Desconectado,
        EstadoDron.Tierra,
        EstadoDron.EditandoRutas,
        EstadoDron.ConfirmandoEjecucion
    };

    void Start()
    {
        if (botonAlternarEscenario != null) botonAlternarEscenario.SetActive(false);
        RefrescarTextosDinamicos();   // Pinta toggles y escenario en el idioma inicial
        CambiarEstado(EstadoDron.Desconectado);
    }

    void OnEnable()
    {
        if (GestorIdioma.Instancia != null)
            GestorIdioma.Instancia.OnIdiomaCambiado += RefrescarTextosDinamicos;
    }

    void OnDisable()
    {
        if (GestorIdioma.Instancia != null)
            GestorIdioma.Instancia.OnIdiomaCambiado -= RefrescarTextosDinamicos;
    }

    /// <summary>Repinta los textos generados por codigo (toggles y escenario) en el idioma actual.</summary>
    private void RefrescarTextosDinamicos()
    {
        if (textoBotonModoManual != null)
            textoBotonModoManual.text = Traducir(modoManualActivo ? "modo_manual_on" : "modo_manual_off");

        if (textoBotonModoTest != null && constructorInstalacion != null)
            textoBotonModoTest.text = Traducir(constructorInstalacion.ModoTestActivo ? "modo_prueba_on" : "modo_prueba_off");

        ActualizarTextoBotonEscenario();
    }

    private string Traducir(string clave)
    {
        return (GestorIdioma.Instancia != null) ? GestorIdioma.Instancia.Traducir(clave) : clave;
    }

    // ════════════════════════════════════════════════════════════════════
    // CAMBIO DE ESTADO
    // ════════════════════════════════════════════════════════════════════

    public void CambiarEstado(EstadoDron nuevo)
    {
        estadoActual = nuevo;

        if (objetoAgarraderaMenu != null) objetoAgarraderaMenu.SetActive(false);
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelRutas != null) panelRutas.SetActive(false);
        if (panelConfirmaEjecucion != null) panelConfirmaEjecucion.SetActive(false);
        if (panelVuelo != null) panelVuelo.SetActive(false);
        if (panelEmergencia != null) panelEmergencia.SetActive(false);
        if (panelWaypoint != null) panelWaypoint.SetActive(false);
        if (panelCorreccion != null) panelCorreccion.SetActive(false);
        if (panelAterrizarAqui != null) panelAterrizarAqui.SetActive(false);
        if (canvasModoManual != null) canvasModoManual.SetActive(false);

        if (btnParadaEmergencia != null) btnParadaEmergencia.interactable = false;

        if (estadoActual == EstadoDron.ModoManual)
        {
            if (canvasPrincipal != null) canvasPrincipal.SetActive(false);
            if (canvasMuneca != null) canvasMuneca.SetActive(false);
            if (canvasModoManual != null) canvasModoManual.SetActive(true);
            if (gestorRutas != null) gestorRutas.EstablecerVisibilidadPlanificacion(false, false);
            return;
        }

        bool usaCanvasPrincipal = System.Array.IndexOf(estadosCanvasPrincipal, estadoActual) >= 0;
        if (canvasPrincipal != null) canvasPrincipal.SetActive(usaCanvasPrincipal);
        if (canvasMuneca != null) canvasMuneca.SetActive(!usaCanvasPrincipal);

        bool mostrarRuta = estadoActual != EstadoDron.Desconectado && estadoActual != EstadoDron.Tierra;
        bool mostrarHerramientas = estadoActual == EstadoDron.EditandoRutas;
        bool enFaseDeVuelo = estadoActual == EstadoDron.EnVuelo ||
                                  estadoActual == EstadoDron.ConfirmandoParadaEmergencia ||
                                  estadoActual == EstadoDron.EsperandoDecisionWaypoint ||
                                  estadoActual == EstadoDron.CorrigiendoPosicion ||
                                  estadoActual == EstadoDron.ConfirmandoAterrizajeVertical ||
                                  estadoActual == EstadoDron.Aterrizando;

        if (gestorRutas != null)
        {
            gestorRutas.EstablecerVisibilidadPlanificacion(mostrarRuta, mostrarHerramientas);
            gestorRutas.NotificarEstadoVuelo(enFaseDeVuelo);
        }

        switch (estadoActual)
        {
            case EstadoDron.Desconectado:
            case EstadoDron.Tierra:
                if (objetoAgarraderaMenu != null) objetoAgarraderaMenu.SetActive(true);
                if (panelPrincipal != null) panelPrincipal.SetActive(true);
                break;

            case EstadoDron.EditandoRutas:
                if (objetoAgarraderaMenu != null) objetoAgarraderaMenu.SetActive(true);
                if (panelRutas != null) panelRutas.SetActive(true);
                break;

            case EstadoDron.ConfirmandoEjecucion:
                if (objetoAgarraderaMenu != null) objetoAgarraderaMenu.SetActive(true);
                if (panelConfirmaEjecucion != null) panelConfirmaEjecucion.SetActive(true);
                break;

            case EstadoDron.EnVuelo:
                if (panelVuelo != null) panelVuelo.SetActive(true);
                // La parada de emergencia (corte de motores) se DESHABILITA en modo
                // prueba: con protectores y poca altura no hay peligro y cortar
                // motores solo arriesgaria danar el dron.
                if (btnParadaEmergencia != null)
                    btnParadaEmergencia.interactable =
                        (constructorInstalacion == null || !constructorInstalacion.ModoTestActivo);
                break;

            case EstadoDron.ConfirmandoParadaEmergencia:
                if (panelEmergencia != null) panelEmergencia.SetActive(true);
                break;

            case EstadoDron.EsperandoDecisionWaypoint:
                if (panelWaypoint != null) panelWaypoint.SetActive(true);
                break;

            case EstadoDron.CorrigiendoPosicion:
                if (panelCorreccion != null) panelCorreccion.SetActive(true);
                break;

            case EstadoDron.ConfirmandoAterrizajeVertical:
                if (panelAterrizarAqui != null) panelAterrizarAqui.SetActive(true);
                break;

            case EstadoDron.Aterrizando:
                if (panelVuelo != null) panelVuelo.SetActive(true);
                if (btnParadaEmergencia != null) btnParadaEmergencia.interactable = false;
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // API PUBLICA — llamada por EjecutorMisionFisica y ControladorModoManual
    // ════════════════════════════════════════════════════════════════════

    public void MostrarMenuWaypoint()
    {
        // Si el usuario esta en la confirmacion de parada de emergencia y el "go"
        // termina mientras decide, NO se pisa el dialogo: se deja pendiente y el
        // "No" llevara al menu de waypoint (el dron ya esta suspendido en el punto).
        if (estadoActual == EstadoDron.ConfirmandoParadaEmergencia)
        {
            menuWaypointPendiente = true;
            siguienteHabilitadoPendiente = true;
            return;
        }
        if (btnSiguienteWaypoint != null) btnSiguienteWaypoint.interactable = true;
        CambiarEstado(EstadoDron.EsperandoDecisionWaypoint);
    }

    public void NotificarRutaFinalizada()
    {
        if (estadoActual == EstadoDron.ConfirmandoParadaEmergencia)
        {
            menuWaypointPendiente = true;
            siguienteHabilitadoPendiente = false;
            return;
        }
        if (btnSiguienteWaypoint != null) btnSiguienteWaypoint.interactable = false;
        CambiarEstado(EstadoDron.EsperandoDecisionWaypoint);
    }

    public void FinalizarOperacionYRegresar()
    {
        CambiarEstado(EstadoDron.Tierra);
    }

    // ════════════════════════════════════════════════════════════════════
    // EVENTOS ONCLICK (UI)
    // ════════════════════════════════════════════════════════════════════

    // ── Planificacion ─────────────────────────────────────────────────────

    public void Click_MenuRutas() => CambiarEstado(EstadoDron.EditandoRutas);
    public void Click_VolverAlMenuPrincipal() => CambiarEstado(EstadoDron.Tierra);

    public void Click_ToggleModoTest()
    {
        if (constructorInstalacion == null) return;
        bool nuevoEstado = !constructorInstalacion.ModoTestActivo;
        constructorInstalacion.SetModoTest(nuevoEstado);

        if (textoBotonModoTest != null)
            textoBotonModoTest.text = Traducir(nuevoEstado ? "modo_prueba_on" : "modo_prueba_off");

        // El boton de alternar escenario solo se muestra con el modo test activo
        if (botonAlternarEscenario != null)
            botonAlternarEscenario.SetActive(nuevoEstado);

        if (nuevoEstado) ActualizarTextoBotonEscenario();
    }

    public void Click_AlternarEscenario()
    {
        if (constructorInstalacion == null) return;
        constructorInstalacion.AlternarEscenario();
        ActualizarTextoBotonEscenario();
    }

    private void ActualizarTextoBotonEscenario()
    {
        if (textoBotonEscenario != null && constructorInstalacion != null)
            textoBotonEscenario.text = Traducir("escenario") + " " + constructorInstalacion.EscenarioActivo;
    }

    public void Click_ToggleModoManual()
    {
        modoManualActivo = !modoManualActivo;

        if (textoBotonModoManual != null)
            textoBotonModoManual.text = Traducir(modoManualActivo ? "modo_manual_on" : "modo_manual_off");

        if (modoManualActivo)
        {
            if (controladorManual != null) controladorManual.Activar();
            CambiarEstado(EstadoDron.ModoManual);
        }
        else
        {
            if (controladorManual != null) controladorManual.Desactivar();
            CambiarEstado(EstadoDron.Tierra);
        }
    }

    public void Click_VolverModoRutaDesdeModoManual()
    {
        modoManualActivo = false;
        if (textoBotonModoManual != null) textoBotonModoManual.text = Traducir("modo_manual_off");
        if (controladorManual != null) controladorManual.Desactivar();
        CambiarEstado(EstadoDron.Tierra);
    }

    public void Click_EjecutarRuta()
    {
        if (gestorRutas != null && gestorRutas.ObtenerRutaCompleta().Count > 0)
            CambiarEstado(EstadoDron.ConfirmandoEjecucion);
        else
            Debug.LogWarning("[FSM] Ruta vacia.");
    }

    public void Click_ConfirmarEjecucionSi()
    {
        CambiarEstado(EstadoDron.EnVuelo);
        if (ejecutorMision != null) ejecutorMision.IniciarMisionAutomatica();
        else Debug.LogError("[FSM] EjecutorMisionFisica no asignado.");
    }

    public void Click_ConfirmarEjecucionNo() => CambiarEstado(EstadoDron.EditandoRutas);

    // ── En vuelo ───────────────────────────────────────────────────────────

    public void Click_ParadaEmergencia()
    {
        // No se toca el dron todavia: solo se pide confirmacion. El "go" en curso
        // sigue (en SDK 1.3 no se puede interrumpir); si termina durante la
        // confirmacion, MostrarMenuWaypoint lo deja pendiente para el "No".
        estadoAnterior = estadoActual;
        menuWaypointPendiente = false;
        CambiarEstado(EstadoDron.ConfirmandoParadaEmergencia);
    }

    public void Click_ConfirmarParadaSi()
    {
        // Corte de motores (emergency). El dron cae y la mision se aborta.
        if (ejecutorMision != null) ejecutorMision.PararMotoresEmergencia();
        FinalizarOperacionYRegresar();
    }

    public void Click_ConfirmarParadaNo()
    {
        if (menuWaypointPendiente)
        {
            // El "go" termino durante la confirmacion: el dron esta suspendido en el
            // waypoint -> menu de decision.
            if (btnSiguienteWaypoint != null)
                btnSiguienteWaypoint.interactable = siguienteHabilitadoPendiente;
            menuWaypointPendiente = false;
            CambiarEstado(EstadoDron.EsperandoDecisionWaypoint);
        }
        else
        {
            // Seguia en transito -> volver al estado anterior (EnVuelo).
            CambiarEstado(estadoAnterior);
        }
    }

    // ── Menu waypoint ──────────────────────────────────────────────────────

    public void Click_WaypointSiguiente()
    {
        CambiarEstado(EstadoDron.EnVuelo);
        if (ejecutorMision != null) ejecutorMision.ContinuarSiguienteWaypoint();
        else Debug.LogError("[FSM] EjecutorMisionFisica no asignado.");
    }

    public void Click_WaypointCorregir()
    {
        CambiarEstado(EstadoDron.CorrigiendoPosicion);
        if (controladorCorreccion != null) controladorCorreccion.IniciarCorreccion();
        else Debug.LogError("[FSM] ControladorCorreccion no asignado.");
    }

    // ── Correccion ─────────────────────────────────────────────────────────

    public void Click_ConfirmarCorreccion()
    {
        CambiarEstado(EstadoDron.EnVuelo);
        if (controladorCorreccion != null) controladorCorreccion.ConfirmarCorreccion();
        else Debug.LogError("[FSM] ControladorCorreccion no asignado.");
    }

    public void Click_CancelarCorreccion()
    {
        if (controladorCorreccion != null) controladorCorreccion.CancelarCorreccion();
        CambiarEstado(EstadoDron.EsperandoDecisionWaypoint);
    }

    // ── Confirmacion de aterrizaje ─────────────────────────────────────────

    public void Click_PrepararAterrizajeVertical()
    {
        estadoAnterior = estadoActual;
        CambiarEstado(EstadoDron.ConfirmandoAterrizajeVertical);
    }

    public void Click_ConfirmarAterrizajeSi()
    {
        CambiarEstado(EstadoDron.Aterrizando);
        if (ejecutorMision != null) ejecutorMision.OrdenarAterrizajeVertical();
    }

    public void Click_CancelarAterrizaje() => CambiarEstado(estadoAnterior);
}