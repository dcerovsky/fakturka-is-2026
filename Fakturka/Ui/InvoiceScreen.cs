using Fakturka.Logic;
using Fakturka.Models;
using Fakturka.Output;

namespace Fakturka.Ui;

/// <summary>
/// Vystavení faktury: odběratel, položky z katalogu, splatnost, náhled a uložení.
/// </summary>
public static class InvoiceScreen
{
    private const int DefaultDueDays = 14;
    private const int MaxDueDays = 90;

    /// <summary>
    /// Provede uživatele vystavením jedné faktury.
    /// </summary>
    public static void Show(InvoiceBook book, Catalog catalog, Party? supplier)
    {
        ConsoleUi.PrintBanner();
        ConsoleUi.PrintTitle("Vystavit fakturu");

        // Bez dodavatele a zboží fakturu vystavit nejde - rovnou řekneme, co chybí a kde to doplnit
        if (supplier == null)
        {
            ConsoleUi.PrintInfo("Nejdřív vyplňte údaje dodavatele (volba 5 v hlavním menu).");
            ConsoleUi.WaitForEnter();
            return;
        }

        if (catalog.Count == 0)
        {
            ConsoleUi.PrintInfo("Katalog je prázdný. Nejdřív přidejte zboží (volba 4 v hlavním menu).");
            ConsoleUi.WaitForEnter();
            return;
        }

        if (book.IsFull)
        {
            ConsoleUi.PrintError($"Evidence je plná ({InvoiceBook.Capacity} faktur), další fakturu nejde vystavit.");
            ConsoleUi.WaitForEnter();
            return;
        }

        DateTime today = DateTime.Today;
        int number = book.NextNumber(today.Year);
        if (number < 0)
        {
            ConsoleUi.PrintError($"V roce {today.Year} už došla čísla faktur (nejvýš {InvoiceBook.MaxSequence} za rok).");
            ConsoleUi.WaitForEnter();
            return;
        }

        Console.WriteLine($"  Faktura dostane číslo {number}.");
        Console.WriteLine();

        Party customer = ReadCustomer();

        // Plátcovství DPH se bere od dodavatele v okamžiku vystavení a na faktuře už zůstane
        Invoice invoice = new Invoice(customer, today, supplier.IsVatPayer);
        invoice.Number = number;

        if (!EditLines(invoice, catalog))
        {
            ConsoleUi.PrintInfo("Faktura se zrušila, nic se neuložilo.");
            ConsoleUi.WaitForEnter();
            return;
        }

        int dueDays = ConsoleUi.ReadIntWithDefault($"Splatnost ve dnech (0-{MaxDueDays}, Enter = {DefaultDueDays}): ",
            0, MaxDueDays, DefaultDueDays);
        invoice.DueDate = today.AddDays(dueDays);

        ConsoleUi.PrintBanner();
        ConsoleViews.PrintInvoice(invoice, supplier);
        Console.WriteLine();

        if (!ConsoleUi.ReadYesNo("Vystavit fakturu?"))
        {
            ConsoleUi.PrintInfo("Faktura se nevystavila, nic se neuložilo.");
            ConsoleUi.WaitForEnter();
            return;
        }

        Issue(invoice, book, supplier);
    }

    /// <summary>
    /// Načte údaje odběratele.
    /// </summary>
    private static Party ReadCustomer()
    {
        ConsoleUi.PrintTitle("Odběratel");

        string name = ConsoleUi.ReadText($"Název firmy nebo jméno (nejvýš {Storage.MaxPartyNameLength} znaků): ",
            Storage.MaxPartyNameLength);
        string address = ConsoleUi.ReadText("Adresa (ulice a číslo, PSČ a obec): ", Storage.MaxAddressLength);
        int ico = ConsoleUi.ReadIco("IČO (0 = soukromá osoba bez IČO): ", true);

        // DIČ má jen firma nebo podnikatel, a i ten ho mít nemusí (když není plátce DPH)
        string dic = ico == 0 ? "" : ConsoleUi.ReadDic("DIČ (Enter = bez DIČ): ", true);

        Party customer = new Party(name, address);
        customer.Ico = ico;
        customer.Dic = dic;
        customer.IsVatPayer = dic.Length > 0;
        return customer;
    }

    /// <summary>
    /// Sestavení položek faktury. Vrátí false, když uživatel fakturu zrušil.
    /// </summary>
    private static bool EditLines(Invoice invoice, Catalog catalog)
    {
        while (true)
        {
            ConsoleUi.PrintBanner();
            ConsoleUi.PrintTitle($"Faktura {invoice.Number} - položky");
            ConsoleViews.PrintCatalog(catalog);
            Console.WriteLine();
            ConsoleViews.PrintInvoiceLines(invoice, true);

            Console.WriteLine();
            Console.WriteLine("  1  Přidat položku");
            Console.WriteLine("  2  Odebrat položku");
            Console.WriteLine("  3  Dokončit a zobrazit náhled");
            Console.WriteLine("  0  Zrušit fakturu");
            Console.WriteLine();
            int choice = ConsoleUi.ReadInt("Zvolte akci (0-3): ", 0, 3);

            switch (choice)
            {
                case 0:
                    // Prázdnou fakturu zrušíme hned, rozepsanou až po potvrzení
                    if (invoice.LineCount == 0 || ConsoleUi.ReadYesNo("Opravdu zrušit rozepsanou fakturu?"))
                    {
                        return false;
                    }

                    break;
                case 1:
                    AddLine(invoice, catalog);
                    break;
                case 2:
                    RemoveLine(invoice);
                    break;
                case 3:
                    if (invoice.LineCount > 0)
                    {
                        return true;
                    }

                    ConsoleUi.PrintError("Faktura musí mít aspoň jednu položku.");
                    ConsoleUi.WaitForEnter();
                    break;
            }
        }
    }

