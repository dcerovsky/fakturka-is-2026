namespace Fakturka.Models;

/// <summary>
/// Katalog zboží. Produkty drží v poli s pevnou kapacitou.
/// </summary>
public class Catalog
{
    public const int Capacity = 100;

    // Obsazené jsou jen indexy 0 až Count - 1, zbytek pole je prázdný (null).
    // Proto všechny cykly níže chodí jen do Count, ne do products.Length.
    private readonly Product[] products = new Product[Capacity];

    /// <summary>Počet produktů, které v katalogu opravdu jsou.</summary>
    public int Count { get; private set; }

    public bool IsFull => Count == Capacity;

    /// <summary>
    /// Přidá produkt na první volné místo. Vrátí false, když je katalog plný.
    /// </summary>
    public bool Add(Product product)
    {
        if (IsFull)
        {
            return false;
        }

        products[Count] = product;
        Count++;
        return true;
    }

    /// <summary>
    /// Vrátí produkt podle indexu v poli (od 0).
    /// </summary>
    public Product Get(int index)
    {
        // Pojistka proti chybě v programu: index mimo obsazenou část by vrátil null.
        // Uživatelské vstupy se kontrolují dřív, takže sem se běžně nedojde.
        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Produkt s indexem {index} v katalogu není.");
        }

        return products[index];
    }

    /// <summary>
    /// Najde produkt podle EAN. Vrátí jeho index, nebo -1, když takový není.
    /// </summary>
    public int FindByEan(long ean)
    {
        // Lineární vyhledávání: procházíme obsazenou část pole, dokud nenajdeme shodu
        for (int i = 0; i < Count; i++)
        {
            if (products[i].Ean == ean)
            {
                return i;
            }
        }

        return -1;
    }
}
