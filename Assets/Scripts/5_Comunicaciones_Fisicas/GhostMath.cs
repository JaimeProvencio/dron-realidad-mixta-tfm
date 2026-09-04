using UnityEngine;

// GhostMath — traduce coordenadas de Unity (mundo) a comandos del Tello
// (locales, en cm). El intercambio de ejes refleja que el Tello usa X adelante,
// Y derecha y Z arriba. Aplica un umbral de giro para no mandar rotaciones
// minimas (ruido).
public class GhostMath : MonoBehaviour
{
    [Header("Configuración de Hardware")]
    [SerializeField] private float offsetVisualY = 180f;

    [Header("Filtros de Estabilización")]
    [Tooltip("Ángulo mínimo requerido para enviar comando de giro. Giros menores se ignoran.")]
    [SerializeField] private float umbralGiroGrados = 5f;

    public string CalcularComandoHaciaPunto(Vector3 origen, Vector3 destino, float yawActualUnity, float velocidad)
    {
        Vector3 deltaGlobal = destino - origen;
        float anguloReferencia = yawActualUnity + offsetVisualY;

        Quaternion rotacionReferencia = Quaternion.Euler(0, anguloReferencia, 0);
        Vector3 deltaLocal = Quaternion.Inverse(rotacionReferencia) * deltaGlobal;

        float telloX = -deltaLocal.z * 100f;
        float telloY = deltaLocal.x * 100f;
        float telloZ = deltaLocal.y * 100f;

        string comando = $"go {(int)telloX} {(int)telloY} {(int)telloZ} {(int)velocidad}";
        Debug.Log($"[Math] Local: {deltaLocal} | Comando: {comando}");
        return comando;
    }

    public string CalcularComandoRotacion(float yawOrigen, float yawDestino)
    {
        float diferenciaGrados = Mathf.DeltaAngle(yawOrigen, yawDestino);

        if (Mathf.Abs(diferenciaGrados) < umbralGiroGrados)
        {
            Debug.Log($"[Math] Rotación de {diferenciaGrados}º ignorada (bajo el umbral de {umbralGiroGrados}º).");
            return string.Empty;
        }

        if (diferenciaGrados > 0)
            return $"cw {Mathf.RoundToInt(diferenciaGrados)}";
        else
            return $"ccw {Mathf.RoundToInt(Mathf.Abs(diferenciaGrados))}";
    }
}