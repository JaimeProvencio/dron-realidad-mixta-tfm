using UnityEngine;

// DatosWaypoint — posicion y orientacion (yaw) de un punto de la ruta.
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