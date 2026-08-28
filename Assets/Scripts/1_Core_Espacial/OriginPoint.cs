using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: OriginPoint
 * FUNCIÓN: Establece el sistema de referencia (0,0,0) para el dron y 
 * gestiona la visualización del punto de despegue automático.
 * MODIFICACIÓN: Parametrización de la altura de despegue (0.9m).
 * ========================================================================
 */
public class OriginPoint : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [SerializeField] private GameObject waypointDespeguePrefab;

    [Header("Configuración Física")]
    [Tooltip("Altura real a la que se estabiliza el dron tras el comando takeoff (en metros).")]
    [SerializeField] private float alturaDespegue = 0.9f;

    private GameObject instanciaDespegue;

    void Start()
    {
        CrearPuntoDespegue();
    }

    void Update()
    {
        if (instanciaDespegue != null)
        {
            instanciaDespegue.transform.position = transform.position + new Vector3(0, alturaDespegue, 0);
            instanciaDespegue.transform.rotation = transform.rotation;
        }
    }

    private void CrearPuntoDespegue()
    {
        if (waypointDespeguePrefab != null)
        {
            instanciaDespegue = Instantiate(waypointDespeguePrefab, transform.position + new Vector3(0, alturaDespegue, 0), transform.rotation);
            instanciaDespegue.transform.SetParent(this.transform);
            instanciaDespegue.name = "Waypoint0_Despegue_Automatico";
        }
        else
        {
            Debug.LogWarning("[Origen] No se ha asignado un Prefab para el punto de despegue.");
        }
    }

    public Vector3 ObtenerPosicionDespegue()
    {
        return transform.position + new Vector3(0, alturaDespegue, 0);
    }

    public float ObtenerRotacionDespegueY()
    {
        return transform.eulerAngles.y;
    }

    public void EstablecerVisibilidadPuntoDespegue(bool visible)
    {
        if (instanciaDespegue != null)
        {
            instanciaDespegue.SetActive(visible);
        }
    }
}