using Fakturka.Logic;
using Fakturka.Models;

namespace Fakturka.Ui;

/// <summary>
/// Výpisy dat do konzole: tabulky, údaje stran a náhled faktury.
/// Všechny tabulky se vejdou do terminálu širokého 80 znaků.
/// </summary>
public static class ConsoleViews
{
    // Šířka sloupce s názvem zboží
    private const int NameColumnWidth = 24;

    // Šířka popisků v blocích „popisek: hodnota“
    private const int LabelWidth = 11;

    // Šířka rámečku s nadpisem faktury
    private const int InvoiceBoxWidth = 60;

    // Délka nejdelšího sloupce grafu ve znacích
    private const int ChartWidth = 40;

    // Názvy měsíců pro měsíční přehled, index 0 = leden
    private static readonly string[] MonthNames =
    {
        "leden", "únor", "březen", "duben", "květen", "červen",
        "červenec", "srpen", "září", "říjen", "listopad", "prosinec"
    };

    /// <summary>
    /// Vypíše katalog jako tabulku s pořadovými čísly od 1.
    /// </summary>
    public static void PrintCatalog(Catalog catalog)
    {
        if (catalog.Count == 0)
        {
            ConsoleUi.PrintInfo("Katalog je prázdný.");
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
    /// <param name="isSupplier">Dodavatel bez DIČ je neplátce DPH, u odběratele to nevíme.</param>
    public static void PrintParty(Party party, bool isSupplier)
    {
        PrintField("Název", party.Name);
        PrintField("Adresa", party.Address);
        PrintField("IČO", party.IcoText());

        if (party.Dic.Length > 0)
        {
            PrintField("DIČ", party.Dic);
        }
        else
        {
            PrintField("DIČ", isSupplier ? "neplátce DPH" : "-");
        }

        if (party.HasAccount())
        {
            PrintField("Účet", party.AccountText());
            PrintField("IBAN", party.IbanText());
        }
    }

    /// <summary>
    /// Vypíše položky faktury jako tabulku s pořadovými čísly od 1.
    /// </summary>
    /// <param name="showSubtotal">True: pod tabulku přidá mezisoučet (při sestavování faktury).</param>
    public static void PrintInvoiceLines(Invoice invoice, bool showSubtotal)
    {
        if (invoice.LineCount == 0)
        {
            ConsoleUi.PrintInfo("Faktura zatím nemá žádné položky.");
            return;
        }

        // Sloupec DPH má smysl jen u plátce DPH
        string vatHeader = invoice.WithVat ? $"  {"DPH",4}" : "";
        string totalHeader = invoice.WithVat ? "bez DPH Kč" : "celkem Kč";
        int width = 3 + 1 + NameColumnWidth + 2 + 5 + 2 + 12 + (invoice.WithVat ? 6 : 0) + 2 + 13;

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  {"č.",3} {"Položka",-NameColumnWidth}  {"ks",5}  {"cena/ks Kč",12}{vatHeader}  {totalHeader,13}");
        Console.WriteLine($"  {new string('─', width)}");
        Console.ResetColor();

        for (int i = 0; i < invoice.LineCount; i++)
        {
            InvoiceLine line = invoice.GetLine(i);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"  {i + 1,3} ");
            Console.ResetColor();

            string vat = invoice.WithVat ? $"  {line.VatRate,2:0} %" : "";
            Console.WriteLine($"{Fit(line.Name, NameColumnWidth),-NameColumnWidth}  {line.Quantity,5}  " +
                              $"{line.UnitPrice,12:#,0.00}{vat}  {line.Base(),13:#,0.00}");
        }

        if (showSubtotal)
        {
            Console.WriteLine($"  {new string('─', width)}");
            if (invoice.WithVat)
            {
                Console.WriteLine($"  Mezisoučet: {Money(invoice.TotalBase())} bez DPH, {Money(invoice.TotalBeforeRounding())} s DPH");
            }
            else
            {
                Console.WriteLine($"  Mezisoučet: {Money(invoice.TotalBase())}");
            }

            Console.WriteLine($"  Položek: {invoice.LineCount} z {Invoice.MaxLines}");
        }
    }

    /// <summary>
    /// Náhled celé faktury před vystavením.
    /// </summary>
    public static void PrintInvoice(Invoice invoice, Party supplier)
    {
        string title = invoice.WithVat
            ? $"FAKTURA - DAŇOVÝ DOKLAD č. {invoice.Number}"
            : $"FAKTURA č. {invoice.Number}";
        PrintBox(title);
        Console.WriteLine();

        PrintSectionLabel("DODAVATEL");
        PrintParty(supplier, true);
        Console.WriteLine();

        PrintSectionLabel("ODBĚRATEL");
        PrintParty(invoice.Customer, false);
        Console.WriteLine();

        PrintField("Vystaveno", Date(invoice.IssueDate));
        if (invoice.WithVat)
        {
            PrintField("DUZP", Date(invoice.TaxableDate));
        }

        PrintField("Splatnost", Date(invoice.DueDate));
        PrintField("VS", invoice.Number.ToString());
        Console.WriteLine();

        PrintInvoiceLines(invoice, false);
        Console.WriteLine();

        if (invoice.WithVat)
        {
            PrintVatSummary(invoice);
        }
        else
        {
            Console.WriteLine("  Nejsem plátce DPH.");
        }

        // Šířky 46 + 14 zarovnají částky pod sloupec „celkem“ rekapitulace (18 + 3 x 14 znaků)
        Console.WriteLine();
        Console.WriteLine($"  {"Zaokrouhlení",-46}{invoice.Rounding(),14:#,0.00} Kč");

        // Částku k úhradě zvýrazníme, je to nejdůležitější číslo celé faktury
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"  {"CELKEM K ÚHRADĚ",-46}{invoice.TotalToPay(),14:#,0.00} Kč");
        Console.ResetColor();
        Console.WriteLine($"  Slovy: {AmountInWords.Convert((long)invoice.TotalToPay())}");
    }

