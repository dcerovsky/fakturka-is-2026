using Fakturka.Models;
using Fakturka.Output;

namespace Fakturka.Ui;

/// <summary>
/// Obrazovka s údaji dodavatele, tedy e-shopu, který faktury vystavuje.
/// </summary>
public static class SupplierScreen
{
    private const int MaxNameLength = Storage.MaxPartyNameLength;
    private const int MaxAddressLength = Storage.MaxAddressLength;

    /// <summary>
    /// Ukáže údaje dodavatele a nabídne jejich úpravu. Vrátí aktuální údaje
    /// (nové, pokud je uživatel změnil, jinak původní).
    /// </summary>
    public static Party? Show(Party? supplier)
    {
        int choice;
        do
        {
            ConsoleUi.PrintBanner();
            ConsoleUi.PrintTitle("Údaje dodavatele");

            if (supplier == null)
            {
                ConsoleUi.PrintInfo("Údaje dodavatele zatím nejsou vyplněné. Bez nich nepůjde vystavit fakturu.");
            }
            else
            {
                ConsoleViews.PrintParty(supplier, true);
            }

            Console.WriteLine();
            Console.WriteLine(supplier == null ? "  1  Vyplnit údaje" : "  1  Změnit údaje");
            Console.WriteLine("  0  Zpět do hlavního menu");
            Console.WriteLine();
            choice = ConsoleUi.ReadInt("Zvolte akci (0-1): ", 0, 1);

            if (choice == 1)
            {
                supplier = Edit(supplier);
            }
        }
        while (choice != 0);

        return supplier;
    }

    /// <summary>
    /// Načte všechny údaje dodavatele, ukáže rekapitulaci a po potvrzení je uloží.
    /// </summary>
    private static Party? Edit(Party? current)
    {
        ConsoleUi.PrintBanner();
        ConsoleUi.PrintTitle(current == null ? "Vyplnění údajů dodavatele" : "Změna údajů dodavatele");

        string name = ConsoleUi.ReadText($"Název firmy nebo jméno (nejvýš {MaxNameLength} znaků): ", MaxNameLength);
        string address = ConsoleUi.ReadText("Adresa (ulice a číslo, PSČ a obec): ", MaxAddressLength);
        int ico = ConsoleUi.ReadIco("IČO: ", false);

        bool vatPayer = ConsoleUi.ReadYesNo("Jste plátce DPH?");
        // Neplátce DPH na faktuře DIČ neuvádí, plátce ho mít musí
        string dic = vatPayer ? ConsoleUi.ReadDic("DIČ (např. CZ12345679): ") : "";

        ConsoleUi.ReadAccount("Bankovní účet (např. 19-2000145399/0800): ", out long prefix, out long number, out int bankCode);

        Party supplier = new Party(name, address);
        supplier.Ico = ico;
        supplier.Dic = dic;
        supplier.IsVatPayer = vatPayer;
        supplier.AccountPrefix = prefix;
        supplier.AccountNumber = number;
        supplier.BankCode = bankCode;

        // Rekapitulace i s vypočteným IBAN, aby uživatel zkontroloval, co se uloží
        Console.WriteLine();
        Console.WriteLine("  ==================================");
        ConsoleViews.PrintParty(supplier, true);
        Console.WriteLine("  ==================================");
        Console.WriteLine();

        if (!ConsoleUi.ReadYesNo("Uložit tyto údaje?"))
        {
            ConsoleUi.PrintInfo("Údaje se nezměnily.");
            ConsoleUi.WaitForEnter();
            return current;
        }

        if (Storage.SaveSupplier(supplier, out string? error))
        {
            ConsoleUi.PrintSuccess("Údaje dodavatele jsou uložené.");
        }
        else
        {
            // Zápis se nepovedl, ale údaje v paměti platí dál - uživatel nepřijde o vyplněné hodnoty
            ConsoleUi.PrintError(error ?? "Údaje se nepodařilo uložit.");
            ConsoleUi.PrintInfo("Údaje platí do ukončení programu, ale do souboru se neuložily.");
        }

        ConsoleUi.WaitForEnter();
        return supplier;
    }
}
