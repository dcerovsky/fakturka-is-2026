using Fakturka.Logic;

namespace Fakturka.Models;

/// <summary>
/// Strana obchodu: dodavatel (e-shop, který fakturu vystavuje) nebo odběratel (zákazník).
/// </summary>
public class Party
{
    public string Name { get; set; }

    /// <summary>Adresa na jednom řádku, např. „Hlavní 12, 460 01 Liberec“.</summary>
    public string Address { get; set; }

    /// <summary>IČO. Hodnota 0 znamená bez IČO (soukromá osoba jako odběratel).</summary>
    public int Ico { get; set; }

    /// <summary>DIČ ve tvaru CZ12345678. Prázdný text = bez DIČ.</summary>
    public string Dic { get; set; } = "";

    public bool IsVatPayer { get; set; }

    /// <summary>Předčíslí účtu, 0 = bez předčíslí.</summary>
    public long AccountPrefix { get; set; }

    /// <summary>Číslo účtu. Hodnota 0 znamená bez účtu (odběratel ho na faktuře nepotřebuje).</summary>
    public long AccountNumber { get; set; }

    public int BankCode { get; set; }

    public Party(string name, string address)
    {
        Name = name;
        Address = address;
    }

    public bool HasAccount()
    {
        return AccountNumber != 0;
    }

    /// <summary>
    /// IČO pro výpis: vždy 8 číslic včetně úvodních nul, nebo pomlčka.
    /// </summary>
    public string IcoText()
    {
        return Ico == 0 ? "-" : Ico.ToString("D8");
    }

    /// <summary>
    /// Číslo účtu v obvyklém zápisu: 19-2000145399/0800, bez předčíslí 2000145399/0800.
    /// </summary>
    public string AccountText()
    {
        if (!HasAccount())
        {
            return "-";
        }

        string account = AccountPrefix == 0 ? $"{AccountNumber}" : $"{AccountPrefix}-{AccountNumber}";
        return $"{account}/{BankCode:D4}";
    }

    /// <summary>
    /// IBAN po skupinách 4 znaků, nebo pomlčka, když strana účet nemá.
    /// </summary>
    public string IbanText()
    {
        return HasAccount() ? Iban.Format(Iban.FromCzechAccount(BankCode, AccountPrefix, AccountNumber)) : "-";
    }
}