    /// <summary>
    /// Rekapitulace DPH: jeden řádek za každou sazbu, která se na faktuře vyskytuje.
    /// </summary>
    private static void PrintVatSummary(Invoice invoice)
    {
        decimal[,] summary = invoice.VatSummary();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  {"Rekapitulace DPH",-18}{"základ Kč",14}{"DPH Kč",14}{"celkem Kč",14}");
        Console.ResetColor();

        for (int rate = 0; rate < VatRates.All.Length; rate++)
        {
            // Sazby, ve kterých na faktuře nic není, nevypisujeme
            if (summary[rate, Invoice.ColumnBase] == 0)
            {
                continue;
            }

            Console.WriteLine($"  {VatRates.All[rate] + " %",-18}{summary[rate, Invoice.ColumnBase],14:#,0.00}" +
                              $"{summary[rate, Invoice.ColumnVat],14:#,0.00}{summary[rate, Invoice.ColumnTotal],14:#,0.00}");
        }
    }

    /// <summary>
    /// Vypíše faktury jako tabulku s pořadovými čísly od 1. Ukáže nejvýš maxRows řádků.
    /// </summary>
    public static void PrintInvoiceTable(Invoice[] invoices, int maxRows)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  {"č.",3} {"faktura",-7}  {"vystaveno",12}  {"Odběratel",-NameColumnWidth}  {"IČO",-8}  {"k úhradě Kč",13}");
        Console.WriteLine($"  {new string('─', 3 + 1 + 7 + 2 + 12 + 2 + NameColumnWidth + 2 + 8 + 2 + 13)}");
        Console.ResetColor();

        int shown = Math.Min(invoices.Length, maxRows);
        for (int i = 0; i < shown; i++)
        {
            Invoice invoice = invoices[i];

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"  {i + 1,3} ");
            Console.ResetColor();

