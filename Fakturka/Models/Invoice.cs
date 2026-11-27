using Fakturka.Logic;

namespace Fakturka.Models;

/// <summary>
/// Faktura: odběratel, data a položky v poli s pevnou kapacitou.
/// </summary>
public class Invoice
{
    public const int MaxLines = 20;

    // Sloupce rekapitulace DPH ve 2D poli vraceném metodou VatSummary
    public const int ColumnBase = 0;
    public const int ColumnVat = 1;
    public const int ColumnTotal = 2;

    /// <summary>Číslo faktury ve tvaru RRRRNNN (2026001). Slouží i jako variabilní symbol.</summary>
    public int Number { get; set; }

    public DateTime IssueDate { get; set; }

    /// <summary>Datum uskutečnění zdanitelného plnění (DUZP).</summary>
    public DateTime TaxableDate { get; set; }

    public DateTime DueDate { get; set; }

    public Party Customer { get; }

    /// <summary>True, když dodavatel byl při vystavení plátce DPH a faktura obsahuje DPH.</summary>
    public bool WithVat { get; }

    // Obsazené jsou jen indexy 0 až LineCount - 1
    private readonly InvoiceLine[] lines = new InvoiceLine[MaxLines];

    public int LineCount { get; private set; }

    public bool IsFull => LineCount == MaxLines;

    public Invoice(Party customer, DateTime issueDate, bool withVat)
    {
        Customer = customer;
        IssueDate = issueDate;
        TaxableDate = issueDate;   // e-shop dodává hned, DUZP je proto stejné jako datum vystavení
        DueDate = issueDate;
        WithVat = withVat;
    }

    /// <summary>
    /// Přidá položku na konec. Vrátí false, když je faktura plná.
    /// </summary>
    public bool AddLine(InvoiceLine line)
    {
        if (IsFull)
        {
            return false;
        }

        lines[LineCount] = line;
        LineCount++;
        return true;
    }

    /// <summary>
    /// Vrátí položku podle indexu v poli (od 0).
    /// </summary>
    public InvoiceLine GetLine(int index)
    {
        if (index < 0 || index >= LineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Položka s indexem {index} na faktuře není.");
        }

        return lines[index];
    }

    /// <summary>
    /// Odebere položku a ostatní posune, aby v poli nezůstala díra.
    /// </summary>
    public void RemoveLine(int index)
    {
        if (index < 0 || index >= LineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Položka s indexem {index} na faktuře není.");
        }

        // Každou položku za odebranou posuneme o jedno místo doleva:
        // [A, B, C, D], odebrání B (index 1) -> [A, C, D, D] a počet 3, takže poslední D se už nepočítá
        for (int i = index; i < LineCount - 1; i++)
        {
            lines[i] = lines[i + 1];
        }

        LineCount--;
    }

    /// <summary>
    /// Najde položku se stejným názvem, cenou a sazbou. Vrátí její index, nebo -1.
    /// </summary>
    public int FindLine(string name, decimal unitPrice, decimal vatRate)
    {
        for (int i = 0; i < LineCount; i++)
        {
            if (lines[i].Name == name && lines[i].UnitPrice == unitPrice && lines[i].VatRate == vatRate)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Součet základů všech položek (cena bez DPH).
    /// </summary>
    public decimal TotalBase()
    {
        decimal sum = 0;
        for (int i = 0; i < LineCount; i++)
        {
            sum += lines[i].Base();
        }

        return sum;
    }

    /// <summary>
    /// Rekapitulace DPH: řádek = sazba z VatRates.All, sloupce = základ, DPH, celkem.
    /// </summary>
    public decimal[,] VatSummary()
    {
        decimal[,] summary = new decimal[VatRates.All.Length, 3];

        // 1. Sečteme základy položek do řádku podle jejich sazby
        for (int i = 0; i < LineCount; i++)
        {
            int rate = VatRates.IndexOf(lines[i].VatRate);
            summary[rate, ColumnBase] += lines[i].Base();
        }

        // 2. Pro každou sazbu spočítáme daň ze součtu základů. Neplátce DPH daň nepočítá.
        for (int rate = 0; rate < VatRates.All.Length; rate++)
        {
            decimal vat = 0;
            if (WithVat)
            {
                // AwayFromZero = obchodní zaokrouhlení na haléře (pětka nahoru)
                vat = Math.Round(summary[rate, ColumnBase] * VatRates.All[rate] / 100, 2, MidpointRounding.AwayFromZero);
            }

            summary[rate, ColumnVat] = vat;
            summary[rate, ColumnTotal] = summary[rate, ColumnBase] + vat;
        }

        return summary;
    }

    /// <summary>
    /// Celková částka před zaokrouhlením: součet sloupce „celkem“ v rekapitulaci.
    /// </summary>
    public decimal TotalBeforeRounding()
    {
        decimal[,] summary = VatSummary();
        decimal total = 0;
        for (int rate = 0; rate < summary.GetLength(0); rate++)
        {
            total += summary[rate, ColumnTotal];
        }

        return total;
    }

    /// <summary>
    /// Částka k úhradě zaokrouhlená na celé koruny.
    /// </summary>
    public decimal TotalToPay()
    {
        return Math.Round(TotalBeforeRounding(), 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Rozdíl mezi částkou k úhradě a přesným součtem (vychází od -0,50 do +0,50 Kč).
    /// </summary>
    public decimal Rounding()
    {
        return TotalToPay() - TotalBeforeRounding();
    }
}
