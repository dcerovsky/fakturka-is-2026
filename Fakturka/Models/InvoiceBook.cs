namespace Fakturka.Models;

/// <summary>
/// Evidence vystavených faktur v poli s pevnou kapacitou.
/// </summary>
public class InvoiceBook
{
    public const int Capacity = 1000;

    // Číslo faktury = rok x 1000 + pořadí v roce, takže za rok jde vystavit nejvýš 999 faktur
    public const int MaxSequence = 999;

    // Obsazené jsou jen indexy 0 až Count - 1
    private readonly Invoice[] invoices = new Invoice[Capacity];

    public int Count { get; private set; }

    public bool IsFull => Count == Capacity;

    /// <summary>
    /// Přidá fakturu na konec evidence. Vrátí false, když je evidence plná.
    /// </summary>
    public bool Add(Invoice invoice)
    {
        if (IsFull)
        {
            return false;
        }

        invoices[Count] = invoice;
        Count++;
        return true;
    }

    /// <summary>
    /// Odebere naposledy přidanou fakturu. Slouží jen k vrácení změny,
    /// když se nový stav evidence nepodaří uložit do souboru.
    /// </summary>
    public void RemoveLast()
    {
        if (Count > 0)
        {
            Count--;
        }
    }

    public Invoice Get(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Faktura s indexem {index} v evidenci není.");
        }

        return invoices[index];
    }

    /// <summary>
    /// Najde fakturu podle čísla (lineární vyhledávání). Vrátí její index, nebo -1.
    /// </summary>
    public int FindByNumber(int number)
    {
        for (int i = 0; i < Count; i++)
        {
            if (invoices[i].Number == number)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Číslo pro další fakturu v daném roce, nebo -1, když v roce už čísla došla.
    /// </summary>
    public int NextNumber(int year)
    {
        // Hledání maxima jako ve cvičení #8, jen mezi fakturami daného roku.
        // Celočíselné dělení 1000 dá z čísla 2026014 rok 2026, zbytek (modulo) pořadí 14.
        int maxSequence = 0;
        for (int i = 0; i < Count; i++)
        {
            int number = invoices[i].Number;
            if (number / 1000 == year && number % 1000 > maxSequence)
            {
                maxSequence = number % 1000;
            }
        }

        if (maxSequence >= MaxSequence)
        {
            return -1;
        }

        return year * 1000 + maxSequence + 1;
    }
}
