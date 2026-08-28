using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: InvocadorCanvasPrincipal
 *
 * Se coloca en la AGARRADERA del canvas principal (el objeto padre que ya
 * sirve para desplazar el canvas manualmente). Al pulsar el gatillo de
 * agarre del mando derecho (HandTrigger, dedo corazon/anular), teletransporta
 * la agarradera a una distancia fija delante del mando, en la direccion a
 * la que apunta. El canvas, al ser hijo, la sigue.
 *
 * Es un reposicionamiento de un toque: coloca la agarradera y la deja ahi
 * (el arrastre fino se hace luego con el gatillo de arriba sobre la barra).
 *
 * Solo actua cuando el canvas principal esta activo; en los menus de vuelo
 * o modo manual el gatillo no hace nada.
 * ========================================================================
 */
public class InvocadorCanvasPrincipal : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Canvas principal. Solo se usa para comprobar si esta activo.")]
    [SerializeField] private GameObject canvasPrincipal;

    [Tooltip("Transform del mando derecho (RightHandAnchor del OVRCameraRig).")]
    [SerializeField] private Transform mandoDerecho;

    [Header("Configuracion")]
    [Tooltip("Distancia a la que se coloca la agarradera delante del mando (metros).")]
    [SerializeField] private float distancia = 0.7f;

    [Tooltip("Umbral del gatillo de agarre para considerarlo pulsado (0-1).")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float umbralGatillo = 0.5f;

    private bool gatilloPulsadoAntes = false;

    void Update()
    {
        if (canvasPrincipal == null || mandoDerecho == null) return;

        // Solo si el canvas principal esta visible
        if (!canvasPrincipal.activeInHierarchy)
        {
            gatilloPulsadoAntes = false;
            return;
        }

        // Se consulta el mando derecho de forma EXPLICITA (RTouch). Usar el
        // mapeo combinado "Secondary" depende del estado de pareja de los
        // mandos: si el izquierdo esta dormido, el derecho no responde hasta
        // que se activa el izquierdo. Con RTouch + PrimaryHandTrigger leemos
        // el gatillo de agarre del mando derecho directamente, sin esa dependencia.
        float valorGatillo = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        bool gatilloPulsadoAhora = valorGatillo > umbralGatillo;

        // Disparo en el flanco de subida: un solo toque por pulsacion
        if (gatilloPulsadoAhora && !gatilloPulsadoAntes)
            ReposicionarAgarradera();

        gatilloPulsadoAntes = gatilloPulsadoAhora;
    }

    private void ReposicionarAgarradera()
    {
        // Se mueve la propia agarradera (este transform); el canvas hijo la sigue
        transform.position = mandoDerecho.position + mandoDerecho.forward * distancia;

        // Orientar hacia el mando para que el canvas sea legible de frente
        Vector3 direccionVista = transform.position - mandoDerecho.position;
        direccionVista.y = 0f;
        if (direccionVista != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direccionVista);
    }
}