using UnityEngine;

/*
 * ========================================================================
 * SCRIPT: HeightCanvas
 * FUNCIÓN: Módulo independiente que ancla la interfaz en el eje Y absoluto 
 * sobre un objetivo y calcula la rotación inversa para que el texto sea 
 * legible desde la perspectiva del usuario (Billboarding).
 * ========================================================================
 */
public class HeightCanvas : MonoBehaviour
{
    [Header("Referencias Espaciales")]
    [Tooltip("El objeto base (Dron) sobre el que flotará este Canvas.")]
    [SerializeField] private Transform objetivoDron;

    [Tooltip("La cámara del usuario. Se auto-asigna si se deja vacía.")]
    [SerializeField] private Transform camaraUsuario;

    [Header("Parámetros Geométricos")]
    [Tooltip("Distancia vertical absoluta (en metros) sobre el dron.")]
    [SerializeField] private float altitudAbsoluta = 0.4f;

    [Tooltip("Bloquea la inclinación vertical para que el cartel no rote hacia el suelo/techo.")]
    [SerializeField] private bool bloquearEjeVertical = false;

    void Start()
    {
        if (camaraUsuario == null && Camera.main != null)
        {
            camaraUsuario = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (objetivoDron == null || camaraUsuario == null) return;

        // 1. ANCLAJE ABSOLUTO
        // Se suma Vector3.up para ignorar la rotación local del dron.
        transform.position = objetivoDron.position + (Vector3.up * altitudAbsoluta);

        // 2. CÁLCULO DE ROTACIÓN (Billboarding Invertido)
        // Se resta la cámara a la posición actual para alejar el eje Z y evitar el efecto espejo.
        Vector3 vectorDireccion = transform.position - camaraUsuario.position;

        if (bloquearEjeVertical)
        {
            vectorDireccion.y = 0;
        }

        if (vectorDireccion != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(vectorDireccion);
        }
    }
}