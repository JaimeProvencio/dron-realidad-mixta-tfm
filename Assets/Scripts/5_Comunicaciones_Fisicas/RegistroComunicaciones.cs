using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// RegistroComunicaciones — instrumentacion de solo observacion. Registra en un
// CSV cada comando enviado (TX) y cada respuesta recibida (RX) del canal de
// control del Tello, con marca de tiempo de alta resolucion. De ese CSV se
// derivan offline (en PC, sin hardware) los numeros de comunicaciones del cap. 4:
// latencia UDP, tasa de comandos, timeouts y frecuencia real del "rc".
//
// No cambia el funcionamiento: escucha los eventos que TelloUDP ya expone,
// encola en un buffer en memoria y vuelca a disco fuera de la ruta caliente,
// asi no perturba la latencia ni la frecuencia que se miden.
//
// Reloj: Stopwatch (no Time.time), porque la recepcion UDP puede ocurrir fuera
// del hilo principal de Unity, donde Time.time no es valido.
// Salida: comms_AAAAMMDD_HHmmss.csv en Application.persistentDataPath (adb pull).
public class RegistroComunicaciones : MonoBehaviour
{
    [Header("Conexiones")]
    [Tooltip("Canal UDP del Tello a instrumentar. Obligatorio.")]
    [SerializeField] private TelloUDP redUDP;

    [Header("Configuracion")]
    [Tooltip("Segundos entre volcados del buffer a disco. No afecta a la medida.")]
    [SerializeField] private float intervaloVolcado = 2f;

    // Reloj de alta resolucion, seguro entre hilos (a diferencia de Time.time).
    private readonly System.Diagnostics.Stopwatch cronometro = new System.Diagnostics.Stopwatch();

    // Buffer productor/consumidor: los eventos (posible hilo secundario) encolan;
    // la corrutina de volcado (hilo principal) desencola y escribe.
    private readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();

    private string rutaFichero;
    private Coroutine volcadoCoroutine;
    private bool suscrito = false;

    // --- Ciclo de vida ---

    void OnEnable()
    {
        if (redUDP == null)
        {
            Debug.LogError("[RegistroComms] Falta la referencia a TelloUDP en el Inspector. Registro desactivado.");
            enabled = false;
            return;
        }

        string marca = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        rutaFichero = Path.Combine(Application.persistentDataPath, $"comms_{marca}.csv");

        try
        {
            File.WriteAllLines(rutaFichero, new[]
            {
                $"# sesion {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "t_ms,dir,mensaje"
            });
        }
        catch (Exception e)
        {
            Debug.LogError("[RegistroComms] No se pudo crear el CSV: " + e.Message);
            enabled = false;
            return;
        }

        cronometro.Restart();

        redUDP.OnComandoEnviado += RegistrarTX;
        redUDP.OnRespuestaRecibida += RegistrarRX;
        suscrito = true;

        volcadoCoroutine = StartCoroutine(VolcadoPeriodico());
        Debug.Log($"[RegistroComms] Registrando comunicaciones en: {rutaFichero}");
    }

    void OnDisable()
    {
        if (suscrito && redUDP != null)
        {
            redUDP.OnComandoEnviado -= RegistrarTX;
            redUDP.OnRespuestaRecibida -= RegistrarRX;
        }
        suscrito = false;

        if (volcadoCoroutine != null)
        {
            StopCoroutine(volcadoCoroutine);
            volcadoCoroutine = null;
        }

        cronometro.Stop();
        Volcar(); // volcado final: no perder los ultimos eventos del buffer
    }

    void OnApplicationQuit() => Volcar();

    // --- Captura (posible hilo secundario: solo encolar, nada de API de Unity) ---

    private void RegistrarTX(string comando) => Encolar("TX", comando);
    private void RegistrarRX(string respuesta) => Encolar("RX", respuesta);

    private void Encolar(string dir, string mensaje)
    {
        double t = cronometro.Elapsed.TotalMilliseconds;
        // Los mensajes del Tello no contienen comas; se guardan tal cual como
        // ultima columna (los espacios de "go 0 0 50 30" son validos en CSV).
        buffer.Enqueue($"{t:F1},{dir},{mensaje}");
    }

    // --- Volcado a disco (hilo principal, fuera de la ruta caliente) ---

    private IEnumerator VolcadoPeriodico()
    {
        var espera = new WaitForSeconds(intervaloVolcado);
        while (true)
        {
            yield return espera;
            Volcar();
        }
    }

    private void Volcar()
    {
        if (buffer.IsEmpty) return;

        var lineas = new List<string>();
        while (buffer.TryDequeue(out string linea))
            lineas.Add(linea);

        if (lineas.Count == 0) return;

        try
        {
            File.AppendAllLines(rutaFichero, lineas);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[RegistroComms] Error al volcar CSV: " + e.Message);
        }
    }
}
