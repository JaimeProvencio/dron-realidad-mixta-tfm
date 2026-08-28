using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

/*
 * ========================================================================
 * SCRIPT: GrabadorVideoTello
 *
 * FUNCION:
 * Captura el stream de video H.264 crudo del Tello (puerto UDP 11111) y
 * lo vuelca tal cual a un fichero .h264 en el almacenamiento de las gafas.
 * NO decodifica: la conversion a .mp4 se hace despues en un PC con FFmpeg.
 *
 * FLUJO:
 *  - Activa el stream con "streamon" (via TelloUDP) y abre el puerto 11111.
 *  - Mientras graba, escribe cada paquete recibido al fichero.
 *  - Al detener, cierra el fichero y envia "streamoff".
 *
 * Disponible solo en modo planificacion. Disparo manual por boton Grabar.
 * ========================================================================
 */
public class GrabadorVideoTello : MonoBehaviour
{
    [Header("Conexiones")]
    [SerializeField] private TelloUDP redUDP;

    [Header("UI")]
    [Tooltip("Texto del boton grabar/detener (cambia segun estado).")]
    [SerializeField] private TextMeshProUGUI textoBotonGrabar;
    [Tooltip("Texto opcional de estado (ej. 'Grabando 12s').")]
    [SerializeField] private TextMeshProUGUI textoEstado;

    [Header("Configuracion de Red")]
    [Tooltip("Puerto UDP del stream de video del Tello.")]
    [SerializeField] private int puertoVideo = 11111;

    private UdpClient videoClient;
    private FileStream ficheroVideo;
    private string rutaVideoActual;
    private bool grabando = false;
    private float tiempoInicio;

    // ════════════════════════════════════════════════════════════════════
    // API PUBLICA (botones)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Boton Grabar/Detener: alterna el estado de grabacion.</summary>
    public void Click_ToggleGrabacion()
    {
        if (grabando) DetenerGrabacion();
        else IniciarGrabacion();
    }

    // ════════════════════════════════════════════════════════════════════
    // GRABACION
    // ════════════════════════════════════════════════════════════════════

    private void IniciarGrabacion()
    {
        if (redUDP == null || !redUDP.conectado)
        {
            Debug.LogWarning("[Grabador] No hay conexion con el dron.");
            return;
        }

        string marca = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        rutaVideoActual = Path.Combine(Application.persistentDataPath, $"vuelo_{marca}.h264");

        try
        {
            ficheroVideo = new FileStream(rutaVideoActual, FileMode.Create, FileAccess.Write);
        }
        catch (Exception e)
        {
            Debug.LogError("[Grabador] No se pudo crear el fichero: " + e.Message);
            return;
        }

        // 'grabando' debe estar en true ANTES de arrancar el receptor de video:
        // RecibirVideoAsync() evalua "while (grabando && ...)" nada mas iniciarse y,
        // si grabando es false, sale de inmediato y no escribe ni un byte (fichero
        // .h264 de 0 bytes). Por eso se activa aqui, antes de AbrirPuertoVideo().
        grabando = true;
        tiempoInicio = Time.time;

        // Abrir el puerto 11111 ANTES de streamon (orden del SDK 1.3: Remark4 -> Remark5):
        // asi el socket ya escucha cuando el Tello empieza a emitir y no se pierde el
        // SPS/PPS inicial. Ver A2_ANALISIS_ESCENA.md §5 (P3).
        AbrirPuertoVideo();
        redUDP.EnviarComando("streamon");

        if (textoBotonGrabar != null) textoBotonGrabar.text = "Detener";
        ActualizarEstado();
        Debug.Log($"[Grabador] Grabacion iniciada: {rutaVideoActual}");
    }

    private void DetenerGrabacion()
    {
        grabando = false;

        if (redUDP != null) redUDP.EnviarComando("streamoff");

        CerrarPuertoVideo();

        if (ficheroVideo != null)
        {
            try { ficheroVideo.Flush(); ficheroVideo.Close(); }
            catch (Exception e) { Debug.LogWarning("[Grabador] Error al cerrar fichero: " + e.Message); }
            ficheroVideo = null;
        }

        if (textoBotonGrabar != null) textoBotonGrabar.text = "Grabar";
        if (textoEstado != null) textoEstado.text = $"Guardado: {Path.GetFileName(rutaVideoActual)}";
        Debug.Log("[Grabador] Grabacion detenida.");
    }

    // ════════════════════════════════════════════════════════════════════
    // STREAM UDP (puerto 11111)
    // ════════════════════════════════════════════════════════════════════

    private void AbrirPuertoVideo()
    {
        try
        {
            videoClient = new UdpClient(puertoVideo);
            // Absorbe las rafagas de keyframe (~25 datagramas / ~35 KB) para que el SO
            // no descarte paquetes si el hilo se retrasa. Ver A2_ANALISIS_ESCENA.md §5 (P1).
            videoClient.Client.ReceiveBufferSize = 4 * 1024 * 1024; // 4 MB
            _ = RecibirVideoAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("[Grabador] No se pudo abrir el puerto de video: " + e.Message);
        }
    }

    private async Task RecibirVideoAsync()
    {
        while (grabando && videoClient != null)
        {
            try
            {
                UdpReceiveResult resultado = await videoClient.ReceiveAsync();
                if (ficheroVideo != null && grabando)
                    ficheroVideo.Write(resultado.Buffer, 0, resultado.Buffer.Length);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Grabador] Error recibiendo video: " + e.Message);
            }
        }
    }

    private void CerrarPuertoVideo()
    {
        if (videoClient != null)
        {
            try { videoClient.Close(); ((IDisposable)videoClient).Dispose(); }
            catch (Exception e) { Debug.LogWarning("[Grabador] Error al cerrar puerto: " + e.Message); }
            videoClient = null;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // UTILIDADES
    // ════════════════════════════════════════════════════════════════════

    private void ActualizarEstado()
    {
        if (textoEstado == null) return;
        float segundos = Time.time - tiempoInicio;
        textoEstado.text = $"Grabando {segundos:F0}s";
    }

    private void OnDestroy() => CerrarPuertoVideo();
    private void OnApplicationQuit()
    {
        if (grabando) DetenerGrabacion();
    }
}