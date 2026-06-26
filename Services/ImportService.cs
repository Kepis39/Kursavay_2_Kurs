using CsvHelper;
using CsvHelper.Configuration;
using LibraryApp.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Services;

public class ImportService
{
    public List<string> ImportErrors { get; } = new();

    private LibraryabonementContext NewCtx() => new();

    private static bool IsExcel(string path)
    {
        try { var b = File.ReadAllBytes(path).Take(2).ToArray(); return b.Length >= 2 && b[0] == 0x50 && b[1] == 0x4B; }
        catch { return false; }
    }

    private void ClearErrors() => ImportErrors.Clear();
    private void AddError(string msg) => ImportErrors.Add(msg);

    // ========== CSV ==========
    public async Task<bool> ImportBooksFromCsvAsync(string path)
    {
        ClearErrors();
        try
        {
            if (IsExcel(path)) { AddError("Это Excel, не CSV"); return false; }
            using var r = new StreamReader(path);
            using var csv = new CsvReader(r, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, HeaderValidated = null, MissingFieldFound = null, BadDataFound = null });
            csv.Context.RegisterClassMap<BookMap>();
            var recs = csv.GetRecords<BookDto>().ToList();
            await using var ctx = NewCtx();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.BookId)) { AddError($"Row {csv.Parser.Row}: Empty BookId"); continue; }
                if (ctx.Books.Any(b => b.BookId == x.BookId)) { AddError($"Book {x.BookId} already exists"); continue; }
                ctx.Books.Add(new Book { BookId = x.BookId, Title = x.Title, FirstAuthor = x.Author, Publisher = x.Publisher, PlaceOfPublication = x.Place, YearOfPublication = x.Year, PageCount = x.Pages, Price = x.Price });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"CSV Error: {ex.Message}"); return false; }
    }

    public async Task<bool> ImportReadersFromCsvAsync(string path)
    {
        ClearErrors();
        try
        {
            if (IsExcel(path)) { AddError("Это Excel, не CSV"); return false; }
            using var r = new StreamReader(path);
            using var csv = new CsvReader(r, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, HeaderValidated = null, MissingFieldFound = null, BadDataFound = null });
            csv.Context.RegisterClassMap<ReaderMap>();
            var recs = csv.GetRecords<ReaderDto>().ToList();
            await using var ctx = NewCtx();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.FullName)) { AddError($"Row {csv.Parser.Row}: Empty FullName"); continue; }
                if (ctx.Readers.Any(r => r.TicketNumber == x.Ticket)) { AddError($"Reader {x.Ticket} already exists"); continue; }
                ctx.Readers.Add(new Reader { TicketNumber = x.Ticket, FullName = x.FullName, BirthDate = string.IsNullOrWhiteSpace(x.Birth) ? null : (DateTime.TryParseExact(x.Birth, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var bd) ? bd : null), Phone = x.Phone });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"CSV Error: {ex.Message}"); return false; }
    }

    public async Task<bool> ImportCopiesFromCsvAsync(string path)
    {
        ClearErrors();
        try
        {
            if (IsExcel(path)) { AddError("Это Excel, не CSV"); return false; }
            using var r = new StreamReader(path);
            using var csv = new CsvReader(r, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, HeaderValidated = null, MissingFieldFound = null, BadDataFound = null });
            csv.Context.RegisterClassMap<CopyMap>();
            var recs = csv.GetRecords<CopyDto>().ToList();
            await using var ctx = NewCtx();
            var existingBooks = ctx.Books.Select(b => b.BookId).ToHashSet();
            var existingThemes = ctx.ThematicCatalogs.Select(t => t.ThemeCode).ToHashSet();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.Inv)) { AddError($"Row {csv.Parser.Row}: Empty InventoryNumber"); continue; }
                if (ctx.Copies.Any(c => c.InventoryNumber == x.Inv)) { AddError($"Copy {x.Inv} already exists"); continue; }
                if (!string.IsNullOrWhiteSpace(x.BookId) && !existingBooks.Contains(x.BookId)) { AddError($"Book {x.BookId} not found"); continue; }
                if (x.Theme.HasValue && !existingThemes.Contains(x.Theme.Value)) { AddError($"Theme {x.Theme} not found"); continue; }
                ctx.Copies.Add(new Copy { InventoryNumber = x.Inv, BookId = x.BookId, ThemeCode = x.Theme, CopyCount = x.Count });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"CSV Error: {ex.Message}"); return false; }
    }

    public async Task<bool> ImportThemesFromCsvAsync(string path)
    {
        ClearErrors();
        try
        {
            if (IsExcel(path)) { AddError("Это Excel, не CSV"); return false; }
            using var r = new StreamReader(path);
            using var csv = new CsvReader(r, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, HeaderValidated = null, MissingFieldFound = null, BadDataFound = null });
            csv.Context.RegisterClassMap<ThemeMap>();
            var recs = csv.GetRecords<ThemeDto>().ToList();
            await using var ctx = NewCtx();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.Name)) { AddError($"Row {csv.Parser.Row}: Empty ThemeName"); continue; }
                if (ctx.ThematicCatalogs.Any(t => t.ThemeCode == x.Code)) { AddError($"Theme {x.Code} already exists"); continue; }
                ctx.ThematicCatalogs.Add(new ThematicCatalog { ThemeCode = x.Code, ThemeName = x.Name });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"CSV Error: {ex.Message}"); return false; }
    }

    // ========== EXCEL ==========
    public async Task<bool> ImportBooksFromExcelAsync(string path)
    {
        ClearErrors();
        try
        {
            ExcelPackage.License.SetNonCommercialPersonal("LibraryApp");
            using var p = new ExcelPackage(new FileInfo(path));
            var ws = p.Workbook.Worksheets[0];
            if (ws == null) { AddError("No sheet"); return false; }
            int rc = ws.Dimension?.Rows ?? 0;
            await using var ctx = NewCtx();
            for (int row = 2; row <= rc; row++)
            {
                var id = ws.Cells[row, 1].Text;
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (ctx.Books.Any(b => b.BookId == id)) { AddError($"Book {id} already exists"); continue; }
                ctx.Books.Add(new Book { BookId = id, Title = ws.Cells[row, 2].Text, FirstAuthor = ws.Cells[row, 3].Text, Publisher = ws.Cells[row, 4].Text, PlaceOfPublication = ws.Cells[row, 5].Text, YearOfPublication = int.TryParse(ws.Cells[row, 6].Text, out var y) ? y : null, PageCount = int.TryParse(ws.Cells[row, 7].Text, out var pc) ? pc : null, Price = double.TryParse(ws.Cells[row, 8].Text, out var pr) ? pr : null });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"Excel Error: {ex.Message}"); return false; }
    }

    public async Task<bool> ImportLoansFromExcelAsync(string path)
    {
        ClearErrors();
        try
        {
            ExcelPackage.License.SetNonCommercialPersonal("LibraryApp");
            using var p = new ExcelPackage(new FileInfo(path));
            var ws = p.Workbook.Worksheets["Выдачи"] ?? p.Workbook.Worksheets[0];
            if (ws == null) { AddError("No sheet"); return false; }
            int rc = ws.Dimension?.Rows ?? 0;
            await using var ctx = NewCtx();
            var existingReaders = ctx.Readers.Select(r => r.TicketNumber).ToHashSet();
            var existingCopies = ctx.Copies.Select(c => c.InventoryNumber).ToHashSet();
            for (int row = 2; row <= rc; row++)
            {
                var t = int.TryParse(ws.Cells[row, 1].Text, out var v) ? v : 0;
                var inv = ws.Cells[row, 2].Text;
                if (t == 0 || string.IsNullOrWhiteSpace(inv)) continue;
                if (!existingReaders.Contains(t)) { AddError($"Reader {t} not found"); continue; }
                if (!existingCopies.Contains(inv)) { AddError($"Copy {inv} not found"); continue; }
                ctx.Issues.Add(new DistributionToReader { TicketNumber = t, InventoryNumber = inv, IssueDate = string.IsNullOrWhiteSpace(ws.Cells[row, 3].Text) ? null : (DateTime.TryParseExact(ws.Cells[row, 3].Text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var id2) ? id2 : null), ReturnDate = string.IsNullOrWhiteSpace(ws.Cells[row, 4].Text) ? null : (DateTime.TryParseExact(ws.Cells[row, 4].Text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var rd2) ? rd2 : null) });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"Excel Error: {ex.Message}"); return false; }
    }

    //  для Readers Excel/JSON
    public async Task<bool> ImportReadersFromExcelAsync(string path)
    {
        ClearErrors();
        AddError("Readers Excel import not implemented");
        return await Task.FromResult(false);
    }

    public async Task<bool> ImportReadersFromJsonAsync(string path)
    {
        ClearErrors();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var recs = JsonSerializer.Deserialize<List<ReaderDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (recs == null) return false;
            await using var ctx = NewCtx();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.FullName)) continue;
                if (ctx.Readers.Any(r => r.TicketNumber == x.Ticket)) { AddError($"Reader {x.Ticket} already exists"); continue; }
                ctx.Readers.Add(new Reader { TicketNumber = x.Ticket, FullName = x.FullName, BirthDate = string.IsNullOrWhiteSpace(x.Birth) ? null : (DateTime.TryParseExact(x.Birth, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var bd) ? bd : null), Phone = x.Phone });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"JSON Error: {ex.Message}"); return false; }
    }

    // ========== JSON ==========
    public async Task<bool> ImportBooksFromJsonAsync(string path)
    {
        ClearErrors();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var recs = JsonSerializer.Deserialize<List<BookDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (recs == null) return false;
            await using var ctx = NewCtx();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.BookId)) continue;
                if (ctx.Books.Any(b => b.BookId == x.BookId)) { AddError($"Book {x.BookId} already exists"); continue; }
                ctx.Books.Add(new Book { BookId = x.BookId, Title = x.Title, FirstAuthor = x.Author, Publisher = x.Publisher, PlaceOfPublication = x.Place, YearOfPublication = x.Year, PageCount = x.Pages, Price = x.Price });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"JSON Error: {ex.Message}"); return false; }
    }

    public async Task<bool> ImportCopiesFromJsonAsync(string path)
    {
        ClearErrors();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var recs = JsonSerializer.Deserialize<List<CopyDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (recs == null) return false;
            await using var ctx = NewCtx();
            var existingBooks = ctx.Books.Select(b => b.BookId).ToHashSet();
            var existingThemes = ctx.ThematicCatalogs.Select(t => t.ThemeCode).ToHashSet();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.Inv)) continue;
                if (ctx.Copies.Any(c => c.InventoryNumber == x.Inv)) { AddError($"Copy {x.Inv} already exists"); continue; }
                if (!string.IsNullOrWhiteSpace(x.BookId) && !existingBooks.Contains(x.BookId)) { AddError($"Book {x.BookId} not found"); continue; }
                if (x.Theme.HasValue && !existingThemes.Contains(x.Theme.Value)) { AddError($"Theme {x.Theme} not found"); continue; }
                ctx.Copies.Add(new Copy { InventoryNumber = x.Inv, BookId = x.BookId, ThemeCode = x.Theme, CopyCount = x.Count });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"JSON Error: {ex.Message}"); return false; }
    }

    public async Task<bool> ImportThemesFromJsonAsync(string path)
    {
        ClearErrors();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var recs = JsonSerializer.Deserialize<List<ThemeDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (recs == null) return false;
            await using var ctx = NewCtx();
            foreach (var x in recs)
            {
                if (string.IsNullOrWhiteSpace(x.Name)) continue;
                if (ctx.ThematicCatalogs.Any(t => t.ThemeCode == x.Code)) { AddError($"Theme {x.Code} already exists"); continue; }
                ctx.ThematicCatalogs.Add(new ThematicCatalog { ThemeCode = x.Code, ThemeName = x.Name });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"JSON Error: {ex.Message}"); return false; }
    }

    public async Task<bool> ImportFromJsonAsync(string path)
    {
        ClearErrors();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var data = JsonSerializer.Deserialize<JsonRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data == null) return false;
            await using var ctx = NewCtx();
            if (data.Books != null) foreach (var b in data.Books) if (!ctx.Books.Any(x => x.BookId == b.BookId)) ctx.Books.Add(b);
            if (data.Readers != null) foreach (var r in data.Readers) if (!ctx.Readers.Any(x => x.TicketNumber == r.TicketNumber)) ctx.Readers.Add(r);
            if (data.ThematicCatalogs != null) foreach (var t in data.ThematicCatalogs) if (!ctx.ThematicCatalogs.Any(x => x.ThemeCode == t.ThemeCode)) ctx.ThematicCatalogs.Add(t);
            if (data.Copies != null) foreach (var c in data.Copies) if (!ctx.Copies.Any(x => x.InventoryNumber == c.InventoryNumber)) ctx.Copies.Add(c);
            if (data.BookCatalogs != null) foreach (var bt in data.BookCatalogs) ctx.BookCatalogs.Add(bt);
            if (data.Issues != null) foreach (var l in data.Issues) ctx.Issues.Add(l);
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"JSON Error: {ex.Message}"); return false; }
    }

    // ========== XML ==========
    public async Task<bool> ImportFromXmlAsync(string path)
    {
        ClearErrors();
        try
        {
            var doc = XDocument.Load(path);
            await using var ctx = NewCtx();
            foreach (var b in doc.Descendants("Book").Concat(doc.Descendants("книга")))
            {
                var bookId = b.Element("BookId")?.Value ?? b.Element("шифр_книги")?.Value ?? "";
                if (string.IsNullOrWhiteSpace(bookId) || ctx.Books.Any(x => x.BookId == bookId)) continue;
                ctx.Books.Add(new Book
                {
                    BookId = bookId,
                    Title = b.Element("Title")?.Value ?? b.Element("название")?.Value ?? "",
                    FirstAuthor = b.Element("Author")?.Value ?? b.Element("первый_автор")?.Value ?? "",
                    Publisher = b.Element("Publisher")?.Value ?? b.Element("издательство")?.Value ?? "",
                    PlaceOfPublication = b.Element("Place")?.Value ?? b.Element("место_издания")?.Value ?? "",
                    YearOfPublication = int.TryParse(b.Element("Year")?.Value ?? b.Element("год_издания")?.Value ?? "0", out var y) ? y : null,
                    PageCount = int.TryParse(b.Element("Pages")?.Value ?? b.Element("количество_страниц")?.Value ?? "0", out var p) ? p : null,
                    Price = double.TryParse(b.Element("Price")?.Value ?? b.Element("цена_руб")?.Value ?? "0", out var pr) ? pr : null
                });
            }
            foreach (var r in doc.Descendants("Reader").Concat(doc.Descendants("читатель")))
            {
                var ticket = int.TryParse(r.Element("Ticket")?.Value ?? r.Element("номер_читательского_билета")?.Value ?? "0", out var t) ? t : 0;
                if (ticket == 0 || ctx.Readers.Any(x => x.TicketNumber == ticket)) continue;
                ctx.Readers.Add(new Reader
                {
                    TicketNumber = ticket,
                    FullName = r.Element("FullName")?.Value ?? r.Element("фио")?.Value ?? "",
                    BirthDate = string.IsNullOrWhiteSpace(r.Element("BirthDate")?.Value ?? r.Element("дата_рождения")?.Value) ? null : DateTime.Parse(r.Element("BirthDate")?.Value ?? r.Element("дата_рождения")?.Value ?? ""),
                    Phone = r.Element("Phone")?.Value ?? r.Element("телефон")?.Value
                });
            }
            foreach (var t in doc.Descendants("Theme").Concat(doc.Descendants("тема")))
            {
                var code = int.TryParse(t.Element("Code")?.Value ?? t.Element("код_темы")?.Value ?? "0", out var c) ? c : 0;
                if (code == 0 || ctx.ThematicCatalogs.Any(x => x.ThemeCode == code)) continue;
                ctx.ThematicCatalogs.Add(new ThematicCatalog
                {
                    ThemeCode = code,
                    ThemeName = t.Element("Name")?.Value ?? t.Element("наименование_темы")?.Value ?? ""
                });
            }
            foreach (var c in doc.Descendants("Copy").Concat(doc.Descendants("экземпляр")))
            {
                var inv = c.Element("Inv")?.Value ?? c.Element("инвентарный_номер")?.Value ?? "";
                if (string.IsNullOrWhiteSpace(inv) || ctx.Copies.Any(x => x.InventoryNumber == inv)) continue;
                ctx.Copies.Add(new Copy
                {
                    InventoryNumber = inv,
                    BookId = c.Element("BookId")?.Value ?? c.Element("шифр_книги")?.Value ?? "",
                    ThemeCode = int.TryParse(c.Element("Theme")?.Value ?? c.Element("код_темы")?.Value ?? "0", out var tc) ? tc : null,
                    CopyCount = int.TryParse(c.Element("Count")?.Value ?? c.Element("количество_экземпляров")?.Value ?? "0", out var cc) ? cc : null
                });
            }
            foreach (var l in doc.Descendants("Loan").Concat(doc.Descendants("выдача")))
            {
                var ticket = int.TryParse(l.Element("Ticket")?.Value ?? l.Element("номер_читательского_билета")?.Value ?? "0", out var tt) ? tt : 0;
                var inv = l.Element("Inv")?.Value ?? l.Element("инвентарный_номер")?.Value ?? "";
                if (ticket == 0 || string.IsNullOrWhiteSpace(inv)) continue;
                ctx.Issues.Add(new DistributionToReader
                {
                    TicketNumber = ticket,
                    InventoryNumber = inv,
                    IssueDate = string.IsNullOrWhiteSpace(l.Element("IssueDate")?.Value ?? l.Element("дата_выдачи")?.Value) ? null : DateTime.Parse(l.Element("IssueDate")?.Value ?? l.Element("дата_выдачи")?.Value ?? ""),
                    ReturnDate = string.IsNullOrWhiteSpace(l.Element("ReturnDate")?.Value ?? l.Element("дата_возврата")?.Value) ? null : DateTime.Parse(l.Element("ReturnDate")?.Value ?? l.Element("дата_возврата")?.Value ?? "")
                });
            }
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { AddError($"XML Error: {ex.Message}"); return false; }
    }
}

