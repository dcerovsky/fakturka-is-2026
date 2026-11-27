using Fakturka.Models;

namespace Fakturka.Ui;

/// <summary>
/// Měsíční přehled tržeb a DPH za zvolený rok: tabulka po měsících a sazbách a sloupcový graf.
/// </summary>
public static class MonthlyReportScreen
{
    // Rozsah roku, na který se jde zeptat. Čísla faktur RRRRNNN počítají se čtyřmístným rokem.
    private const int MinYear = 2000;
    private const int MaxYear = 2099;

    /// <summary>
    /// Zeptá se na rok a vypíše měsíční tabulku a graf tržeb.
    /// </summary>
    public static void Show(InvoiceBook book)
    {
        ConsoleUi.PrintBanner();
        ConsoleUi.PrintTitle("Měsíční přehled tržeb a DPH");

        if (book.Count == 0)
        {
            ConsoleUi.PrintInfo("Zatím není vystavená žádná faktura (volba 1 v hlavním menu).");
            ConsoleUi.WaitForEnter();
            return;
        }

        // Nabídneme rok nejnovější faktury, to je skoro vždy ten, který uživatel chce
        int defaultYear = book.LatestYear();
        // Rok bez mezery mezi tisíci, proto vlastní hláška místo obecné („od 2 000 do 2 099“)
        int year = ConsoleUi.ReadIntWithDefault($"Rok ({MinYear}-{MaxYear}, Enter = {defaultYear}): ",
            MinYear, MaxYear, defaultYear, $"Zadejte rok od {MinYear} do {MaxYear}, nebo jen Enter pro {defaultYear}.");

        int[] counts = book.MonthlyCounts(year);
        int total = 0;
        for (int month = 0; month < counts.Length; month++)
        {
            total += counts[month];
        }

        if (total == 0)
        {
            ConsoleUi.PrintInfo($"V roce {year} není žádná faktura.");
            ConsoleUi.WaitForEnter();
            return;
        }

        // Dvě 2D pole [měsíc, sazba]: základy a DPH
        decimal[,] bases = book.MonthlySummary(year, Invoice.ColumnBase);
        decimal[,] vat = book.MonthlySummary(year, Invoice.ColumnVat);

        ConsoleUi.PrintBanner();
        ConsoleUi.PrintTitle($"Měsíční přehled tržeb a DPH, rok {year}");
        Console.WriteLine("  Částky v Kč. Tržby jsou bez DPH, měsíc určuje datum zdanitelného plnění.");
        Console.WriteLine();
        ConsoleViews.PrintMonthlyTable(counts, bases, vat);

        Console.WriteLine();
        Console.WriteLine();
        ConsoleUi.PrintTitle("Tržby po měsících");
        ConsoleViews.PrintMonthlyChart(counts, bases);

        ConsoleUi.WaitForEnter();
    }
}
