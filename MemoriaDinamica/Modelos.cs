using System.Runtime.CompilerServices;

namespace MemoriaDinamica;

/// <summary>
/// Vector dinámico propio (similar a List&lt;int&gt;): cuando se llena,
/// reserva un arreglo nuevo en el Heap del doble de tamaño y copia los datos.
/// </summary>
public class VectorDinamico
{
    private int[] _datos;

    public int Count { get; private set; }
    public int Capacity => _datos.Length;

    public VectorDinamico(int capacidadInicial = 4)
    {
        _datos = new int[Math.Max(1, capacidadInicial)];
    }

    /// <summary>Agrega un valor. Devuelve true si fue necesario redimensionar.</summary>
    public bool Add(int valor)
    {
        bool redimensionado = false;
        if (Count == _datos.Length)
        {
            Redimensionar(_datos.Length * 2);
            redimensionado = true;
        }
        _datos[Count++] = valor;
        return redimensionado;
    }

    public void Redimensionar(int nuevaCapacidad)
    {
        nuevaCapacidad = Math.Max(1, nuevaCapacidad);
        var nuevo = new int[nuevaCapacidad];              // nuevo bloque en el Heap
        Array.Copy(_datos, nuevo, Math.Min(Count, nuevaCapacidad));
        _datos = nuevo;                                    // la referencia apunta al bloque nuevo
        if (Count > nuevaCapacidad) Count = nuevaCapacidad; // el bloque viejo queda para el GC
    }

    public void RecortarExceso() => Redimensionar(Count);

    public int[] ToArray()
    {
        var copia = new int[Count];
        Array.Copy(_datos, copia, Count);
        return copia;
    }

    public string Id => Utilidades.Id(_datos);
}

/// <summary>Tipo por referencia: las instancias viven en el Heap.</summary>
public class Persona
{
    public string Nombre { get; set; }
    public int Edad { get; set; }

    public Persona(string nombre, int edad)
    {
        Nombre = nombre;
        Edad = edad;
    }

    public Persona Clonar() => new(Nombre, Edad);

    public override string ToString() => $"{Nombre} ({Edad} años)  @0x{Utilidades.Id(this)}";
}

/// <summary>Nodo de lista enlazada: cada nodo es un objeto en el Heap que referencia al siguiente.</summary>
public class Nodo
{
    public int Valor { get; set; }
    public Nodo? Siguiente { get; set; }

    public Nodo(int valor) => Valor = valor;
}

/// <summary>Tipo por valor (se copia completo al asignar).</summary>
public struct PuntoValor
{
    public int X;
}

/// <summary>Tipo por referencia (al asignar solo se copia la referencia).</summary>
public class PuntoReferencia
{
    public int X;
}

public static class Utilidades
{
    /// <summary>Identificador del objeto (no es una dirección real, pero distingue instancias).</summary>
    public static string Id(object o) => RuntimeHelpers.GetHashCode(o).ToString("X8");
}
