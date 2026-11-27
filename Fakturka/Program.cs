using System.Globalization;
using System.Text;
using Fakturka.Logic;
using Fakturka.Models;
using Fakturka.Output;
using Fakturka.Ui;

// Fakturka - fakturační nástroj pro malý e-shop
// Semestrální projekt předmětu IS 2026 (EF TUL), Daniel Čeřovský

// Česká kultura zajistí desetinnou čárku, mezery mezi tisíci a český formát data
// na každém počítači, ať má systém nastavený jakýkoli jazyk
CultureInfo.CurrentCulture = new CultureInfo("cs-CZ");

// UTF-8 kvůli diakritice a rámečkům i ve starších konzolích na Windows
Console.OutputEncoding = Encoding.UTF8;

// Načtení uložených dat. Při prvním spuštění soubory ještě neexistují a začne se s prázdnými daty.
Catalog catalog = new Catalog();
Storage.LoadCatalog(catalog, out string? catalogWarning);
Party? supplier = Storage.LoadSupplier(out string? supplierWarning);
InvoiceBook book = new InvoiceBook();
Storage.LoadInvoices(book, out string? invoicesWarning);

// Problémy se soubory ukážeme hned po startu, ať uživatel ví, že část dat chybí
if (supplierWarning != null || catalogWarning != null || invoicesWarning != null)
{
    ConsoleUi.PrintBanner();
    ConsoleUi.PrintTitle("Načtení uložených dat");
    if (supplierWarning != null)
    {
        ConsoleUi.PrintError(supplierWarning);
    }

    if (catalogWarning != null)
    {
        ConsoleUi.PrintError(catalogWarning);
    }

    if (invoicesWarning != null)
    {
        ConsoleUi.PrintError(invoicesWarning);
    }

    ConsoleUi.WaitForEnter();
}

// Při zapnutí ukázkového režimu se sem odloží skutečná data a při vypnutí se vrátí
Party? realSupplier = supplier;
Catalog realCatalog = catalog;
InvoiceBook realBook = book;

// Hlavní řídicí cyklus: po každé akci se menu ukáže znovu, dokud uživatel nezvolí 0.
// Tím je splněný požadavek na opakované spuštění bez ukončení programu.
int choice;
do
{
    ConsoleUi.PrintBanner();
    PrintMainMenu(supplier, catalog, book);
    choice = ConsoleUi.ReadInt("Zvolte akci (0-6): ", 0, 6);

    // Volby 1, 4 a 5 ukládají do souborů, proto v ukázkovém režimu nejsou dostupné
    switch (choice)
    {
        case 1:
            if (DemoData.IsActive)
            {
                ShowBlockedInDemo("Vystavit fakturu");
            }
            else
            {
                InvoiceScreen.Show(book, catalog, supplier);
            }

            break;
        case 2:
            InvoiceListScreen.Show(book, supplier);
            break;
        case 3:
            MonthlyReportScreen.Show(book);
            break;
        case 4:
            if (DemoData.IsActive)
            {
                ShowBlockedInDemo("Katalog zboží");
            }
            else
            {
                CatalogScreen.Show(catalog);
            }

            break;
        case 5:
            if (DemoData.IsActive)
            {
                ShowBlockedInDemo("Údaje dodavatele");
            }
            else
            {
                supplier = SupplierScreen.Show(supplier);
            }

            break;
        case 6:
            ToggleDemo();
            break;
    }
}
while (choice != 0);

Console.WriteLine();
Console.WriteLine("Na shledanou.");


// ------------------------------------------------------------
// Lokální metody hlavního menu
// ------------------------------------------------------------

// Vypíše hlavní menu. V ukázkovém režimu jsou volby, které ukládají, šedé.
// Pod menu je stručný stav dat.
void PrintMainMenu(Party? currentSupplier, Catalog currentCatalog, InvoiceBook currentBook)
{
    bool real = !DemoData.IsActive;
    PrintMenuItem(1, "Vystavit fakturu", real);
    PrintMenuItem(2, "Přehled faktur (hledání, řazení)", true);
    PrintMenuItem(3, "Měsíční přehled tržeb a DPH", true);
    PrintMenuItem(4, "Katalog zboží", real);
    PrintMenuItem(5, "Údaje dodavatele", real);
    PrintMenuItem(6, real ? "Ukázkový režim (vygenerovaná data)" : "Vypnout ukázkový režim", true);
    Console.WriteLine();
    PrintMenuItem(0, "Konec", true);
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  Dodavatel: {(currentSupplier == null ? "zatím nevyplněný (volba 5)" : currentSupplier.Name)}");
    Console.WriteLine($"  Produktů v katalogu: {currentCatalog.Count}, vystavených faktur: {currentBook.Count}");
    Console.WriteLine(real
        ? $"  Data: {Storage.DataFolder}"
        : $"  Data: ukázková, jen v paměti (seed {DemoData.Seed}), nic se neukládá");
    Console.ResetColor();
    Console.WriteLine();
}

// Vypíše jednu položku menu. Nedostupná položka je šedá s vysvětlením.
void PrintMenuItem(int number, string text, bool enabled)
{
    if (!enabled)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
    }

    Console.Write($"  {number}  {text}");
    if (!enabled)
    {
        Console.Write("  (ne v ukázkovém režimu)");
    }

    Console.WriteLine();
    Console.ResetColor();
}

// Obrazovka pro volbu, která v ukázkovém režimu není dostupná
void ShowBlockedInDemo(string title)
{
    ConsoleUi.PrintBanner();
    ConsoleUi.PrintTitle(title);
    ConsoleUi.PrintInfo("V ukázkovém režimu se nic nevystavuje ani neukládá.");
    ConsoleUi.PrintInfo("Ukázkový režim vypnete volbou 6 v hlavním menu.");
    ConsoleUi.WaitForEnter();
}

// Zapne, nebo vypne ukázkový režim. Skutečná data se při zapnutí odloží stranou
// a při vypnutí se vrátí, takže se s ukázkovými nikdy nesmíchají.
void ToggleDemo()
{
    if (DemoData.IsActive)
    {
        supplier = realSupplier;
        catalog = realCatalog;
        book = realBook;
        DemoData.IsActive = false;
        return;
    }

    realSupplier = supplier;
    realCatalog = catalog;
    realBook = book;
    DemoScreen.Start(ref supplier, ref catalog, ref book);
}
