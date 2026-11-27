using Fakturka.Logic;
using Fakturka.Models;
using Fakturka.Output;

namespace Fakturka.Ui;

/// <summary>
/// Přehled vystavených faktur: tabulka, řazení, hledání, souhrn a zobrazení jedné faktury.
/// </summary>
public static class InvoiceListScreen
{
    // Kolik faktur se vejde do tabulky, aby přehled nezabral několik obrazovek
    private const int MaxRows = 15;

    /// <summary>
    /// Podmenu přehledu. Běží ve smyčce, dokud uživatel nezvolí návrat (0).
    /// </summary>
    public static void Show(InvoiceBook book, Party? supplier)
    {
        if (book.Count == 0)
        {
            ConsoleUi.PrintBanner();
            ConsoleUi.PrintTitle("Přehled faktur");
            ConsoleUi.PrintInfo("Zatím není vystavená žádná faktura (volba 1 v hlavním menu).");
            ConsoleUi.WaitForEnter();
            return;
        }

        // Výchozí pohled: všechny faktury, nejnovější nahoře
        int sortKey = InvoiceBook.SortByDate;
        bool descending = true;
        string query = "";

        int choice;
        do
        {
            // Pohled se sestaví pokaždé znovu: buď kopie celé evidence, nebo výsledek hledání,
            // a ten se seřadí. Evidence sama zůstává v pořadí podle čísla.
            Invoice[] view = query.Length == 0 ? book.Copy() : book.Search(query);
            InvoiceBook.Sort(view, sortKey, descending);

            ConsoleUi.PrintBanner();
            ConsoleUi.PrintTitle("Přehled faktur");
            PrintViewInfo(sortKey, descending, query, view.Length, book.Count);
            Console.WriteLine();

            if (view.Length == 0)
            {
                ConsoleUi.PrintInfo("Hledanému textu neodpovídá žádná faktura.");
            }
            else
            {
                ConsoleViews.PrintInvoiceTable(view, MaxRows);
                Console.WriteLine();
                PrintSummary(view);
            }

            Console.WriteLine();
            Console.WriteLine("  1  Seřadit podle data");
            Console.WriteLine("  2  Seřadit podle částky");
            Console.WriteLine("  3  Hledat (číslo faktury, IČO, název odběratele)");
            Console.WriteLine("  4  Zobrazit všechny faktury");
            Console.WriteLine("  5  Zobrazit fakturu");
            Console.WriteLine("  0  Zpět do hlavního menu");
            Console.WriteLine();
            choice = ConsoleUi.ReadInt("Zvolte akci (0-5): ", 0, 5);

            switch (choice)
            {
                case 1:
                    sortKey = InvoiceBook.SortByDate;
                    descending = ConsoleUi.ReadYesNo("Nejnovější nahoře?");
                    break;
                case 2:
                    sortKey = InvoiceBook.SortByAmount;
                    descending = ConsoleUi.ReadYesNo("Nejvyšší částka nahoře?");
                    break;
                case 3:
                    query = ConsoleUi.ReadText("Hledaný text: ", Storage.MaxPartyNameLength);
                    break;
                case 4:
                    query = "";
                    break;
                case 5:
                    ShowInvoice(view, book, supplier);
                    break;
            }
        }
        while (choice != 0);
    }

    /// <summary>
    /// Řádek nad tabulkou: jak je seřazeno a co se hledá.
    /// </summary>
    private static void PrintViewInfo(int sortKey, bool descending, string query, int found, int total)
    {
        string order;
        if (sortKey == InvoiceBook.SortByDate)
        {
            order = descending ? "podle data, nejnovější nahoře" : "podle data, nejstarší nahoře";
        }
        else
        {
            order = descending ? "podle částky, nejvyšší nahoře" : "podle částky, nejnižší nahoře";
        }

        Console.WriteLine($"  Řazení: {order}");
        if (query.Length > 0)
        {
            Console.WriteLine($"  Hledání: „{query}“, nalezeno {found} z {total}");
        }
    }

