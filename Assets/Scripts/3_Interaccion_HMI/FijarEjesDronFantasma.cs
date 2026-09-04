using UnityEngine;

// FijarEjesDronFantasma — corre en LateUpdate (tras la transformacion de
// Meta XR) y mantiene fijos los ejes marcados, conservando el angulo que el
// objeto tenia al activarse (no lo fuerza a 0). Por defecto bloquea X (pitch)
// y Z (roll) para dejar el dron paralelo al suelo, con Y (yaw) libre.
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