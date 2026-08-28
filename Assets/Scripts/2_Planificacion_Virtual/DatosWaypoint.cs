using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: DatosWaypoint
 * FUNCIÓN: Estructura de datos modular. Almacena la posición espacial y 
 * la orientación requerida para cada punto de la ruta.
 * ========================================================================
 */
[System.Serializable]
public class DatosWaypoint
{
    public Vector3 posicion;
    public float rotacionY;

    public DatosWaypoint(Vector3 pos, float rotY)
    {
        posicion = pos;
        rotacionY = rotY;
    }
}