// Maps
public class BookMap : ClassMap<BookDto>
{
    public BookMap()
    {
        Map(m => m.BookId).Name("шифр_книги", "BookId");
        Map(m => m.Title).Name("название", "Title");
        Map(m => m.Author).Name("первый_автор", "Author");
        Map(m => m.Publisher).Name("издательство", "Publisher");
        Map(m => m.Place).Name("место_издания", "Place");
        Map(m => m.Year).Name("год_издания", "Year");
        Map(m => m.Pages).Name("количество_страниц", "Pages");
        Map(m => m.Price).Name("цена_руб", "Price");
    }
}
public class ReaderMap : ClassMap<ReaderDto>
{
    public ReaderMap()
    {
        Map(m => m.Ticket).Name("номер_читательского_билета", "Ticket");
        Map(m => m.FullName).Name("фио", "FullName");
        Map(m => m.Birth).Name("дата_рождения", "Birth");
        Map(m => m.Phone).Name("телефон", "Phone");
    }
}
public class CopyMap : ClassMap<CopyDto>
{
    public CopyMap()
    {
        Map(m => m.Inv).Name("инвентарный_номер", "Inv");
        Map(m => m.BookId).Name("шифр_книги", "BookId");
        Map(m => m.Theme).Name("код_темы", "Theme");
        Map(m => m.Count).Name("количество_экземпляров", "Count");
    }
}
public class ThemeMap : ClassMap<ThemeDto>
{
    public ThemeMap()
    {
        Map(m => m.Code).Name("код_темы", "Code");
        Map(m => m.Name).Name("наименование_темы", "Name");
    }
}

