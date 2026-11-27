using System.Globalization;
using Fakturka.Logic;

namespace Fakturka.Models;

/// <summary>
/// Evidence vystavených faktur v poli s pevnou kapacitou.
/// Faktury jsou v pořadí, v jakém se vystavily, tedy podle čísla.
/// </summary>
public class InvoiceBook
{
    public const int Capacity = 1000;

    // Číslo faktury = rok x 1000 + pořadí v roce, takže za rok jde vystavit nejvýš 999 faktur
    public const int MaxSequence = 999;

    // Klíče řazení pro metodu Sort
    public const int SortByDate = 0;
    public const int SortByAmount = 1;

    // Počet řádků měsíčního přehledu
    public const int MonthCount = 12;

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

    // ------------------------------------------------------------
    // Vyhledávání a řazení
    // ------------------------------------------------------------

    /// <summary>
    /// Kopie evidence jako pole přesné délky. Řazení pak mění jen kopii,
    /// evidence sama zůstane v pořadí podle čísla.
    /// </summary>
    public Invoice[] Copy()
    {
        Invoice[] copy = new Invoice[Count];
        for (int i = 0; i < Count; i++)
        {
            copy[i] = invoices[i];   // kopírují se jen odkazy, faktury samotné zůstávají jedny
        }

        return copy;
    }

    /// <summary>
    /// Najde faktury podle čísla, IČO odběratele nebo části jeho názvu (lineární vyhledávání).
    /// Vrátí pole přesně tak dlouhé, kolik faktur se našlo (i prázdné).
    /// </summary>
    public Invoice[] Search(string query)
    {
        // Dva průchody polem: první spočítá shody, abychom věděli, jak velké pole vytvořit,
        // druhý ho naplní
        int found = 0;
        for (int i = 0; i < Count; i++)
        {
            if (Matches(invoices[i], query))
            {
                found++;
            }
        }

        Invoice[] result = new Invoice[found];
        int next = 0;
        for (int i = 0; i < Count; i++)
        {
            if (Matches(invoices[i], query))
            {
                result[next] = invoices[i];
                next++;
            }
        }

        return result;
    }

    /// <summary>
    /// Odpovídá faktura hledanému textu? Číslo faktury a IČO musí sedět celé,
    /// u názvu odběratele stačí část.
    /// </summary>
    private static bool Matches(Invoice invoice, string query)
    {
        if (int.TryParse(query, out int number))
        {
            // Odběratel bez IČO má Ico = 0, hledání „0“ ho proto najít nesmí
            if (invoice.Number == number || (invoice.Customer.Ico != 0 && invoice.Customer.Ico == number))
            {
                return true;
            }
        }

        // Bez ohledu na velikost písmen a diakritiku: „novak“ najde i „Novák“, „cerny“ i „Černý“.
        // Invariantní kultura, ne česká: v češtině jsou č, ř, š, ž a ch samostatná písmena,
        // takže by „cerny“ „Černého“ nenašlo.
        CompareInfo compare = CultureInfo.InvariantCulture.CompareInfo;
        return compare.IndexOf(invoice.Customer.Name, query, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }

    /// <summary>
    /// Seřadí pole faktur (celou evidenci i výsledek hledání) bublinkovým řazením.
    /// </summary>
    /// <param name="key">SortByDate nebo SortByAmount.</param>
    /// <param name="descending">True: nejnovější nebo nejvyšší částka na začátku.</param>
    public static void Sort(Invoice[] array, int key, bool descending)
    {
        // Bubble sort: procházíme pole a prohazujeme sousedy, kteří jsou ve špatném pořadí.
        // Po každém průchodu „probublá“ na konec ten největší (u sestupného řazení nejmenší),
        // takže další průchod může skončit o jedno místo dřív.
        for (int pass = 0; pass < array.Length - 1; pass++)
        {
            bool swapped = false;

            for (int i = 0; i < array.Length - 1 - pass; i++)
            {
                int comparison = Compare(array[i], array[i + 1], key);
                bool wrongOrder = descending ? comparison < 0 : comparison > 0;

                if (wrongOrder)
                {
                    // Prohazují se jen odkazy na faktury, ne faktury samotné
                    Invoice temp = array[i];
                    array[i] = array[i + 1];
                    array[i + 1] = temp;
                    swapped = true;
                }
            }

            // Když se za celý průchod nic neprohodilo, pole už je seřazené
            if (!swapped)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Porovná dvě faktury: záporné číslo = a patří před b, 0 = stejné, kladné = a patří za b.
    /// </summary>
    private static int Compare(Invoice a, Invoice b, int key)
    {
        int result = key == SortByAmount
            ? a.TotalToPay().CompareTo(b.TotalToPay())
            : a.IssueDate.CompareTo(b.IssueDate);

        // Při shodě rozhodne číslo faktury, aby pořadí bylo pokaždé stejné
        if (result == 0)
        {
            result = a.Number.CompareTo(b.Number);
        }

        return result;
    }

    // ------------------------------------------------------------
    // Měsíční přehled
    // ------------------------------------------------------------

    /// <summary>
    /// Rok nejnovější faktury, nebo 0, když je evidence prázdná.
    /// </summary>
    public int LatestYear()
    {
        int latest = 0;
        for (int i = 0; i < Count; i++)
        {
            if (invoices[i].TaxableDate.Year > latest)
            {
                latest = invoices[i].TaxableDate.Year;
            }
        }

        return latest;
    }

    /// <summary>
    /// Počet faktur v jednotlivých měsících roku (index 0 = leden).
    /// </summary>
    public int[] MonthlyCounts(int year)
    {
        int[] counts = new int[MonthCount];
        for (int i = 0; i < Count; i++)
        {
            if (invoices[i].TaxableDate.Year == year)
            {
                counts[invoices[i].TaxableDate.Month - 1]++;
            }
        }

        return counts;
    }

    /// <summary>
    /// Měsíční součty jednoho sloupce rekapitulace DPH: řádek = měsíc (0 = leden), sloupec = sazba z VatRates.All.
    /// </summary>
    /// <param name="column">Invoice.ColumnBase (základ), Invoice.ColumnVat (DPH) nebo Invoice.ColumnTotal.</param>
    public decimal[,] MonthlySummary(int year, int column)
    {
        decimal[,] monthly = new decimal[MonthCount, VatRates.All.Length];

        for (int i = 0; i < Count; i++)
        {
            // Do kterého měsíce faktura patří, určuje u DPH datum zdanitelného plnění (DUZP)
            Invoice invoice = invoices[i];
            if (invoice.TaxableDate.Year != year)
            {
                continue;
            }

            int month = invoice.TaxableDate.Month - 1;   // leden = řádek 0

            // Sčítáme rekapitulace jednotlivých faktur, takže DPH sedí na haléř s tím, co je na fakturách
            decimal[,] summary = invoice.VatSummary();
            for (int rate = 0; rate < VatRates.All.Length; rate++)
            {
                monthly[month, rate] += summary[rate, column];
            }
        }

        return monthly;
    }
}
