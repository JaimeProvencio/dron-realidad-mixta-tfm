using System;
using System.Collections.Generic;
using UnityEngine;

// GestorIdioma — singleton que centraliza el idioma de la interfaz (espanol /
// ingles). Mantiene un diccionario clave -> (es, en) y expone Traducir(clave).
// Los textos fijos usan el componente TextoTraducible; los dinamicos piden la
// traduccion al regenerarse. Para anadir texto, nueva entrada en el diccionario.
public class GestorIdioma : MonoBehaviour
{
    public enum Idioma { Espanol, Ingles }

    public static GestorIdioma Instancia { get; private set; }

    [Tooltip("Idioma con el que arranca la aplicacion.")]
    [SerializeField] private Idioma idiomaActual = Idioma.Espanol;

    /// <summary>Se dispara cada vez que cambia el idioma. Los textos se refrescan aqui.</summary>
    public event Action OnIdiomaCambiado;

    private Dictionary<string, string[]> diccionario;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        InicializarDiccionario();
    }

    // --- API publica ---

    /// <summary>Devuelve el texto de la clave en el idioma actual. Si no existe, devuelve la clave.</summary>
    public string Traducir(string clave)
    {
        if (diccionario != null && diccionario.TryGetValue(clave, out string[] valores))
            return idiomaActual == Idioma.Espanol ? valores[0] : valores[1];
        return clave;
    }

    /// <summary>Alterna entre espanol e ingles. Conectar al boton de cambio de idioma.</summary>
    public void AlternarIdioma()
    {
        idiomaActual = (idiomaActual == Idioma.Espanol) ? Idioma.Ingles : Idioma.Espanol;
        OnIdiomaCambiado?.Invoke();
    }

    public void EstablecerIdioma(Idioma nuevo)
    {
        idiomaActual = nuevo;
        OnIdiomaCambiado?.Invoke();
    }

    // --- Diccionario: clave : { espanol, ingles } ---

    private void InicializarDiccionario()
    {
        diccionario = new Dictionary<string, string[]>
        {
            // ── Botones de vuelo / waypoint ──────────────────────────────
            { "volver_planificacion",   new[] { "Volver al\nmodo\nplanificacion", "Back to\nplanning\nmode" } },
            { "parada_emergencia",      new[] { "PARADA\nEMERGENCIA", "EMERGENCY\nSTOP" } },
            { "siguiente_punto",        new[] { "Siguiente\npunto de\npaso", "Next\nwaypoint" } },
            { "corregir_posicion",      new[] { "Corregir\nposicion dron", "Correct\ndrone position" } },
            { "aterrizar_vertical",     new[] { "Aterrizar\nen la vertical", "Land\nvertically" } },

            // ── Confirmaciones de aterrizaje ─────────────────────────────
            { "confirm_aterrizar_titulo", new[] { "Estas seguro de que quieres aterrizar en la vertical?", "Are you sure you want to land vertically?" } },
            { "si_aterrizar_aqui",      new[] { "Si, quiero\naterrizar aqui", "Yes, land\nhere" } },
            { "no_cancela",             new[] { "No, cancela", "No, cancel" } },

            // ── Confirmacion de parada de emergencia (corte de motores) ──
            { "confirm_parada_titulo",  new[] { "Seguro que quieres parar los motores? El dron caera.", "Cut the drone's motors? It will drop." } },
            { "si_parar_motores",       new[] { "Si, parar\nmotores", "Yes, cut\nmotors" } },

            // ── Confirmaciones de correccion ─────────────────────────────
            { "si_corregir",            new[] { "Si, quiero\nque el dron\ncorrija", "Yes, let the\ndrone\ncorrect" } },
            { "no_cancelar_correccion", new[] { "No, cancelar\ncorreccion", "No, cancel\ncorrection" } },
            { "confirm_correccion_titulo", new[] { "Confirmar correccion de posicion del dron?", "Confirm drone position correction?" } },

            // ── Menu principal / planificacion ───────────────────────────
            { "conectar",               new[] { "Conectar", "Connect" } },
            { "desconectar",            new[] { "Desconectar", "Disconnect" } },
            { "definir_ruta",           new[] { "Definir Ruta", "Define Route" } },
            { "definir_origen",         new[] { "Definir\norigen", "Set\norigin" } },
            { "crear_punto",            new[] { "Crear\npunto de paso", "Add\nwaypoint" } },
            { "borrar_ultimo_punto",    new[] { "Borrar ultimo\npunto de paso", "Delete last\nwaypoint" } },
            { "borrar_ruta",            new[] { "Borrar\nruta", "Clear\nroute" } },
            { "simular_ruta",           new[] { "Simular ruta", "Simulate route" } },
            { "visualizar_mallado",     new[] { "Visualizar\nmallado", "Show\nmesh" } },
            { "editar_mallado",         new[] { "Editar\nmallado", "Edit\nmesh" } },
            { "ejecutar_ruta",          new[] { "Ejecutar ruta", "Run route" } },
            { "volver_menu_principal",  new[] { "Volver al\nmenu principal", "Back to\nmain menu" } },

            // ── Confirmacion de ejecucion ────────────────────────────────
            { "si_ejecutar",            new[] { "Si, quiero\nejecutarla", "Yes, run it" } },
            { "confirm_ejecutar_titulo", new[] { "Estas seguro de que quieres ejecutar esta ruta?", "Are you sure you want to run this route?" } },

            // ── Toggles (frase completa con ON/OFF) ──────────────────────
            { "modo_manual_on",         new[] { "Modo manual: ON", "Manual mode: ON" } },
            { "modo_manual_off",        new[] { "Modo manual: OFF", "Manual mode: OFF" } },
            { "modo_prueba_on",         new[] { "Modo prueba: ON", "Test mode: ON" } },
            { "modo_prueba_off",        new[] { "Modo prueba: OFF", "Test mode: OFF" } },

            // ── Escenario (se concatena con el numero) ───────────────────
            { "escenario",              new[] { "Escenario", "Scenario" } },

            // ── Calibrador de origen ─────────────────────────────────────
            { "calibrador_escaneando",  new[] { "Escaneando QR...", "Scanning QR..." } },
            { "calibrador_escaneado",   new[] { "Escaneado\nVolver a escanear", "Scanned\nScan again" } },
        };
    }
}