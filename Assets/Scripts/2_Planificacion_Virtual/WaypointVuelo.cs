using UnityEngine;

// WaypointVuelo — reduce la escala del waypoint y desactiva sus colliders
// (los propios y los de sus hijos, p. ej. conos) mientras el dron vuela.
public class WaypointVuelo : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("Factor de escala al volar (0.2 = 20% del tamaño original)")]
    [SerializeField] private float factorEscalaVuelo = 0.2f;

    private Vector3 escalaOriginal;
    private Collider[] colisionadores;

    void Awake()
    {
        escalaOriginal = transform.localScale;
        // Incluye los colliders de los hijos (el true recorre la jerarquia)
        colisionadores = GetComponentsInChildren<Collider>(true);
    }

    public void EstablecerModoVuelo(bool enVuelo)
    {
        transform.localScale = enVuelo ? (escalaOriginal * factorEscalaVuelo) : escalaOriginal;

        if (colisionadores != null)
        {
            foreach (Collider col in colisionadores)
            {
                if (col != null) col.enabled = !enVuelo;
            }
        }
    }
}