// DTOs
public class BookDto
{
    [JsonPropertyName("шифр_книги")] public string BookId { get; set; } = "";
    [JsonPropertyName("название")] public string Title { get; set; } = "";
    [JsonPropertyName("первый_автор")] public string Author { get; set; } = "";
    [JsonPropertyName("издательство")] public string Publisher { get; set; } = "";
    [JsonPropertyName("место_издания")] public string Place { get; set; } = "";
    [JsonPropertyName("год_издания")] public int? Year { get; set; }
    [JsonPropertyName("количество_страниц")] public int? Pages { get; set; }
    [JsonPropertyName("цена_руб")] public double? Price { get; set; }
}
public class ReaderDto
{
    [JsonPropertyName("номер_читательского_билета")] public int Ticket { get; set; }
    [JsonPropertyName("фио")] public string FullName { get; set; } = "";
    [JsonPropertyName("дата_рождения")] public string Birth { get; set; } = "";
    [JsonPropertyName("телефон")] public string? Phone { get; set; }
}
public class CopyDto
{
    [JsonPropertyName("инвентарный_номер")] public string Inv { get; set; } = "";
    [JsonPropertyName("шифр_книги")] public string BookId { get; set; } = "";
    [JsonPropertyName("код_темы")] public int? Theme { get; set; }
    [JsonPropertyName("количество_экземпляров")] public int? Count { get; set; }
}
public class ThemeDto
{
    [JsonPropertyName("код_темы")] public int Code { get; set; }
    [JsonPropertyName("наименование_темы")] public string Name { get; set; } = "";
}
public class JsonRoot
{
    [JsonPropertyName("книги")] public List<Book>? Books { get; set; }
    [JsonPropertyName("читатели")] public List<Reader>? Readers { get; set; }
    [JsonPropertyName("тематические_каталоги")] public List<ThematicCatalog>? ThematicCatalogs { get; set; }
    [JsonPropertyName("экземпляры")] public List<Copy>? Copies { get; set; }
    [JsonPropertyName("книги_темы")] public List<BookCatalog>? BookCatalogs { get; set; }
    [JsonPropertyName("выдачи_читателям")] public List<DistributionToReader>? Issues { get; set; }
}
