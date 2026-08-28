using UnityEngine;
using System.Collections;
using Meta.XR.MRUtilityKit;

/*
 * ========================================================================
 * SCRIPT: MRUtilityManager
 * RESPONSABILIDAD: Controlar la visualización de la malla espacial,
 * llamar a la herramienta nativa de escaneo y enrutar las capas físicas
 * buscando directamente en la jerarquía de la habitación dinámica.
 * ========================================================================
 */
public class MRUtilityManager : MonoBehaviour
{
    private bool recargaPendiente = false;

    // Controla el estado visual de los renderizadores. Oculto por defecto.
    private bool mallaVisible = false;

    void Start()
    {
        if (MRUK.Instance != null)
        {
            // Suscripción dual para capturar tanto creaciones iniciales como modificaciones.
            MRUK.Instance.RoomCreatedEvent.AddListener(AlDetectarHabitacion);
            MRUK.Instance.RoomUpdatedEvent.AddListener(AlDetectarHabitacion);
            Debug.Log("[MR] Suscrito con éxito a eventos nativos de creación y actualización.");
        }
    }

    void OnDestroy()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.RemoveListener(AlDetectarHabitacion);
            MRUK.Instance.RoomUpdatedEvent.RemoveListener(AlDetectarHabitacion);
        }
    }

    /// <summary>
    /// Muestra u oculta la representación poligonal buscando los renderizadores
    /// directamente en el objeto de la habitación activa de MRUK.
    /// </summary>
    public void AlternarMallaVisual()
    {
        mallaVisible = !mallaVisible;

        if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
        {
            // Búsqueda dirigida a la habitación real instanciada en memoria
            MRUKRoom habitacionActual = MRUK.Instance.GetCurrentRoom();
            Renderer[] mallasGeneradas = habitacionActual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer malla in mallasGeneradas)
            {
                malla.enabled = mallaVisible;
            }
            Debug.Log($"[MR] Estado visual actualizado a: {mallaVisible}. Renderizadores afectados: {mallasGeneradas.Length}");
        }
        else
        {
            Debug.LogWarning("[MR] Error: No se ha detectado una habitación activa en el entorno para manipular.");
        }
    }

    public void IniciarCapturaEscena()
    {
        Debug.Log("[MR] Solicitando al SO la utilidad de escaneo espacial...");
        recargaPendiente = true;
        OVRScene.RequestSpaceSetup();
    }

    private void OnApplicationPause(bool pausa)
    {
        if (!pausa && recargaPendiente)
        {
            recargaPendiente = false;
            Debug.Log("[MR] SO cerrado. Recargando datos físicos de la habitación...");

            if (MRUK.Instance != null)
            {
                MRUK.Instance.LoadSceneFromDevice();
            }
            else
            {
                Debug.LogError("[MR] No se encontró una instancia activa de MRUK para recargar.");
            }
        }
    }

    private void AlDetectarHabitacion(MRUKRoom habitacionGenerada)
    {
        if (habitacionGenerada != null)
        {
            Debug.Log($"[MR] Habitación '{habitacionGenerada.name}' detectada. Iniciando auditoría física y visual...");
            StartCoroutine(RutinaEnrutarCapa(habitacionGenerada));
        }
    }

    private IEnumerator RutinaEnrutarCapa(MRUKRoom habitacionGenerada)
    {
        // Retraso de 1 segundo para garantizar que Meta procese la inserción de componentes físicos y visuales antes de iterar.
        yield return new WaitForSeconds(1.0f);

        // 1. GESTIÓN FÍSICA (Colisiones del Validador)
        int capaEntorno = LayerMask.NameToLayer("EntornoReal");
        if (capaEntorno != -1)
        {
            Collider[] colisionadoresHabitacion = habitacionGenerada.GetComponentsInChildren<Collider>(true);
            int conteoExitoso = 0;

            foreach (Collider colisionador in colisionadoresHabitacion)
            {
                colisionador.gameObject.layer = capaEntorno;
                conteoExitoso++;
            }
            Debug.Log($"[MR] Arquitectura física asegurada: {conteoExitoso} elementos movidos a EntornoReal.");
        }
        else
        {
            Debug.LogError("[MR] ERROR: La capa 'EntornoReal' no existe en los Tags & Layers de Unity.");
        }

        // 2. GESTIÓN VISUAL (Sincronización de la malla con el estado de la UI)
        // Se busca directamente dentro de 'habitacionGenerada' garantizando que el array devuelva los datos correctos
        Renderer[] mallasNuevas = habitacionGenerada.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer malla in mallasNuevas)
        {
            // Fuerza a la nueva geometría a respetar el estado actual de visibilidad
            malla.enabled = mallaVisible;
        }
        Debug.Log($"[MR] Sincronización visual completada. Renderizadores interceptados: {mallasNuevas.Length}. Visibilidad actual: {mallaVisible}");
    }
}