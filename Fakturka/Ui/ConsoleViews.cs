using Fakturka.Models;

namespace Fakturka.Ui;

/// <summary>
/// Výpisy dat do konzole: tabulky, později i náhled faktury a grafy.
/// </summary>
public static class ConsoleViews
{
    // Šířka sloupce s názvem. Tabulka se tak vejde do terminálu široké 80 znaků.
    private const int NameColumnWidth = 24;

    /// <summary>
    /// Vypíše katalog jako tabulku s pořadovými čísly od 1.
    /// </summary>
    public static void PrintCatalog(Catalog catalog)
    {
        if (catalog.Count == 0)
        {
            ConsoleUi.PrintInfo("Katalog je prázdný. Přidejte první produkt volbou 1.");
            return;
        }

        // Zápis {hodnota,šířka} zarovná hodnotu do sloupce:
        // záporná šířka = doleva (text), kladná = doprava (čísla)
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  {"č.",3} {"EAN",-13}  {"Název",-NameColumnWidth}  {"bez DPH Kč",12}  {"DPH",4}  {"s DPH Kč",12}");
        Console.WriteLine($"  {new string('─', 3 + 1 + 13 + 2 + NameColumnWidth + 2 + 12 + 2 + 4 + 2 + 12)}");
        Console.ResetColor();

        for (int i = 0; i < catalog.Count; i++)
        {
            Product product = catalog.Get(i);

            // Pořadové číslo je barevně, protože podle něj uživatel produkt vybírá.
            // Uživateli číslujeme od 1, v poli je produkt na indexu i (od 0).
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"  {i + 1,3} ");
            Console.ResetColor();

            Console.WriteLine($"{product.EanText(),-13}  {Fit(product.Name, NameColumnWidth),-NameColumnWidth}  " +
                              $"{product.UnitPrice,12:#,0.00}  {product.VatRate,2:0} %  {product.PriceWithVat(),12:#,0.00}");
        }

        Console.WriteLine();
        Console.WriteLine($"  Produktů v katalogu: {catalog.Count} z {Catalog.Capacity}");
    }

    /// <summary>
    /// Vypíše údaje dodavatele nebo odběratele pod sebe.
    /// </summary>
    public static void PrintParty(Party party)
    {
        PrintField("Název", party.Name);
        PrintField("Adresa", party.Address);
        PrintField("IČO", party.IcoText());
        PrintField("DIČ", party.IsVatPayer ? party.Dic : "neplátce DPH");

        if (party.HasAccount())
        {
            PrintField("Účet", party.AccountText());
            PrintField("IBAN", party.IbanText());
        }
    }

    /// <summary>
    /// Jeden řádek „popisek: hodnota“, popisek barevně a zarovnaný.
    /// </summary>
    private static void PrintField(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  {label + ":",-8}");
        Console.ResetColor();
        Console.WriteLine(value);
    }

    /// <summary>
    /// Částka ve tvaru „1 452,00 Kč“.
    /// </summary>
    public static string Money(decimal amount)
    {
        return $"{amount:#,0.00} Kč";
    }

    /// <summary>
    /// Zkrátí text na danou šířku; když je delší, nahradí konec třemi tečkami.
    /// </summary>
    private static string Fit(string text, int width)
    {
        if (text.Length <= width)
        {
            return text;
        }

        return text.Substring(0, width - 3) + "...";
    }
}
