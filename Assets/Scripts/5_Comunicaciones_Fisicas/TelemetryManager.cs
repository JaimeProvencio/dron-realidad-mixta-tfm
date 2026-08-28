using UnityEngine;
using TMPro;

/*
 * ========================================================================
 * SCRIPT: TelemetryManager
 * FUNCION: Consulta ciclica de bateria del Tello.
 *
 * Watchdog: si no llegan datos durante un tiempo, asume perdida de
 * conexion y muestra "N/A".
 * ========================================================================
 */
public class TelemetryManager : MonoBehaviour
{
    [SerializeField] private TelloUDP redUDP;

    [Header("Indicadores Visuales")]
    [Tooltip("Asigna aqui todos los textos de bateria de los diferentes paneles")]
    [SerializeField] private TextMeshProUGUI[] textosBateria;

    [Header("Configuracion de Red")]
    [SerializeField] private float intervalo = 5.0f;
    [Tooltip("Tiempo maximo sin recibir datos antes de asumir perdida de conexion (Segundos).")]
    [SerializeField] private float tiempoMaximoSinRespuesta = 15f;

    private float proximaVez = 0f;
    private float ultimoDatoRecibidoTime = 0f;
    private bool sdkActivado = false;
    private bool conexionPerdida = false;

    void Start()
    {
        if (redUDP != null)
            redUDP.OnRespuestaRecibida += ProcesarDato;
    }

    void Update()
    {
        // Perdida de conexion por desconexion explicita del socket
        if (redUDP == null || !redUDP.conectado)
        {
            if (!conexionPerdida)
            {
                conexionPerdida = true;
                sdkActivado = false;
                ActualizarUIBateriaDesconectada();
            }
            return;
        }

        if (!sdkActivado) return;

        // Watchdog: perdida de conexion silenciosa
        if (Time.time - ultimoDatoRecibidoTime > tiempoMaximoSinRespuesta)
        {
            Debug.LogWarning("[Telemetria] Timeout sin datos. Enlace perdido.");
            sdkActivado = false;
            ActualizarUIBateriaDesconectada();
            return;
        }

        if (Time.time > proximaVez)
        {
            redUDP.EnviarComando("battery?");
            proximaVez = Time.time + intervalo;
        }
    }

    void ProcesarDato(string respuesta)
    {
        ultimoDatoRecibidoTime = Time.time;
        conexionPerdida = false;

        if (respuesta == "ok")
        {
            if (!sdkActivado)
            {
                sdkActivado = true;
                Debug.Log("[Telemetria] Modo SDK confirmado. Iniciando ciclo de bateria.");
            }
            return;
        }

        if (int.TryParse(respuesta, out int nivel))
        {
            ActualizarUIBateria(nivel);
        }
    }

    void ActualizarUIBateria(int nivel)
    {
        Color colorBateria = Color.white;
        if (nivel <= 15) colorBateria = Color.red;
        else if (nivel <= 30) colorBateria = Color.yellow;

        AplicarTextoBateria($"{nivel}%", colorBateria);
    }

    void ActualizarUIBateriaDesconectada()
    {
        AplicarTextoBateria("N/A", Color.gray);
    }

    void AplicarTextoBateria(string textoNivel, Color color)
    {
        foreach (TextMeshProUGUI texto in textosBateria)
        {
            if (texto != null)
            {
                texto.text = textoNivel;
                texto.color = color;
            }
        }
    }

    private void OnDestroy()
    {
        if (redUDP != null) redUDP.OnRespuestaRecibida -= ProcesarDato;
    }
}