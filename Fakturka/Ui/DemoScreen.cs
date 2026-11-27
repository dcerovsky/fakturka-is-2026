using Fakturka.Logic;
using Fakturka.Models;

namespace Fakturka.Ui;

/// <summary>
/// Zapnutí ukázkového režimu: počet faktur, seed a vygenerování dat.
/// </summary>
public static class DemoScreen
{
    /// <summary>
    /// Zeptá se na počet faktur a seed a vygeneruje ukázková data. Předané proměnné
    /// přepíše ukázkovými daty, jen když uživatel nezvolí návrat.
    /// </summary>
    public static void Start(ref Party? supplier, ref Catalog catalog, ref InvoiceBook book)
    {
        ConsoleUi.PrintBanner();
        ConsoleUi.PrintTitle("Ukázkový režim");

        Console.WriteLine("  Program vygeneruje ukázkového dodavatele (čajový e-shop), katalog zboží");
        Console.WriteLine($"  a náhodné faktury za rok {DemoData.Year}. Data budou jen v paměti a nic se neuloží.");
        Console.WriteLine("  Vaše skutečná data zůstanou beze změny a vrátí se po vypnutí ukázkového");
        Console.WriteLine("  režimu (volba 6 v hlavním menu).");
        Console.WriteLine();

        int count = ConsoleUi.ReadInt($"Počet faktur (1-{DemoData.MaxInvoices}, 0 = zpět): ", 0, DemoData.MaxInvoices);
        if (count == 0)
        {
            return;
        }

        // Bez zadaného seedu ho vylosujeme, ale vypíšeme, aby šla stejná data vytvořit znovu
        int randomSeed = new Random().Next(DemoData.MaxSeed + 1);
        int seed = ConsoleUi.ReadIntWithDefault($"Seed generátoru (0-{DemoData.MaxSeed:#,0}, Enter = náhodný): ",
            0, DemoData.MaxSeed, randomSeed, $"Zadejte celé číslo od 0 do {DemoData.MaxSeed:#,0}, nebo jen Enter pro náhodný seed.");

        Party demoSupplier = DemoData.CreateSupplier();
        Catalog demoCatalog = DemoData.CreateCatalog();
        InvoiceBook demoBook = DemoData.GenerateInvoices(demoCatalog, demoSupplier, count, seed);

        supplier = demoSupplier;
        catalog = demoCatalog;
        book = demoBook;
        DemoData.Seed = seed;
        DemoData.IsActive = true;

        Console.WriteLine();
        ConsoleUi.PrintSuccess($"Ukázkový režim je zapnutý. Produktů: {demoCatalog.Count}, faktur: {demoBook.Count}, seed: {seed}.");
        ConsoleUi.PrintInfo($"Se seedem {seed} vyjdou příště stejná data.");
        ConsoleUi.WaitForEnter();
    }
}