    /// <summary>
    /// Souhrn faktur v tabulce: počet, součet, největší a nejmenší faktura.
    /// </summary>
    private static void PrintSummary(Invoice[] invoices)
    {
        // Jeden průchod polem: součty a zároveň hledání maxima a minima (cvičení #8)
        decimal total = 0;
        decimal vat = 0;
        int largest = 0;
        int smallest = 0;
        for (int i = 0; i < invoices.Length; i++)
        {
            decimal amount = invoices[i].TotalToPay();
            total += amount;
            vat += invoices[i].TotalBeforeRounding() - invoices[i].TotalBase();

            if (amount > invoices[largest].TotalToPay())
            {
                largest = i;
            }

            if (amount < invoices[smallest].TotalToPay())
            {
                smallest = i;
            }
        }

        Console.Write($"  Faktur: {invoices.Length}, celkem k úhradě {ConsoleViews.Money(total)}");
        Console.WriteLine(vat > 0 ? $", z toho DPH {ConsoleViews.Money(vat)}" : "");
        PrintSummaryLine("Největší", invoices[largest]);
        PrintSummaryLine("Nejmenší", invoices[smallest]);
    }

    private static void PrintSummaryLine(string label, Invoice invoice)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  {label + ":",-10}");
        Console.ResetColor();
        Console.WriteLine($"{invoice.Number}  {invoice.Customer.Name}, {ConsoleViews.Money(invoice.TotalToPay())}");
    }

    /// <summary>
    /// Výběr faktury podle řádku v tabulce nebo čísla faktury, náhled v konzoli
    /// a nabídka otevření v prohlížeči.
    /// </summary>
    private static void ShowInvoice(Invoice[] view, InvoiceBook book, Party? supplier)
    {
        if (view.Length == 0)
        {
            ConsoleUi.PrintInfo("V tabulce není žádná faktura. Zobrazte všechny faktury (volba 4).");
            ConsoleUi.WaitForEnter();
            return;
        }

        Invoice? invoice = ReadInvoice(view, book);
        if (invoice == null)
        {
            return;   // uživatel zvolil návrat
        }

        // Bez dodavatele nejde fakturu zobrazit ani znovu vytvořit (dodavatel se na fakturu nepřepisuje)
        if (supplier == null)
        {
            ConsoleUi.PrintError("Chybí údaje dodavatele, fakturu nejde zobrazit (volba 5 v hlavním menu).");
            ConsoleUi.WaitForEnter();
            return;
        }

        // Uložený soubor je faktura tak, jak se vystavila. Když chybí (někdo ho smazal), vytvoří se znovu.
        // Ukázkové faktury se vytvoří vždy, protože ve stejné složce můžou být faktury z jiného seedu.
        string path = InvoiceHtml.FilePath(invoice.Number);
        bool recreated = false;
        if (DemoData.IsActive || !File.Exists(path))
        {
            if (!InvoiceHtml.Save(invoice, supplier, out path, out string? error))
            {
                ConsoleUi.PrintError(error ?? "Soubor s fakturou se nepodařilo vytvořit.");
                ConsoleUi.WaitForEnter();
                return;
            }

            recreated = !DemoData.IsActive;
        }

        ConsoleUi.PrintBanner();
        ConsoleViews.PrintInvoice(invoice, supplier);
        Console.WriteLine();
        Console.WriteLine($"  Soubor: {path}");
        if (recreated)
        {
            ConsoleUi.PrintInfo("Soubor s fakturou chyběl, vytvořil se znovu s aktuálními údaji dodavatele.");
        }

        Console.WriteLine();
        if (ConsoleUi.ReadYesNo("Otevřít fakturu v prohlížeči?") && !InvoiceHtml.Open(path))
        {
            ConsoleUi.PrintError("Prohlížeč se nepodařilo otevřít. Fakturu najdete v souboru výše.");
        }

        ConsoleUi.WaitForEnter();
    }

    /// <summary>
    /// Načte fakturu podle čísla řádku v tabulce, nebo podle čísla faktury. Vrátí null pro návrat (0).
    /// </summary>
    private static Invoice? ReadInvoice(Invoice[] view, InvoiceBook book)
    {
        int rows = Math.Min(view.Length, MaxRows);

        while (true)
        {
            int code = ConsoleUi.ReadInt($"Řádek tabulky (1-{rows}) nebo číslo faktury, 0 = zpět: ", 0, int.MaxValue,
                "Zadejte číslo řádku z tabulky, číslo faktury, nebo 0 pro návrat.");

            if (code == 0)
            {
                return null;
            }

            // Malé číslo je řádek tabulky (od 1), větší hledáme jako číslo faktury v celé evidenci
            if (code <= rows)
            {
                return view[code - 1];
            }

            int index = book.FindByNumber(code);
            if (index >= 0)
            {
                return book.Get(index);
            }

            ConsoleUi.PrintError($"Faktura číslo {code} v evidenci není.");
        }
    }
}
