using UnityEngine;
using Oculus.Interaction;

/*
 * ========================================================================
 * SCRIPT: TiradorRotacionY
 * FUNCION: Tirador para orientar el chasis del dron en planificacion.
 * Mientras esta agarrado, el chasis gira libremente siguiendo la posicion
 * de la mano (sin iman de retencion: cualquier giro, por pequeno que sea,
 * se refleja). El ruido de rotacion se absorbe despues, al crear el
 * waypoint (RouteManager hereda el yaw anterior si la diferencia es menor
 * que su umbral). Al soltar, el tirador vuelve a su posicion de reposo.
 * ========================================================================
 */
[RequireComponent(typeof(Grabbable))]
public class TiradorRotacionY : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform chasisVisual;
    [SerializeField] private Transform raizDron;

    [Header("Reposo")]
    [SerializeField] private float distanciaReposo = 0.35f;
    [SerializeField] private float velocidadRetorno = 10f;

    private Grabbable grabbable;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
    }

    void Update()
    {
        if (chasisVisual == null || raizDron == null) return;

        bool agarrado = (grabbable != null && grabbable.SelectingPointsCount > 0);

        if (agarrado)
        {
            // El chasis sigue libremente la direccion mano -> dron (solo en Y)
            Vector3 direccion = transform.position - raizDron.position;
            direccion.y = 0f;

            if (direccion != Vector3.zero)
            {
                float anguloReal = Quaternion.LookRotation(direccion).eulerAngles.y;
                chasisVisual.rotation = Quaternion.Euler(0f, anguloReal, 0f);
            }
        }
        else
        {
            // Retorno suave a la posicion de reposo frente al dron
            Vector3 objetivo = raizDron.position + (chasisVisual.forward * distanciaReposo);
            objetivo.y = raizDron.position.y;
            transform.position = Vector3.Lerp(transform.position, objetivo, Time.deltaTime * velocidadRetorno);
        }
    }
}