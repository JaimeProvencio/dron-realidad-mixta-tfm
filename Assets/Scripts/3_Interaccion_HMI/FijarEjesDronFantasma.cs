using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: FijarEjesDronFantasma
 *
 * FUNCION:
 * Se ejecuta despues de los calculos de transformacion de Meta XR.
 * Mantiene fijos los ejes de rotacion marcados en el Inspector,
 * conservando el angulo que el objeto tenia al activarse (no lo fuerza
 * a 0). Asi el dron de planificacion puede aparecer con la rotacion del
 * waypoint y, aun bloqueando Y, no gira al manipularlo con la mano.
 *
 * Por defecto bloquea X (Pitch) y Z (Roll) para mantener el dron paralelo
 * al suelo, dejando Y (Yaw) libre.
 * ========================================================================
 */
public class FijarEjesDronFantasma : MonoBehaviour
{
    [Header("Ejes bloqueados (se mantiene el angulo inicial)")]
    [SerializeField] private bool bloquearX = true;
    [SerializeField] private bool bloquearY = false;
    [SerializeField] private bool bloquearZ = true;

    private Vector3 eulerInicial;

    void OnEnable()
    {
        eulerInicial = transform.eulerAngles;
    }

    void LateUpdate()
    {
        Vector3 eulerActual = transform.eulerAngles;

        float x = bloquearX ? eulerInicial.x : eulerActual.x;
        float y = bloquearY ? eulerInicial.y : eulerActual.y;
        float z = bloquearZ ? eulerInicial.z : eulerActual.z;

        transform.rotation = Quaternion.Euler(x, y, z);
    }
}