namespace Fakturka.Logic;

/// <summary>
/// Sazby DPH, se kterými program počítá.
/// </summary>
public static class VatRates
{
    // Sazby v procentech, platné od 1. 1. 2024. Když se zákon změní, stačí upravit
    // tato dvě pole - zbytek programu se řídí jejich délkou, nemá sazby napevno.
    public static readonly decimal[] All = { 21m, 12m };
    public static readonly string[] Names = { "základní", "snížená" };

    /// <summary>
    /// Pozice sazby v poli All, nebo -1, když taková sazba neplatí.
    /// </summary>
    public static int IndexOf(decimal rate)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i] == rate)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// True, když je sazba jednou z platných (kontrola dat načtených ze souboru).
    /// </summary>
    public static bool IsKnown(decimal rate)
    {
        return IndexOf(rate) >= 0;
    }
}
