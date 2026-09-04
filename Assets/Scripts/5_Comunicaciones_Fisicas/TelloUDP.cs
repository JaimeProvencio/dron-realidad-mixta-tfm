using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// TelloUDP — gestion asincrona de comandos por UDP y liberacion de puertos.
// Reconexion: el UDP no detecta la caida del dron (es sin conexion), asi que
// 'conectado' puede quedar en true tras apagarse el Tello. Por eso el boton
// Conectar llama a Reconectar() (cierra y reabre el socket), no a
// InicializarSocket() (que aborta si cree que ya esta conectado).
public class TelloUDP : MonoBehaviour
{
    private UdpClient udpClient;
    private readonly string ipDron = "192.168.10.1";
    private readonly int puertoControl = 8889;

    public bool conectado = false;
    public event Action<string> OnRespuestaRecibida;

    // Hook de instrumentacion (SOLO logging): se dispara tras cada envio de
    // comando. Sin suscriptores es un no-op; no altera el flujo ni los tiempos.
    public event Action<string> OnComandoEnviado;

    private bool escuchando = false;

    // Boton Conectar: cierra cualquier socket previo y abre uno limpio.
    public void Reconectar()
    {
        CerrarConexion();
        InicializarSocket();
    }

    public void InicializarSocket()
    {
        if (conectado) return;

        Debug.Log("[Red UDP] Inicializando socket...");
        try
        {
            udpClient = new UdpClient();
            udpClient.Connect(ipDron, puertoControl);
            conectado = true;
            escuchando = true;
            _ = EscucharRespuestasAsync();

            EnviarComando("command");
        }
        catch (Exception e)
        {
            Debug.LogError("[Red UDP] Error de conexion: " + e.Message);
            conectado = false;
        }
    }

    public async void EnviarComando(string comando)
    {
        if (udpClient == null || !conectado) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(comando);
            await udpClient.SendAsync(data, data.Length);
            Debug.Log($"[Red UDP] -> ENVIADO: {comando}");
            OnComandoEnviado?.Invoke(comando);
        }
        catch (Exception e)
        {
            Debug.LogError("[Red UDP] Error de envio: " + e.Message);
        }
    }

    private async Task EscucharRespuestasAsync()
    {
        while (escuchando && udpClient != null)
        {
            try
            {
                UdpReceiveResult resultado = await udpClient.ReceiveAsync();
                string respuesta = Encoding.UTF8.GetString(resultado.Buffer).Trim().ToLower();
                Debug.Log($"[Red UDP] <- RECIBIDO: {respuesta}");
                OnRespuestaRecibida?.Invoke(respuesta);
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
                Debug.LogWarning("[Red UDP] Error de escucha: " + e.Message);
            }
        }
    }

    private void OnDisable() => CerrarConexion();
    private void OnDestroy() => CerrarConexion();
    private void OnApplicationQuit() => CerrarConexion();

    public void CerrarConexion()
    {
        escuchando = false;
        conectado = false;

        if (udpClient != null)
        {
            try
            {
                udpClient.Close();
                ((IDisposable)udpClient).Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Red UDP] Excepcion controlada al liberar socket: " + e.Message);
            }
            finally
            {
                udpClient = null;
                Debug.Log("[Red UDP] Socket destruido y puerto liberado.");
            }
        }
    }
}