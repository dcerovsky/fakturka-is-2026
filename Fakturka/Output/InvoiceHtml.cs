using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Fakturka.Logic;
using Fakturka.Models;

namespace Fakturka.Output;

/// <summary>
/// Faktura jako HTML dokument. V prohlížeči jde vytisknout nebo uložit jako PDF.
/// </summary>
public static class InvoiceHtml
{
    // Pevná česká kultura pro částky a data, aby faktura nezávisela na nastavení systému
    private static readonly CultureInfo Czech = new CultureInfo("cs-CZ");

    // Styl je přímo v souboru, takže faktura vypadá stejně i bez internetu.
    // @page a @media print zajistí tisk na A4 bez šedého pozadí a stínu.
    private const string Css = """
        :root { --accent: #0e7490; --accent-soft: #ecfeff; --ink: #1f2937; --muted: #6b7280; --line: #e5e7eb; }
        * { box-sizing: border-box; }
        body { margin: 0; background: #e5e7eb; color: var(--ink);
               font: 14px/1.5 -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif; }
        .sheet { max-width: 210mm; margin: 24px auto; padding: 15mm; background: #fff;
                 border-radius: 14px; box-shadow: 0 10px 30px rgba(0, 0, 0, .12); }
        .top { display: flex; justify-content: space-between; align-items: flex-end; gap: 16px;
               border-bottom: 3px solid var(--accent); padding-bottom: 12px; margin-bottom: 20px; }
        .kind { color: var(--accent); font-weight: 700; text-transform: uppercase; letter-spacing: .08em; font-size: 12px; }
        h1 { margin: 2px 0 0; font-size: 30px; line-height: 1.1; }
        .seller { font-size: 18px; font-weight: 600; text-align: right; }
        h2 { margin: 0 0 6px; font-size: 11px; text-transform: uppercase; letter-spacing: .08em; color: var(--muted); }
        .parties { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 16px; }
        .box { border: 1px solid var(--line); border-radius: 10px; padding: 12px 14px; }
        .name { font-weight: 600; font-size: 15px; }
        dl { display: grid; grid-template-columns: auto 1fr; gap: 2px 12px; margin: 8px 0 0; }
        dt { color: var(--muted); }
        dd { margin: 0; font-weight: 500; }
        .pay dl { grid-template-columns: 1fr; gap: 0; }
        .pay dt { font-size: 11px; margin-top: 6px; }
        .pay dt:first-child { margin-top: 0; }
        .pay dd { white-space: nowrap; font-variant-numeric: tabular-nums; }
        .meta { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; background: var(--accent-soft);
                border-radius: 10px; padding: 10px 14px; margin-bottom: 18px; }
        .meta span { display: block; font-size: 11px; color: var(--muted); }
        .meta strong { font-size: 14px; }
        table { width: 100%; border-collapse: collapse; }
        th { text-align: left; font-size: 11px; text-transform: uppercase; letter-spacing: .06em; color: var(--muted);
             border-bottom: 2px solid var(--line); padding: 8px 6px; }
        td { padding: 9px 6px; border-bottom: 1px solid var(--line); vertical-align: top; }
        .num { text-align: right; white-space: nowrap; font-variant-numeric: tabular-nums; }
        .ean { font-size: 11px; color: var(--muted); }
        .bottom { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-top: 20px; align-items: start; }
        .recap th, .recap td { padding: 4px 6px; font-size: 12px; border-bottom: 1px solid var(--line); }
        .recap th { text-align: right; }
        .recap th:first-child { text-align: left; }
        .rounding { display: flex; justify-content: space-between; font-size: 12px; color: var(--muted); padding: 6px 6px 0; }
        .grand { display: flex; justify-content: space-between; align-items: baseline; gap: 12px; background: var(--accent);
                 color: #fff; border-radius: 10px; padding: 12px 16px; margin-top: 10px; }
        .grand strong { font-size: 22px; white-space: nowrap; }
        .words { margin: 8px 2px 0; font-style: italic; color: var(--muted); }
        .note { margin: 18px 0 0; font-weight: 600; }
        footer { margin-top: 28px; font-size: 11px; color: var(--muted); text-align: center; }
        @page { size: A4; margin: 0; }
        @media print {
            body { background: #fff; }
            .sheet { margin: 0; max-width: none; border-radius: 0; box-shadow: none; }
            .meta, .grand { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
        }
        """;

