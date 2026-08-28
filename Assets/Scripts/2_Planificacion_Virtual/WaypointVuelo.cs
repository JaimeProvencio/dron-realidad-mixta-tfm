using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: WaypointVuelo
 * FUNCIÓN: Altera la escala y la interactividad del punto y de todos sus 
 * hijos (ej. conos direccionales) durante el vuelo.
 * ========================================================================
 */
public class WaypointVuelo : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("Factor de escala al volar (0.2 = 20% del tamaño original)")]
    [SerializeField] private float factorEscalaVuelo = 0.2f;

    private Vector3 escalaOriginal;

    // Array para almacenar todos los colisionadores de la jerarquía
    private Collider[] colisionadores;

    void Awake()
    {
        escalaOriginal = transform.localScale;

        // Busca colisionadores en el objeto raíz y en TODOS sus hijos (incluyendo el cono)
        colisionadores = GetComponentsInChildren<Collider>(true);
    }

    /// <summary>
    /// Activa el modo de visualización de trayectoria de vuelo.
    /// </summary>
    public void EstablecerModoVuelo(bool enVuelo)
    {
        // 1. Escala (afecta automáticamente a los hijos)
        transform.localScale = enVuelo ? (escalaOriginal * factorEscalaVuelo) : escalaOriginal;

        // 2. Física (recorre toda la jerarquía)
        if (colisionadores != null)
        {
            foreach (Collider col in colisionadores)
            {
                if (col != null) col.enabled = !enVuelo;
            }
        }
    }
}