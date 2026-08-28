using UnityEngine;
using TMPro;

/*
 * ========================================================================
 * SCRIPT: HeightDisplay
 * RESPONSABILIDAD: Traducir la posición Y de Unity a una lectura métrica
 * comprensible para el operador humano durante la fase de planificación.
 * ========================================================================
 */
public class HeightDisplay : MonoBehaviour
{
    [SerializeField] private OriginPoint anclaOrigen;
    [SerializeField] private TextMeshProUGUI campoTexto;

    // Referencia explícita al dron para calcular la altura 
    // ignorando la posición física flotante de este texto.
    [SerializeField] private Transform dronFantasma;

    void Update()
    {
        if (anclaOrigen != null && campoTexto != null && dronFantasma != null)
        {
            // Cálculo corregido. Mide desde la raíz del dron, no desde el texto.
            float alturaActual = dronFantasma.position.y - anclaOrigen.transform.position.y;

            campoTexto.text = $"{alturaActual:F2} m";
        }
    }
}