using UnityEngine;
using TMPro;

// HeightDisplay — muestra la altura del dron sobre el origen (en metros)
// durante la planificacion.
public class HeightDisplay : MonoBehaviour
{
    [SerializeField] private OriginPoint anclaOrigen;
    [SerializeField] private TextMeshProUGUI campoTexto;

    // Se mide desde el dron, no desde este texto (que flota en otra posicion)
    [SerializeField] private Transform dronFantasma;

    void Update()
    {
        if (anclaOrigen != null && campoTexto != null && dronFantasma != null)
        {
            float alturaActual = dronFantasma.position.y - anclaOrigen.transform.position.y;

            campoTexto.text = $"{alturaActual:F2} m";
        }
    }
}