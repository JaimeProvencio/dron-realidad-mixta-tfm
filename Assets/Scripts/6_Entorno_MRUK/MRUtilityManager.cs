using UnityEngine;
using System.Collections;
using Meta.XR.MRUtilityKit;

// MRUtilityManager — controla la visualizacion de la malla espacial de MRUK,
// lanza la herramienta nativa de escaneo y asigna las capas fisicas buscando
// en la jerarquia de la habitacion.
public class MRUtilityManager : MonoBehaviour
{
    private bool recargaPendiente = false;

    // Controla el estado visual de los renderizadores. Oculto por defecto.
    private bool mallaVisible = false;

    void Start()
    {
        if (MRUK.Instance != null)
        {
            // Se escuchan creacion y actualizacion de la habitacion
            MRUK.Instance.RoomCreatedEvent.AddListener(AlDetectarHabitacion);
            MRUK.Instance.RoomUpdatedEvent.AddListener(AlDetectarHabitacion);
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

    // Muestra u oculta la malla de la habitacion activa de MRUK.
    public void AlternarMallaVisual()
    {
        mallaVisible = !mallaVisible;

        if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
        {
            MRUKRoom habitacionActual = MRUK.Instance.GetCurrentRoom();
            Renderer[] mallasGeneradas = habitacionActual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer malla in mallasGeneradas)
                malla.enabled = mallaVisible;
        }
        else
        {
            Debug.LogWarning("[MR] No hay una habitacion activa de MRUK para mostrar la malla.");
        }
    }

    public void IniciarCapturaEscena()
    {
        recargaPendiente = true;
        OVRScene.RequestSpaceSetup();
    }

    private void OnApplicationPause(bool pausa)
    {
        if (!pausa && recargaPendiente)
        {
            recargaPendiente = false;

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
            StartCoroutine(RutinaEnrutarCapa(habitacionGenerada));
    }

    private IEnumerator RutinaEnrutarCapa(MRUKRoom habitacionGenerada)
    {
        // Espera 1 s para que Meta termine de crear los componentes antes de recorrerlos
        yield return new WaitForSeconds(1.0f);

        // Fisica: mover los colliders de la habitacion a la capa que usa el validador
        int capaEntorno = LayerMask.NameToLayer("EntornoReal");
        if (capaEntorno != -1)
        {
            Collider[] colisionadoresHabitacion = habitacionGenerada.GetComponentsInChildren<Collider>(true);
            foreach (Collider colisionador in colisionadoresHabitacion)
                colisionador.gameObject.layer = capaEntorno;
        }
        else
        {
            Debug.LogError("[MR] La capa 'EntornoReal' no existe en Tags & Layers de Unity.");
        }

        // Visual: la geometria nueva respeta el estado de visibilidad actual de la UI
        Renderer[] mallasNuevas = habitacionGenerada.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer malla in mallasNuevas)
            malla.enabled = mallaVisible;
    }
}