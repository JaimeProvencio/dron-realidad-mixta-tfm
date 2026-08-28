
using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: WaypointDepthHelper (Versión MRUK Mesh Collision)
 * FUNCIÓN: Traza una línea blanca desde el holograma hacia la malla física.
 * ========================================================================
 */

[RequireComponent(typeof(LineRenderer))]

public class WaypointDepthHelper : MonoBehaviour

{
    [Header("Configuración de Escaneo")]
    [Tooltip("Capa donde el MRUK genera la malla física de la habitación. Usualmente es 'Default' o una capa específica creada por ti.")]
    [SerializeField] private LayerMask capaSueloFisico;

    private LineRenderer lineaPlomada;
    void Start()
    {
        lineaPlomada = GetComponent<LineRenderer>();
        lineaPlomada.positionCount = 2;
        lineaPlomada.startWidth = 0.005f;
        lineaPlomada.endWidth = 0.005f;

        // Forzar el color blanco (Requiere que el material del LineRenderer soporte vértices de color, ej: Sprites/Default)
        lineaPlomada.startColor = Color.white;
        lineaPlomada.endColor = Color.white;

        // Si el LineRenderer no tiene material asignado, inyectamos uno básico sin iluminación
        if (lineaPlomada.material == null || lineaPlomada.material.name == "Default-Material")
        {
            lineaPlomada.material = new Material(Shader.Find("Sprites/Default"));
        }

    }

    void Update()
    {
        lineaPlomada.SetPosition(0, transform.position);

        // Raycast contra la malla espacial de Meta

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f, capaSueloFisico))
        {
            lineaPlomada.SetPosition(1, hit.point);
        }
        else
        {
            lineaPlomada.SetPosition(1, transform.position + (Vector3.down * 1.5f));
        }

    }

}