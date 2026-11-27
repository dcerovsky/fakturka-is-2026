using Fakturka.Logic;
using Fakturka.Models;

namespace Fakturka.Ui;

/// <summary>
/// Obrazovka katalogu zboží: výpis, přidání produktu a změna ceny.
/// </summary>
public static class CatalogScreen
{
    // Logické meze vstupů. Název se na faktuře vypíše celý, v tabulce se případně zkrátí.
    private const int MaxNameLength = 40;
    private const decimal MinPrice = 0.01m;
    private const decimal MaxPrice = 1_000_000m;
    private const long MaxEan = 9_999_999_999_999;   // největší třináctimístné číslo

    /// <summary>
    /// Podmenu katalogu. Běží ve smyčce, dokud uživatel nezvolí návrat (0).
    /// </summary>
    public static void Show(Catalog catalog)
    {
        int choice;
        do
        {
            ConsoleUi.PrintBanner();
            ConsoleUi.PrintTitle("Katalog zboží");
            ConsoleViews.PrintCatalog(catalog);

            Console.WriteLine();
            Console.WriteLine("  1  Přidat produkt");
            Console.WriteLine("  2  Změnit cenu produktu");
            Console.WriteLine("  0  Zpět do hlavního menu");
            Console.WriteLine();
            choice = ConsoleUi.ReadInt("Zvolte akci (0-2): ", 0, 2);

            switch (choice)
            {
                case 1:
                    AddProduct(catalog);
                    break;
                case 2:
                    ChangePrice(catalog);
                    break;
            }
        }
        while (choice != 0);
    }

    /// <summary>
    /// Načte nový produkt, ukáže rekapitulaci a po potvrzení ho přidá do katalogu.
    /// </summary>
    private static void AddProduct(Catalog catalog)
    {
        ConsoleUi.PrintBanner();
        ConsoleUi.PrintTitle("Nový produkt");

        // Plný katalog hlídáme hned, ať uživatel zbytečně nevyplňuje údaje
        if (catalog.IsFull)
        {
            ConsoleUi.PrintError($"Katalog je plný ({Catalog.Capacity} produktů), další produkt nejde přidat.");
            ConsoleUi.WaitForEnter();
            return;
        }

        string name = ConsoleUi.ReadText($"Název (nejvýš {MaxNameLength} znaků): ", MaxNameLength);

        // Dva produkty se stejným EAN by se při vystavování faktury nedaly rozlišit.
        // EAN 0 znamená „bez kódu“ a ten mít může produktů víc, proto ho nekontrolujeme.
        long ean = ReadEan();
        int duplicate = ean == 0 ? -1 : catalog.FindByEan(ean);
        while (duplicate >= 0)
        {
            ConsoleUi.PrintError($"Tento EAN už má produkt „{catalog.Get(duplicate).Name}“.");
            ean = ReadEan();
            duplicate = ean == 0 ? -1 : catalog.FindByEan(ean);
        }

        decimal price = ConsoleUi.ReadDecimal("Cena za 1 ks bez DPH v Kč: ", MinPrice, MaxPrice);
        decimal vatRate = ReadVatRate();

        Product product = new Product(ean, name, price, vatRate);

        // Rekapitulace před uložením, aby uživatel viděl, co přesně přidává
        Console.WriteLine();
        Console.WriteLine("  ==================================");
        Console.WriteLine($"  Název:        {product.Name}");
        Console.WriteLine($"  EAN:          {product.EanText()}");
        Console.WriteLine($"  Cena bez DPH: {ConsoleViews.Money(product.UnitPrice)}");
        Console.WriteLine($"  DPH:          {product.VatRate:0} %");
        Console.WriteLine($"  Cena s DPH:   {ConsoleViews.Money(product.PriceWithVat())}");
        Console.WriteLine("  ==================================");
        Console.WriteLine();

        if (!ConsoleUi.ReadYesNo("Přidat produkt do katalogu?"))
        {
            ConsoleUi.PrintInfo("Produkt se nepřidal.");
        }
        else if (catalog.Add(product))
        {
            ConsoleUi.PrintSuccess($"Produkt „{product.Name}“ je v katalogu pod číslem {catalog.Count}.");
        }
        else
        {
            ConsoleUi.PrintError("Katalog je plný, produkt se nepřidal.");
        }

        ConsoleUi.WaitForEnter();
    }

    /// <summary>
    /// Změní cenu bez DPH u vybraného produktu.
    /// </summary>
    private static void ChangePrice(Catalog catalog)
    {
        if (catalog.Count == 0)
        {
            ConsoleUi.PrintInfo("Katalog je prázdný, není u čeho měnit cenu.");
            ConsoleUi.WaitForEnter();
            return;
        }

        int number = ConsoleUi.ReadInt($"Číslo produktu (1-{catalog.Count}): ", 1, catalog.Count);
        // V tabulce číslujeme od 1, v poli se indexuje od 0
        Product product = catalog.Get(number - 1);

        Console.WriteLine($"  {product.Name}: teď {ConsoleViews.Money(product.UnitPrice)} bez DPH");
        decimal newPrice = ConsoleUi.ReadDecimal("Nová cena za 1 ks bez DPH v Kč: ", MinPrice, MaxPrice);

        if (newPrice == product.UnitPrice)
        {
            ConsoleUi.PrintInfo("Cena je stejná jako dosud, nic se nemění.");
        }
        else if (ConsoleUi.ReadYesNo($"Změnit cenu z {ConsoleViews.Money(product.UnitPrice)} na {ConsoleViews.Money(newPrice)}?"))
        {
            product.UnitPrice = newPrice;
            ConsoleUi.PrintSuccess("Cena je změněná.");
        }
        else
        {
            ConsoleUi.PrintInfo("Cena zůstala beze změny.");
        }

        ConsoleUi.WaitForEnter();
    }

    /// <summary>
    /// Načte EAN: 0 (bez kódu) nebo číslo s nejvýš 13 číslicemi.
    /// </summary>
    private static long ReadEan()
    {
        // Kontrolní číslice EAN přibude s třídou Validators v dalším milníku
        return ConsoleUi.ReadLong("EAN-13 (0 = produkt bez EAN): ", 0, MaxEan,
            "Zadejte kód EAN s nejvýš 13 číslicemi, nebo 0, když ho produkt nemá.");
    }

    /// <summary>
    /// Nabídne sazby DPH z třídy VatRates a vrátí vybranou sazbu v procentech.
    /// </summary>
    private static decimal ReadVatRate()
    {
        Console.WriteLine("Sazba DPH:");
        for (int i = 0; i < VatRates.All.Length; i++)
        {
            // Uživatel volí od 1, pole se indexuje od 0
            Console.WriteLine($"  {i + 1}  {VatRates.All[i]:0} % ({VatRates.Names[i]})");
        }

        int choice = ConsoleUi.ReadInt($"Zvolte sazbu (1-{VatRates.All.Length}): ", 1, VatRates.All.Length);
        return VatRates.All[choice - 1];
    }
}