    /// <summary>
    /// Přidá produkt z katalogu na fakturu. Stejný produkt za stejnou cenu jen navýší počet kusů.
    /// </summary>
    private static void AddLine(Invoice invoice, Catalog catalog)
    {
        Product product = ReadProduct(catalog);
        int quantity = ConsoleUi.ReadInt($"Počet kusů (1-{InvoiceLine.MaxQuantity}): ", 1, InvoiceLine.MaxQuantity);

        int existing = invoice.FindLine(product.Name, product.UnitPrice, product.VatRate);
        if (existing >= 0)
        {
            InvoiceLine line = invoice.GetLine(existing);
            if (line.Quantity + quantity > InvoiceLine.MaxQuantity)
            {
                ConsoleUi.PrintError($"Položka by měla víc než {InvoiceLine.MaxQuantity} ks, počet se nezměnil.");
                ConsoleUi.WaitForEnter();
                return;
            }

            line.Quantity += quantity;
            return;
        }

        // Kopie názvu, ceny a sazby: když se cena v katalogu později změní, faktura zůstane, jak byla
        InvoiceLine newLine = new InvoiceLine(product.Ean, product.Name, quantity, product.UnitPrice, product.VatRate);
        if (!invoice.AddLine(newLine))
        {
            ConsoleUi.PrintError($"Faktura už má {Invoice.MaxLines} položek, další se na ni nevejde.");
            ConsoleUi.WaitForEnter();
        }
    }

    /// <summary>
    /// Výběr produktu: pořadovým číslem z tabulky katalogu, nebo jeho EAN.
    /// </summary>
    private static Product ReadProduct(Catalog catalog)
    {
        while (true)
        {
            long code = ConsoleUi.ReadLong($"Číslo produktu (1-{catalog.Count}) nebo jeho EAN: ", 1, Validators.MaxEan,
                "Zadejte číslo produktu z tabulky, nebo jeho EAN.");

            // Malé číslo je pořadí v tabulce (od 1), větší hledáme jako EAN
            if (code <= catalog.Count)
            {
                return catalog.Get((int)code - 1);
            }

            int index = catalog.FindByEan(code);
            if (index >= 0)
            {
                return catalog.Get(index);
            }

            ConsoleUi.PrintError("Produkt s tímto číslem ani EAN v katalogu není.");
        }
    }

    /// <summary>
    /// Odebere položku podle jejího čísla v tabulce faktury.
    /// </summary>
    private static void RemoveLine(Invoice invoice)
    {
        if (invoice.LineCount == 0)
        {
            ConsoleUi.PrintInfo("Faktura zatím nemá žádné položky.");
            ConsoleUi.WaitForEnter();
            return;
        }

        int number = ConsoleUi.ReadInt($"Číslo položky k odebrání (1-{invoice.LineCount}): ", 1, invoice.LineCount);
        invoice.RemoveLine(number - 1);   // v tabulce od 1, v poli od 0
    }

    /// <summary>
    /// Zapíše fakturu do evidence, uloží ji jako HTML a nabídne otevření v prohlížeči.
    /// </summary>
    private static void Issue(Invoice invoice, InvoiceBook book, Party supplier)
    {
        // Faktura je vystavená, teprve když je bezpečně uložená v evidenci.
        // Když se uložení nepovede, vrátíme evidenci do původního stavu.
        book.Add(invoice);
        if (!Storage.SaveInvoices(book, out string? error))
        {
            book.RemoveLast();
            ConsoleUi.PrintError(error ?? "Evidenci faktur se nepodařilo uložit.");
            ConsoleUi.PrintInfo("Faktura se nevystavila. Zkuste to znovu, např. po zavření souboru v Excelu.");
            ConsoleUi.WaitForEnter();
            return;
        }

        ConsoleUi.PrintSuccess($"Faktura {invoice.Number} je vystavená a zapsaná v evidenci.");

        if (!InvoiceHtml.Save(invoice, supplier, out string path, out string? htmlError))
        {
            ConsoleUi.PrintError(htmlError ?? "Soubor s fakturou se nepodařilo uložit.");
            ConsoleUi.WaitForEnter();
            return;
        }

        Console.WriteLine($"  Soubor: {path}");
        Console.WriteLine();

        if (ConsoleUi.ReadYesNo("Otevřít fakturu v prohlížeči?") && !InvoiceHtml.Open(path))
        {
            ConsoleUi.PrintError("Prohlížeč se nepodařilo otevřít. Fakturu najdete v souboru výše.");
        }

        ConsoleUi.WaitForEnter();
    }
}
