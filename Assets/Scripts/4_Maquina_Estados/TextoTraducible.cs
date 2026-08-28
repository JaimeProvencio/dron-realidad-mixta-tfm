using UnityEngine;
using TMPro;

/*
 * ========================================================================
 * SCRIPT: TextoTraducible
 *
 * Se coloca en cada TextMeshProUGUI FIJO de la interfaz. Indica la clave
 * de traduccion; el texto se actualiza solo al arrancar y cada vez que
 * cambia el idioma (se suscribe al evento de GestorIdioma).
 *
 * No usar en textos dinamicos (bateria, altura, toggles): esos los
 * gestionan sus propios scripts.
 * ========================================================================
 */
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextoTraducible : MonoBehaviour
{
    [Tooltip("Clave del diccionario de GestorIdioma (ej. 'definir_ruta').")]
    [SerializeField] private string clave;

    private TextMeshProUGUI texto;

    void Awake()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (GestorIdioma.Instancia != null)
        {
            GestorIdioma.Instancia.OnIdiomaCambiado += Refrescar;
            Refrescar();
        }
    }

    void OnDisable()
    {
        if (GestorIdioma.Instancia != null)
            GestorIdioma.Instancia.OnIdiomaCambiado -= Refrescar;
    }

    private void Refrescar()
    {
        if (texto != null && GestorIdioma.Instancia != null)
            texto.text = GestorIdioma.Instancia.Traducir(clave);
    }
}