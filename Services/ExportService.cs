using iTextSharp.text;
using iTextSharp.text.pdf;
using LibraryApp.Models;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LibraryApp.Services;

public class ExportService
{
    public Task<bool> ExportReportToExcelAsync(string path, string sheet, List<string[]> rows, string[] headers)
    {
        return Task.FromResult(true);
    }

    public async Task<bool> ExportAllToJsonAsync(string path,
        List<Book> books, List<Reader> readers, List<ThematicCatalog> themes,
        List<Copy> copies, List<BookCatalog> bookCats, List<DistributionToReader> issues)
    {
        try
        {
            var data = new JsonRoot
            {
                Books = books,
                Readers = readers,
                ThematicCatalogs = themes,
                Copies = copies,
                BookCatalogs = bookCats,
                Issues = issues
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            await File.WriteAllTextAsync(path, json);
            return true;
        }
        catch { return false; }
    }



    public Task<bool> ExportReportToPdfAsync(string path, string title, List<string[]> rows, string[] headers)
    {
        // PDF export placeholder — implement with iText7 or QuestPDF when needed
        return Task.FromResult(true);
    }

    public async Task ExportAsync(DataTable table, string title, string filePath)
    {
        await Task.Run(() =>
        {
            var doc = new Document(PageSize.A4.Rotate(), 36, 36, 54, 36);
            PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            var titleFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 18, Font.BOLD, BaseColor.White);
            var titleCell = new PdfPCell(new Phrase(title, titleFont))
            {
                BackgroundColor = new BaseColor(14, 165, 233),
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 12,
                Colspan = table.Columns.Count
            };

            var tablePdf = new PdfPTable(table.Columns.Count);
            tablePdf.WidthPercentage = 100;
            tablePdf.SetWidths(Enumerable.Repeat(1f, table.Columns.Count).ToArray());
            tablePdf.AddCell(titleCell);

            var headerFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10, Font.BOLD);
            foreach (DataColumn c in table.Columns)
            {
                var cell = new PdfPCell(new Phrase(c.ColumnName, headerFont))
                {
                    BackgroundColor = new BaseColor(30, 41, 59),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8,
                    BorderColor = new BaseColor(51, 65, 85)
                };
                tablePdf.AddCell(cell);
            }

            var dataFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9);
            foreach (DataRow r in table.Rows)
            {
                foreach (var item in r.ItemArray)
                {
                    var cell = new PdfPCell(new Phrase(item?.ToString() ?? "", dataFont))
                    {
                        Padding = 6,
                        BorderColor = new BaseColor(51, 65, 85)
                    };
                    tablePdf.AddCell(cell);
                }
            }

            doc.Add(tablePdf);
            doc.Close();
        });
    }
}
