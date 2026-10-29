using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MemoriaDinamica;

public class MainForm : Form
{
    private readonly ToolStripStatusLabel _lblMemoria = new() { Text = "Memoria GC: ..." };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };

    public MainForm()
    {
        Text = "Memoria dinámica en C# - Arreglos y Heap";
        Size = new Size(1020, 760);
        MinimumSize = new Size(820, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CrearTabArreglos());
        tabs.TabPages.Add(CrearTabHeap());

        var status = new StatusStrip();
        status.Items.Add(_lblMemoria);

        Controls.Add(tabs);
        Controls.Add(status);

        _timer.Tick += (_, _) => ActualizarMemoria();
        _timer.Start();
        ActualizarMemoria();
    }

    private void ActualizarMemoria()
    {
        long bytes = GC.GetTotalMemory(false);
        _lblMemoria.Text = $"Memoria administrada (GC): {bytes / 1024.0:N0} KB   |   Colecciones Gen0/1/2: " +
                           $"{GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}";
    }

    // =====================================================================
    //  PESTAÑA 1: Arreglos / vectores redimensionables en tiempo de ejecución
    // =====================================================================
    private TabPage CrearTabArreglos()
    {
        var tab = new TabPage("1. Arreglos dinámicos");
        var log = NuevoLog();

        int[] arreglo = Array.Empty<int>();
        var vector = new VectorDinamico(2);
        var rnd = new Random();

        // ---- Grupo A: int[] con Array.Resize ----
        var nudTam = Nud(1, 1_000_000, 10);
        var nudNuevo = Nud(0, 1_000_000, 15);
        var chkRnd = new CheckBox { Text = "Rellenar posiciones nuevas con aleatorios", AutoSize = true, Checked = true, Margin = new Padding(3, 7, 3, 3) };

        var btnGenerar = Btn("Generar arreglo aleatorio", () =>
        {
            int n = (int)nudTam.Value;
            arreglo = new int[n];                       // tamaño decidido en tiempo de ejecución
            for (int i = 0; i < n; i++) arreglo[i] = rnd.Next(1, 101);
            Log(log, $"new int[{n}]  → {Describir(arreglo)}");
        });

        var btnResize = Btn("Redimensionar (Array.Resize)", () =>
        {
            int antes = arreglo.Length;
            string idAntes = Utilidades.Id(arreglo);
            int n = (int)nudNuevo.Value;
            Array.Resize(ref arreglo, n);               // crea un arreglo nuevo y copia
            if (chkRnd.Checked)
                for (int i = antes; i < n; i++) arreglo[i] = rnd.Next(1, 101);
            Log(log, $"Array.Resize: {antes} → {n}  (ref 0x{idAntes} → 0x{Utilidades.Id(arreglo)})");
            Log(log, "   " + Describir(arreglo));
        });

        var btnManual = Btn("Redimensionar manual (new + Array.Copy)", () =>
        {
            int antes = arreglo.Length;
            string idAntes = Utilidades.Id(arreglo);
            int n = (int)nudNuevo.Value;
            var nuevo = new int[n];
            Array.Copy(arreglo, nuevo, Math.Min(antes, n));
            arreglo = nuevo;
            if (chkRnd.Checked)
                for (int i = antes; i < n; i++) arreglo[i] = rnd.Next(1, 101);
            Log(log, $"Manual: {antes} → {n}  (ref 0x{idAntes} → 0x{Utilidades.Id(arreglo)})");
            Log(log, "   " + Describir(arreglo));
        });

        var grupoA = Grupo("Arreglo int[]  (tamaño fijo, se reemplaza al redimensionar)",
            Etiqueta("Tamaño inicial:"), nudTam, btnGenerar,
            Etiqueta("   Nuevo tamaño:"), nudNuevo, chkRnd, btnResize, btnManual);

        // ---- Grupo B: vector dinámico propio ----
        var nudValor = Nud(0, 9999, 7);

        void AgregarUno(int valor)
        {
            int capAntes = vector.Capacity;
            string idAntes = vector.Id;
            bool crecio = vector.Add(valor);
            string extra = crecio
                ? $"   ← se llenó: capacidad {capAntes} → {vector.Capacity}, nuevo bloque 0x{vector.Id} (antes 0x{idAntes})"
                : "";
            Log(log, $"Add({valor})  Count={vector.Count}  Capacity={vector.Capacity}{extra}");
        }

        var btnAdd = Btn("Agregar valor", () => AgregarUno((int)nudValor.Value));
        var btnAdd10 = Btn("Agregar 10 aleatorios", () =>
        {
            for (int i = 0; i < 10; i++) AgregarUno(rnd.Next(1, 101));
        });
        var btnRecortar = Btn("Recortar exceso (Capacity = Count)", () =>
        {
            int cap = vector.Capacity;
            vector.RecortarExceso();
            Log(log, $"RecortarExceso: capacidad {cap} → {vector.Capacity}");
        });
        var btnVer = Btn("Ver contenido", () =>
            Log(log, $"Vector: Count={vector.Count} Capacity={vector.Capacity} datos=[{Vista(vector.ToArray())}]"));
        var btnReiniciar = Btn("Reiniciar vector", () =>
        {
            vector = new VectorDinamico(2);
            Log(log, "Nuevo VectorDinamico(2) creado.");
        });
        var btnLimpiar = Btn("Limpiar registro", () => log.Clear());

        var grupoB = Grupo("Vector dinámico propio (crece al doble cuando se llena)",
            Etiqueta("Valor:"), nudValor, btnAdd, btnAdd10, btnRecortar, btnVer, btnReiniciar, btnLimpiar);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(grupoA, 0, 0);
        layout.Controls.Add(grupoB, 0, 1);
        layout.Controls.Add(log, 0, 2);
        tab.Controls.Add(layout);

        Log(log, "Tip: genera un arreglo, cambia 'Nuevo tamaño' y redimensiona. Observa cómo cambia la referencia (0x...).");
        return tab;
    }

    // =====================================================================
    //  PESTAÑA 2: Memoria dinámica (instancias y referencias en el Heap)
    // =====================================================================
    private TabPage CrearTabHeap()
    {
        var tab = new TabPage("2. Memoria dinámica (Heap)");
        var log = NuevoLog();
        var personas = new List<Persona>();
        Nodo? cabeza = null;

        // ---- Grupo A: instancias de clase ----
        var txtNombre = new TextBox { Width = 140, Text = "Ana" };
        var nudEdad = Nud(1, 120, 25);
        var lst = new ListBox { Width = 480, Height = 110, IntegralHeight = false };

        void Refrescar(int seleccion = -1)
        {
            lst.Items.Clear();
            foreach (var p in personas) lst.Items.Add(p);
            if (seleccion >= 0 && seleccion < lst.Items.Count) lst.SelectedIndex = seleccion;
        }

        bool TryObtener(out Persona persona, out int idx)
        {
            idx = lst.SelectedIndex;
            if (idx >= 0 && lst.SelectedItem is Persona p)
            {
                persona = p;
                return true;
            }
            persona = null!;
            Log(log, "⚠ Selecciona primero una persona de la lista.");
            return false;
        }

        var btnCrear = Btn("Crear instancia (new Persona)", () =>
        {
            string nombre = string.IsNullOrWhiteSpace(txtNombre.Text) ? "Sin nombre" : txtNombre.Text.Trim();
            var p = new Persona(nombre, (int)nudEdad.Value);   // objeto en el Heap
            personas.Add(p);                                    // la lista guarda otra referencia
            Refrescar(personas.Count - 1);
            Log(log, $"new Persona(\"{nombre}\", {p.Edad})  → objeto en el Heap, referencia 0x{Utilidades.Id(p)}");
        });

        var btnAlias = Btn("Alias (2ª referencia al mismo objeto)", () =>
        {
            if (!TryObtener(out var p, out int idx)) return;
            Persona alias = p;                                  // copia solo la referencia
            int antes = p.Edad;
            alias.Edad++;
            Log(log, $"Persona alias = p;  ReferenceEquals(p, alias) = {ReferenceEquals(p, alias)}");
            Log(log, $"   alias.Edad++  →  p.Edad pasó de {antes} a {p.Edad}  (es UN solo objeto en el Heap)");
            Refrescar(idx);
        });

        var btnClonar = Btn("Clonar (new independiente)", () =>
        {
            if (!TryObtener(out var p, out _)) return;
            var clon = p.Clonar();
            clon.Edad += 10;
            personas.Add(clon);
            Log(log, $"clon = p.Clonar();  ReferenceEquals = {ReferenceEquals(p, clon)}");
            Log(log, $"   clon.Edad += 10  →  original {p.Edad}, clon {clon.Edad}  (objetos distintos)");
            Refrescar(personas.Count - 1);
        });

        var btnQuitar = Btn("Quitar referencia y recolectar (GC)", () =>
        {
            if (!TryObtener(out _, out int idx)) return;
            var debil = QuitarYObtenerReferenciaDebil(personas, idx);
            Refrescar();                                        // la ListBox también guardaba referencias
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Log(log, $"Se quitó la última referencia fuerte y se llamó a GC.Collect().");
            Log(log, $"   WeakReference.IsAlive = {debil.IsAlive}  → " +
                     (debil.IsAlive ? "el GC aún no liberó el objeto" : "el GC liberó el objeto del Heap"));
        });

        var btnStruct = Btn("Struct vs Class", () =>
        {
            var v1 = new PuntoValor { X = 1 };
            var v2 = v1;                 // copia completa
            v2.X = 99;
            var r1 = new PuntoReferencia { X = 1 };
            var r2 = r1;                 // misma referencia
            r2.X = 99;
            Log(log, "Struct (valor):     v2 = v1; v2.X = 99  →  v1.X = " + v1.X + ", v2.X = " + v2.X + "  (copias independientes)");
            Log(log, "Class (referencia): r2 = r1; r2.X = 99  →  r1.X = " + r1.X + ", r2.X = " + r2.X + "  (mismo objeto)");
        });

        var grupoA = Grupo("Instancias en el Heap (clase Persona)",
            Etiqueta("Nombre:"), txtNombre, Etiqueta("Edad:"), nudEdad, btnCrear, lst,
            btnAlias, btnClonar, btnQuitar, btnStruct);

        // ---- Grupo B: lista enlazada ----
        var nudNodo = Nud(0, 9999, 5);
        var lblLista = new Label
        {
            AutoSize = false,
            Width = 940,
            Height = 44,
            Font = new Font("Consolas", 9.5f),
            Text = "cabeza → null",
            Margin = new Padding(3, 6, 3, 3)
        };

        void MostrarLista()
        {
            var sb = new StringBuilder("cabeza → ");
            for (var n = cabeza; n != null; n = n.Siguiente)
                sb.Append($"[{n.Valor} @{Utilidades.Id(n)[^4..]}] → ");
            sb.Append("null");
            lblLista.Text = sb.ToString();
        }

        var btnInicio = Btn("Insertar al inicio", () =>
        {
            var nuevo = new Nodo((int)nudNodo.Value) { Siguiente = cabeza };
            cabeza = nuevo;
            MostrarLista();
            Log(log, $"new Nodo({nuevo.Valor}) al inicio, referencia 0x{Utilidades.Id(nuevo)}");
        });

        var btnFinal = Btn("Insertar al final", () =>
        {
            var nuevo = new Nodo((int)nudNodo.Value);
            if (cabeza == null) cabeza = nuevo;
            else
            {
                var actual = cabeza;
                while (actual.Siguiente != null) actual = actual.Siguiente;
                actual.Siguiente = nuevo;
            }
            MostrarLista();
            Log(log, $"new Nodo({nuevo.Valor}) al final, referencia 0x{Utilidades.Id(nuevo)}");
        });

        var btnQuitarPrimero = Btn("Eliminar primero", () =>
        {
            if (cabeza == null) { Log(log, "⚠ La lista está vacía."); return; }
            Log(log, $"Eliminar primero: cabeza = cabeza.Siguiente. El nodo [{cabeza.Valor}] queda sin referencias → candidato a GC.");
            cabeza = cabeza.Siguiente;
            MostrarLista();
        });

        var btnVaciar = Btn("Vaciar lista", () =>
        {
            cabeza = null;
            MostrarLista();
            Log(log, "cabeza = null → toda la cadena de nodos queda inalcanzable para el GC.");
        });

        var grupoB = Grupo("Lista enlazada (cada Nodo es un objeto del Heap que apunta al siguiente)",
            Etiqueta("Valor:"), nudNodo, btnInicio, btnFinal, btnQuitarPrimero, btnVaciar, lblLista);

        // ---- Grupo C: memoria administrada vs no administrada ----
        var btnBloque = Btn("Reservar 40 MB y medir GC", () =>
        {
            long antes = GC.GetTotalMemory(true);
            long durante = ReservarBloqueGrande();
            long despues = GC.GetTotalMemory(true);
            Log(log, $"Memoria antes: {antes / 1024:N0} KB | con bloque int[10 000 000]: {durante / 1024:N0} KB | tras liberar: {despues / 1024:N0} KB");
        });

        var btnNoAdmin = Btn("Memoria no administrada (puntero IntPtr)", () =>
        {
            const int n = 5;
            IntPtr ptr = Marshal.AllocHGlobal(n * sizeof(int));  // malloc: el GC NO lo administra
            try
            {
                for (int i = 0; i < n; i++) Marshal.WriteInt32(ptr, i * sizeof(int), (i + 1) * 10);
                var leidos = new int[n];
                for (int i = 0; i < n; i++) leidos[i] = Marshal.ReadInt32(ptr, i * sizeof(int));
                Log(log, $"AllocHGlobal({n * sizeof(int)} bytes) → puntero 0x{ptr.ToInt64():X}, datos leídos: [{string.Join(", ", leidos)}]");
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);                         // hay que liberarlo manualmente
                Log(log, "   FreeHGlobal(ptr): liberado manualmente (el GC no lo haría por nosotros).");
            }
        });

        var btnGc = Btn("GC.Collect()", () =>
        {
            long antes = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long despues = GC.GetTotalMemory(true);
            Log(log, $"GC.Collect(): {antes / 1024:N0} KB → {despues / 1024:N0} KB");
            ActualizarMemoria();
        });

        var btnLimpiar = Btn("Limpiar registro", () => log.Clear());

        var grupoC = Grupo("Memoria administrada (Heap + GC) vs no administrada",
            btnBloque, btnNoAdmin, btnGc, btnLimpiar);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(grupoA, 0, 0);
        layout.Controls.Add(grupoB, 0, 1);
        layout.Controls.Add(grupoC, 0, 2);
        layout.Controls.Add(log, 0, 3);
        tab.Controls.Add(layout);

        Log(log, "Cada 'new' reserva un objeto en el Heap; las variables (Stack) solo guardan la referencia.");
        return tab;
    }

    // ---- Métodos auxiliares de memoria (NoInlining para que el GC pueda liberar de verdad) ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QuitarYObtenerReferenciaDebil(List<Persona> lista, int idx)
    {
        var p = lista[idx];
        lista.RemoveAt(idx);
        return new WeakReference(p);   // no mantiene vivo al objeto
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReservarBloqueGrande()
    {
        var bloque = new int[10_000_000];   // ~40 MB en el Large Object Heap
        bloque[0] = 1;
        return GC.GetTotalMemory(false);
    }

    // ---- Helpers de UI ----

    private static string Vista(int[] datos, int max = 30)
    {
        var parte = string.Join(", ", datos.Take(max));
        return datos.Length > max ? parte + $", ... (+{datos.Length - max} más)" : parte;
    }

    private static string Describir(int[] a) =>
        $"Length={a.Length}, ref=0x{Utilidades.Id(a)}, datos=[{Vista(a)}]";

    private static void Log(TextBox t, string linea) => t.AppendText(linea + Environment.NewLine);

    private static TextBox NuevoLog() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 9.5f),
        BackColor = Color.White,
        WordWrap = true
    };

    private static NumericUpDown Nud(int min, int max, int valor) =>
        new() { Minimum = min, Maximum = max, Value = valor, Width = 90 };

    private static Label Etiqueta(string texto) =>
        new() { Text = texto, AutoSize = true, Margin = new Padding(3, 8, 3, 3) };

    private static Button Btn(string texto, Action accion)
    {
        var b = new Button { Text = texto, AutoSize = true, Padding = new Padding(6, 2, 6, 2) };
        b.Click += (_, _) => accion();
        return b;
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // MainForm
        // 
        ClientSize = new Size(951, 335);
        Name = "MainForm";
        Load += MainForm_Load;
        ResumeLayout(false);

    }

    private static GroupBox Grupo(string titulo, params Control[] controles)
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        flow.Controls.AddRange(controles);
        var g = new GroupBox { Text = titulo, Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8, 6, 8, 8) };
        g.Controls.Add(flow);
        return g;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {

    }
}