    /// <summary>
    /// Sestaví celý HTML dokument faktury.
    /// </summary>
    public static string Render(Invoice invoice, Party supplier)
    {
        StringBuilder html = new StringBuilder();
        string kind = invoice.WithVat ? "Faktura - daňový doklad" : "Faktura";

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"cs\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine($"<title>Faktura {invoice.Number}</title>");
        html.AppendLine($"<style>\n{Css}\n</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<main class=\"sheet\">");

        // Hlavička: druh dokladu, číslo a jméno dodavatele
        html.AppendLine("<header class=\"top\">");
        html.AppendLine($"<div><div class=\"kind\">{kind}</div><h1>{invoice.Number}</h1></div>");
        html.AppendLine($"<div class=\"seller\">{Encode(supplier.Name)}</div>");
        html.AppendLine("</header>");

        // Dodavatel a odběratel vedle sebe
        html.AppendLine("<section class=\"parties\">");
        AppendParty(html, "Dodavatel", supplier, true);
        AppendParty(html, "Odběratel", invoice.Customer, false);
        html.AppendLine("</section>");

        // Data a variabilní symbol
        html.AppendLine("<section class=\"meta\">");
        AppendMeta(html, "Datum vystavení", Date(invoice.IssueDate));
        if (invoice.WithVat)
        {
            AppendMeta(html, "Datum zdan. plnění", Date(invoice.TaxableDate));
        }

        AppendMeta(html, "Datum splatnosti", Date(invoice.DueDate));
        AppendMeta(html, "Variabilní symbol", invoice.Number.ToString());
        html.AppendLine("</section>");

        AppendLines(html, invoice);

        // Dole vlevo platební údaje, vpravo rekapitulace a celková částka
        html.AppendLine("<section class=\"bottom\">");
        // Popisky nad hodnotami (třída pay), aby se IBAN vešel na jeden řádek i v polovině A4
        html.AppendLine("<div class=\"box pay\">");
        html.AppendLine("<h2>Platební údaje</h2>");
        html.AppendLine("<dl>");
        html.AppendLine($"<dt>Číslo účtu</dt><dd>{Encode(supplier.AccountText())}</dd>");
        html.AppendLine($"<dt>IBAN</dt><dd>{Encode(supplier.IbanText())}</dd>");
        html.AppendLine($"<dt>Variabilní symbol</dt><dd>{invoice.Number}</dd>");
        html.AppendLine($"<dt>Splatnost</dt><dd>{Date(invoice.DueDate)}</dd>");
        html.AppendLine("</dl>");
        html.AppendLine("</div>");

        html.AppendLine("<div>");
        if (invoice.WithVat)
        {
            AppendVatSummary(html, invoice);
        }

        decimal rounding = invoice.Rounding();
        if (rounding != 0)
        {
            html.AppendLine($"<div class=\"rounding\"><span>Zaokrouhlení</span><span>{Money(rounding)}</span></div>");
        }

        decimal total = invoice.TotalToPay();
        html.AppendLine($"<div class=\"grand\"><span>Celkem k úhradě</span><strong>{Money(total)}</strong></div>");
        html.AppendLine($"<p class=\"words\">Slovy: {Encode(AmountInWords.Convert((long)total))}</p>");
        html.AppendLine("</div>");
        html.AppendLine("</section>");

        if (!invoice.WithVat)
        {
            html.AppendLine("<p class=\"note\">Nejsem plátce DPH.</p>");
        }

        html.AppendLine($"<footer>Vystaveno {Date(invoice.IssueDate)} v programu Fakturka.</footer>");
        html.AppendLine("</main>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    /// <summary>
    /// Uloží fakturu do složky faktury vedle ostatních dat jako 2026001.html.
    /// </summary>
    public static bool Save(Invoice invoice, Party supplier, out string path, out string? error)
    {
        string folder = Path.Combine(Storage.DataFolder, "faktury");
        path = Path.Combine(folder, $"{invoice.Number}.html");

        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, Render(invoice, supplier), new UTF8Encoding(false));
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            error = $"Fakturu nejde uložit do souboru {path}: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Otevře soubor ve výchozím programu (u .html v prohlížeči). Vrátí false, když to nejde.
    /// </summary>
    public static bool Open(string path)
    {
        try
        {
            // UseShellExecute = soubor otevře systém stejně jako dvojklik:
            // na Macu přes příkaz open, na Windows přes Průzkumníka
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is PlatformNotSupportedException)
        {
            return false;
        }
    }

    // ------------------------------------------------------------
    // Části dokumentu
    // ------------------------------------------------------------

    private static void AppendParty(StringBuilder html, string title, Party party, bool isSupplier)
    {
        html.AppendLine("<div class=\"box\">");
        html.AppendLine($"<h2>{title}</h2>");
        html.AppendLine($"<div class=\"name\">{Encode(party.Name)}</div>");
        html.AppendLine($"<div>{Encode(party.Address)}</div>");
        html.AppendLine("<dl>");

        // Odběratel bez IČO nebo DIČ (soukromá osoba) ty řádky prostě nemá
        if (party.Ico != 0)
        {
            html.AppendLine($"<dt>IČO</dt><dd>{party.IcoText()}</dd>");
        }

        if (party.Dic.Length > 0)
        {
            html.AppendLine($"<dt>DIČ</dt><dd>{Encode(party.Dic)}</dd>");
        }
        else if (isSupplier)
        {
            html.AppendLine("<dt>DPH</dt><dd>neplátce DPH</dd>");
        }

        html.AppendLine("</dl>");
        html.AppendLine("</div>");
    }

    private static void AppendMeta(StringBuilder html, string label, string value)
    {
        html.AppendLine($"<div><span>{label}</span><strong>{value}</strong></div>");
    }

    private static void AppendLines(StringBuilder html, Invoice invoice)
    {
        html.AppendLine("<table>");
        html.Append("<thead><tr><th>Položka</th><th class=\"num\">Množství</th><th class=\"num\">Cena za ks</th>");
        if (invoice.WithVat)
        {
            html.Append("<th class=\"num\">DPH</th><th class=\"num\">Celkem bez DPH</th>");
        }
        else
        {
            html.Append("<th class=\"num\">Celkem</th>");
        }

        html.AppendLine("</tr></thead>");
        html.AppendLine("<tbody>");

        for (int i = 0; i < invoice.LineCount; i++)
        {
            InvoiceLine line = invoice.GetLine(i);
            string ean = line.Ean == 0 ? "" : $"<div class=\"ean\">EAN {line.Ean:D13}</div>";

            html.Append($"<tr><td>{Encode(line.Name)}{ean}</td>");
            html.Append($"<td class=\"num\">{line.Quantity}&nbsp;ks</td>");
            html.Append($"<td class=\"num\">{Money(line.UnitPrice)}</td>");
            if (invoice.WithVat)
            {
                html.Append($"<td class=\"num\">{line.VatRate.ToString("0", Czech)}&nbsp;%</td>");
            }

            html.AppendLine($"<td class=\"num\">{Money(line.Base())}</td></tr>");
        }

        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
    }

    private static void AppendVatSummary(StringBuilder html, Invoice invoice)
    {
        decimal[,] summary = invoice.VatSummary();

        html.AppendLine("<table class=\"recap\">");
        html.AppendLine("<thead><tr><th>Sazba</th><th>Základ</th><th>DPH</th><th>Celkem</th></tr></thead>");
        html.AppendLine("<tbody>");
        for (int rate = 0; rate < VatRates.All.Length; rate++)
        {
            // Sazby, ve kterých na faktuře nic není, nevypisujeme
            if (summary[rate, Invoice.ColumnBase] == 0)
            {
                continue;
            }

            html.AppendLine($"<tr><td>{VatRates.All[rate].ToString("0", Czech)}&nbsp;%</td>" +
                            $"<td class=\"num\">{Money(summary[rate, Invoice.ColumnBase])}</td>" +
                            $"<td class=\"num\">{Money(summary[rate, Invoice.ColumnVat])}</td>" +
                            $"<td class=\"num\">{Money(summary[rate, Invoice.ColumnTotal])}</td></tr>");
        }

        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
    }

    // ------------------------------------------------------------
    // Formátování
    // ------------------------------------------------------------

    /// <summary>
    /// Text od uživatele převede tak, aby ho prohlížeč zobrazil a nečetl jako HTML.
    /// Bez toho by odběratel „Novák &amp; syn &lt;s.r.o.&gt;“ rozbil celý dokument.
    /// </summary>
    private static string Encode(string text)
    {
        // Nahrazujeme jen znaky, které mají v HTML zvláštní význam. Ampersand musí být první,
        // jinak by se znovu převedly i ampersandy z už nahrazených &lt; a &gt;.
        // Diakritiku necháváme být - soubor je v UTF-8 (meta charset), takže ji prohlížeč zobrazí.
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&#39;");
    }

    private static string Money(decimal amount)
    {
        //   je pevná mezera: „Kč“ se nikdy neodtrhne od čísla na další řádek
        return amount.ToString("#,0.00", Czech) + " Kč";
    }

    private static string Date(DateTime date)
    {
        return date.ToString("d. M. yyyy", Czech);
    }
}
