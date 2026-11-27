using System.Globalization;
using System.Text;
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

// Hlavní řídicí cyklus: po každé akci se menu ukáže znovu, dokud uživatel nezvolí 0.
// Tím je splněný požadavek na opakované spuštění bez ukončení programu.
int choice;
do
{
    ConsoleUi.PrintBanner();
    PrintMainMenu(supplier, catalog, book);
    choice = ConsoleUi.ReadInt("Zvolte akci (0-6): ", 0, 6);

    switch (choice)
    {
        case 1:
            InvoiceScreen.Show(book, catalog, supplier);
            break;
        case 2:
            ShowNotReady("Přehled faktur");
            break;
        case 3:
            ShowNotReady("Měsíční přehled tržeb a DPH");
            break;
        case 4:
            CatalogScreen.Show(catalog);
            break;
        case 5:
            supplier = SupplierScreen.Show(supplier);
            break;
        case 6:
            ShowNotReady("Ukázkový režim");
            break;
    }
}
while (choice != 0);

Console.WriteLine();
Console.WriteLine("Na shledanou.");


// ------------------------------------------------------------
// Lokální metody hlavního menu
// ------------------------------------------------------------

// Vypíše hlavní menu. Hotové části mají běžnou barvu textu, rozpracované jsou šedé,
// aby bylo hned vidět, co už funguje. Pod menu je stručný stav dat.
void PrintMainMenu(Party? currentSupplier, Catalog currentCatalog, InvoiceBook currentBook)
{
    PrintMenuItem(1, "Vystavit fakturu", true);
    PrintMenuItem(2, "Přehled faktur (hledání, řazení)", false);
    PrintMenuItem(3, "Měsíční přehled tržeb a DPH", false);
    PrintMenuItem(4, "Katalog zboží", true);
    PrintMenuItem(5, "Údaje dodavatele", true);
    PrintMenuItem(6, "Ukázkový režim (vygenerovaná data)", false);
    Console.WriteLine();
    PrintMenuItem(0, "Konec", true);
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  Dodavatel: {(currentSupplier == null ? "zatím nevyplněný (volba 5)" : currentSupplier.Name)}");
    Console.WriteLine($"  Produktů v katalogu: {currentCatalog.Count}, vystavených faktur: {currentBook.Count}");
    Console.WriteLine($"  Data: {Storage.DataFolder}");
    Console.ResetColor();
    Console.WriteLine();
}

// Vypíše jednu položku menu
void PrintMenuItem(int number, string text, bool ready)
{
    if (!ready)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
    }

    Console.Write($"  {number}  {text}");
    if (!ready)
    {
        Console.Write("  (připravuje se)");
    }

    Console.WriteLine();
    Console.ResetColor();
}

// Zástupná obrazovka pro části menu, které ještě nejsou naprogramované
void ShowNotReady(string title)
{
    ConsoleUi.PrintBanner();
    ConsoleUi.PrintTitle(title);
    ConsoleUi.PrintInfo("Tahle část se teprve připravuje.");
    ConsoleUi.WaitForEnter();
}