            Console.WriteLine($"{invoice.Number,-7}  {Date(invoice.IssueDate),12}  " +
                              $"{Fit(invoice.Customer.Name, NameColumnWidth),-NameColumnWidth}  " +
                              $"{invoice.Customer.IcoText(),-8}  {invoice.TotalToPay(),13:#,0.00}");
        }

        if (invoices.Length > shown)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Zobrazeno prvních {shown} z {invoices.Length} faktur. Zužte výběr hledáním, nebo změňte řazení.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Tabulka měsíčního přehledu: počet faktur, základ a DPH za každou sazbu a tržby bez DPH.
    /// </summary>
    /// <param name="counts">Počet faktur v měsících (index 0 = leden).</param>
    /// <param name="bases">Základy DPH [měsíc, sazba].</param>
    /// <param name="vat">DPH [měsíc, sazba].</param>
    public static void PrintMonthlyTable(int[] counts, decimal[,] bases, decimal[,] vat)
    {
        // Sloupce se sazbami mají smysl, jen když je v roce nějaké DPH (dodavatel je plátce)
        bool withVat = false;
        for (int month = 0; month < MonthNames.Length; month++)
        {
            if (RowSum(vat, month) > 0)
            {
                withVat = true;
            }
        }

        // Hlavička: sloupce za sazby se skládají v cyklu, takže další sazba v poli VatRates.All přidá sloupce sama
        string header = $"  {"měsíc",-8}{"faktur",7}";
        if (withVat)
        {
            for (int rate = 0; rate < VatRates.All.Length; rate++)
            {
                header += $"{$"základ {VatRates.All[rate]:0} %",13}{$"DPH {VatRates.All[rate]:0} %",11}";
            }
        }

        header += $"{"tržby",13}";
        string separator = $"  {new string('─', header.Length - 2)}";

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(header);
        Console.WriteLine(separator);
        Console.ResetColor();

        int totalCount = 0;
        for (int month = 0; month < MonthNames.Length; month++)
        {
            totalCount += counts[month];

            // Měsíce bez faktur jsou šedě, aby vynikly ty, kdy se prodávalo
            if (counts[month] == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            string row = $"  {MonthNames[month],-8}{counts[month],7}";
            if (withVat)
            {
                for (int rate = 0; rate < VatRates.All.Length; rate++)
                {
                    row += $"{bases[month, rate],13:#,0.00}{vat[month, rate],11:#,0.00}";
                }
            }

            row += $"{RowSum(bases, month),13:#,0.00}";
            Console.WriteLine(row);
            Console.ResetColor();
        }

        // Řádek celkem: součty sloupců za celý rok
        string totalRow = $"  {"celkem",-8}{totalCount,7}";
        decimal totalRevenue = 0;
        for (int rate = 0; rate < VatRates.All.Length; rate++)
        {
            decimal rateBase = ColumnSum(bases, rate);
            totalRevenue += rateBase;
            if (withVat)
            {
                totalRow += $"{rateBase,13:#,0.00}{ColumnSum(vat, rate),11:#,0.00}";
            }
        }

        totalRow += $"{totalRevenue,13:#,0.00}";

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(separator);
        Console.ResetColor();
        Console.WriteLine(totalRow);
    }

    /// <summary>
    /// Sloupcový graf tržeb bez DPH po měsících. Délka sloupce je poměrná k nejsilnějšímu měsíci,
    /// nejsilnější měsíc je zeleně, nejslabší červeně.
    /// </summary>
    public static void PrintMonthlyChart(int[] counts, decimal[,] bases)
    {
        // 1. Tržby jednotlivých měsíců: součet řádku 2D pole přes všechny sazby
        decimal[] revenue = new decimal[MonthNames.Length];
        for (int month = 0; month < MonthNames.Length; month++)
        {
            revenue[month] = RowSum(bases, month);
        }

        // 2. Nejsilnější a nejslabší měsíc, hledání maxima a minima jako ve cvičení #8.
        //    Měsíce bez faktur přeskočíme, jinak by nejslabší byl vždycky prázdný měsíc.
        int best = -1;
        int worst = -1;
        for (int month = 0; month < MonthNames.Length; month++)
        {
            if (counts[month] == 0)
            {
                continue;
            }

            if (best < 0 || revenue[month] > revenue[best])
            {
                best = month;
            }

            if (worst < 0 || revenue[month] < revenue[worst])
            {
                worst = month;
            }
        }

        if (best < 0 || revenue[best] == 0)
        {
            return;   // není co kreslit (a dělit nulou nechceme)
        }

        // 3. Kreslení: vnější cyklus jde po měsících, vnitřní kreslí sloupec po znacích
        for (int month = 0; month < MonthNames.Length; month++)
        {
            // Délka sloupce = tržba / nejvyšší tržba x šířka grafu. Zaokrouhlení nahoru zajistí,
            // že i malá tržba má aspoň jeden dílek a nesplyne s prázdným měsícem.
            int length = (int)Math.Ceiling(revenue[month] / revenue[best] * ChartWidth);

            Console.Write($"  {MonthNames[month],-9}");

            if (month == best)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            }
            else if (month == worst)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
            }

            for (int i = 0; i < length; i++)
            {
                Console.Write('█');
            }

            // Zbytek do plné šířky jako světlá stopa, aby byla vidět stupnice
            Console.ForegroundColor = ConsoleColor.DarkGray;
            for (int i = length; i < ChartWidth; i++)
            {
                Console.Write('░');
            }

            Console.ResetColor();
            Console.WriteLine($" {revenue[month],13:#,0.00} Kč");
        }

        // Legenda
        Console.WriteLine();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.Write("██");
        Console.ResetColor();
        Console.Write($" nejsilnější měsíc ({MonthNames[best]})");
        if (worst != best)
        {
            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("██");
            Console.ResetColor();
            Console.Write($" nejslabší měsíc ({MonthNames[worst]})");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Součet jednoho řádku 2D pole (u měsíčního přehledu: všechny sazby v jednom měsíci).
    /// </summary>
    private static decimal RowSum(decimal[,] table, int row)
    {
        decimal sum = 0;
        for (int column = 0; column < table.GetLength(1); column++)
        {
            sum += table[row, column];
        }

        return sum;
    }

    /// <summary>
    /// Součet jednoho sloupce 2D pole (u měsíčního přehledu: jedna sazba za celý rok).
    /// </summary>
    private static decimal ColumnSum(decimal[,] table, int column)
    {
        decimal sum = 0;
        for (int row = 0; row < table.GetLength(0); row++)
        {
            sum += table[row, column];
        }

        return sum;
    }

    /// <summary>
    /// Částka ve tvaru „1 452,00 Kč“.
    /// </summary>
    public static string Money(decimal amount)
    {
        return $"{amount:#,0.00} Kč";
    }

    /// <summary>
    /// Datum ve tvaru „27. 11. 2026“.
    /// </summary>
    public static string Date(DateTime date)
    {
        return date.ToString("d. M. yyyy");
    }

    /// <summary>
    /// Nadpis v rámečku.
    /// </summary>
    private static void PrintBox(string title)
    {
        string line = new string('═', InvoiceBoxWidth);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  ╔{line}╗");
        Console.Write("  ║");
        Console.ResetColor();
        Console.Write($"  {title}".PadRight(InvoiceBoxWidth));
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("║");
        Console.WriteLine($"  ╚{line}╝");
        Console.ResetColor();
    }

    private static void PrintSectionLabel(string label)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  {label}");
        Console.ResetColor();
    }

    /// <summary>
    /// Jeden řádek „popisek: hodnota“, popisek barevně a zarovnaný.
    /// </summary>
    private static void PrintField(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  {label + ":",-LabelWidth}");
        Console.ResetColor();
        Console.WriteLine(value);
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
