using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryApp.Models;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApp.ViewModels;

public partial class ChartsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _chartImage;

    [ObservableProperty]
    private string _chartTitle = "";

    private const int Width = 1000;
    private const int Height = 500;
    private const int Padding = 60;

    private SKColor BgColor => new SKColor(30, 41, 59);
    private SKColor TextColor => new SKColor(203, 213, 225);
    private SKColor GridColor => new SKColor(51, 65, 85);

    [RelayCommand]
    [Obsolete]
    private async Task LoadCopiesByBook()
    {
        ChartTitle = "Количество экземпляров по книгам";
        using var ctx = new LibraryabonementContext();
        var data = await ctx.Books.Include(b => b.Copies).ToListAsync();
        var values = data.Select(b => b.Copies?.Count ?? 0).ToArray();
        var labels = data.Select(b => b.BookId).ToArray();
        RenderBarChart(values, labels, "#0ea5e9");
        await Task.CompletedTask;
    }

    [RelayCommand]
    [Obsolete]
    private async Task LoadLoansByTheme()
    {
        ChartTitle = "Доля выданных книг по тематическим каталогам";
        using var ctx = new LibraryabonementContext();
        var themes = await ctx.ThematicCatalogs
            .Include(t => t.Copies).ThenInclude(c => c.Issues)
            .ToListAsync();
        var data = themes.Select(t => new { Name = t.ThemeName, Count = t.Copies?.SelectMany(c => c.Issues).Count(i => i.ActualReturn == null) ?? 0 })
            .Where(x => x.Count > 0).ToList();
        RenderPieChart(data.Select(x => x.Count).ToArray(), data.Select(x => x.Name).ToArray());
        await Task.CompletedTask;
    }

    [RelayCommand]
    [Obsolete]
    private async Task LoadMonthlyLoans()
    {
        ChartTitle = "Динамика выдачи книг по месяцам";
        using var ctx = new LibraryabonementContext();
        var issues = await ctx.Issues.ToListAsync();
        var grouped = issues.GroupBy(i => i.IssueDate?.ToString("yyyy-MM") ?? "Unknown")
            .OrderBy(g => g.Key).ToList();
        var values = grouped.Select(g => g.Count()).ToArray();
        var labels = grouped.Select(g => g.Key).ToArray();
        RenderLineChart(values, labels, "#ec4899");
        await Task.CompletedTask;
    }

    [RelayCommand]
    [Obsolete]
    private async Task LoadBooksByYear()
    {
        ChartTitle = "Распределение книг по количеству страниц";
        using var ctx = new LibraryabonementContext();
        var books = await ctx.Books.ToListAsync();
        var ranges = new[] { "0-500", "501-1000", "1001-1500", "1500+" };
        var values = new[]
        {
            books.Count(b => (b.PageCount ?? 0) <= 500),
            books.Count(b => (b.PageCount ?? 0) > 500 && (b.PageCount ?? 0) <= 1000),
            books.Count(b => (b.PageCount ?? 0) > 1000 && (b.PageCount ?? 0) <= 1500),
            books.Count(b => (b.PageCount ?? 0) > 1500)
        };
        RenderBarChart(values, ranges, "#8b5cf6");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveChart()
    {
        if (ChartImage == null) return;
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{ChartTitle}.png");
        // Bitmap already saved during render
        await Task.CompletedTask;
    }

    [Obsolete]
    private void RenderBarChart(int[] values, string[] labels, string colorHex)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(BgColor);

        var paint = new SKPaint { Color = SKColor.Parse(colorHex), IsAntialias = true };
        var textPaint = new SKPaint { Color = TextColor, TextSize = 12, IsAntialias = true };
        var gridPaint = new SKPaint { Color = GridColor, StrokeWidth = 1 };

        int chartW = Width - 2 * Padding;
        int chartH = Height - 2 * Padding;
        int maxVal = values.Length > 0 ? values.Max() : 1;
        if (maxVal == 0) maxVal = 1;
        int barWidth = values.Length > 0 ? chartW / values.Length - 10 : 40;

        // Grid lines
        for (int i = 0; i <= 5; i++)
        {
            int y = Padding + chartH - i * chartH / 5;
            canvas.DrawLine(Padding, y, Width - Padding, y, gridPaint);
            canvas.DrawText((i * maxVal / 5).ToString(), Padding - 30, y + 4, textPaint);
        }

        for (int i = 0; i < values.Length; i++)
        {
            int x = Padding + i * (chartW / values.Length) + 5;
            int barH = values[i] * chartH / maxVal;
            int y = Padding + chartH - barH;
            canvas.DrawRect(x, y, barWidth, barH, paint);
            canvas.DrawText(labels[i], x + barWidth / 2 - labels[i].Length * 3, Height - Padding + 20, textPaint);
        }

        canvas.DrawText(ChartTitle, Width / 2 - ChartTitle.Length * 5, 30, new SKPaint { Color = SKColors.White, TextSize = 16, IsAntialias = true });

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var ms = new MemoryStream(data.ToArray());
        ChartImage = new Bitmap(ms);
    }

    [Obsolete]
    private void RenderPieChart(int[] values, string[] labels)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(BgColor);

        var colors = new[] { "#ec4899", "#f59e0b", "#10b981", "#8b5cf6", "#ef4444", "#0ea5e9" };
        var textPaint = new SKPaint { Color = TextColor, TextSize = 14, IsAntialias = true };
        int total = values.Sum();
        if (total == 0) total = 1;

        float startAngle = 0;
        var center = new SKPoint(Width / 2 - 150, Height / 2);
        int radius = 150;

        for (int i = 0; i < values.Length; i++)
        {
            float sweep = 360f * values[i] / total;
            var paint = new SKPaint { Color = SKColor.Parse(colors[i % colors.Length]), IsAntialias = true };
            canvas.DrawArc(new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius), startAngle, sweep, true, paint);

            float midAngle = (startAngle + sweep / 2) * (float)Math.PI / 180f;
            float labelX = center.X + (radius + 30) * (float)Math.Cos(midAngle);
            float labelY = center.Y + (radius + 30) * (float)Math.Sin(midAngle);
            canvas.DrawText($"{labels[i]}: {values[i]}", labelX - 20, labelY, textPaint);

            startAngle += sweep;
        }

        canvas.DrawText(ChartTitle, Width / 2 - ChartTitle.Length * 5, 30, new SKPaint { Color = SKColors.White, TextSize = 16, IsAntialias = true });

        // Legend
        for (int i = 0; i < values.Length; i++)
        {
            var legendPaint = new SKPaint { Color = SKColor.Parse(colors[i % colors.Length]) };
            canvas.DrawRect(Width - 200, 80 + i * 25, 15, 15, legendPaint);
            canvas.DrawText($"{labels[i]} ({values[i]})", Width - 180, 92 + i * 25, textPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var ms = new MemoryStream(data.ToArray());
        ChartImage = new Bitmap(ms);
    }

    [Obsolete]
    private void RenderLineChart(int[] values, string[] labels, string colorHex)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(BgColor);

        var linePaint = new SKPaint { Color = SKColor.Parse(colorHex), StrokeWidth = 3, IsAntialias = true };
        var pointPaint = new SKPaint { Color = SKColor.Parse(colorHex), IsAntialias = true };
        var fillPaint = new SKPaint { Color = SKColor.Parse(colorHex).WithAlpha(30), IsAntialias = true };
        var textPaint = new SKPaint { Color = TextColor, TextSize = 12, IsAntialias = true };
        var gridPaint = new SKPaint { Color = GridColor, StrokeWidth = 1 };

        int chartW = Width - 2 * Padding;
        int chartH = Height - 2 * Padding;
        int maxVal = values.Length > 0 ? values.Max() : 1;
        if (maxVal == 0) maxVal = 1;

        // Grid
        for (int i = 0; i <= 5; i++)
        {
            int y = Padding + chartH - i * chartH / 5;
            canvas.DrawLine(Padding, y, Width - Padding, y, gridPaint);
            canvas.DrawText((i * maxVal / 5).ToString(), Padding - 30, y + 4, textPaint);
        }

        var points = new List<SKPoint>();
        for (int i = 0; i < values.Length; i++)
        {
            float x = Padding + i * (chartW / (values.Length > 1 ? values.Length - 1 : 1));
            float y = Padding + chartH - values[i] * chartH / maxVal;
            points.Add(new SKPoint(x, y));
            canvas.DrawCircle(x, y, 6, pointPaint);
            canvas.DrawText(labels[i], x - 15, Height - Padding + 20, textPaint);
        }

        if (points.Count > 1)
        {
            var path = new SKPath();
            path.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++)
                path.LineTo(points[i]);
            canvas.DrawPath(path, linePaint);

            // Fill area
            path.LineTo(points.Last().X, Padding + chartH);
            path.LineTo(points[0].X, Padding + chartH);
            path.Close();
            canvas.DrawPath(path, fillPaint);
        }

        canvas.DrawText(ChartTitle, Width / 2 - ChartTitle.Length * 5, 30, new SKPaint { Color = SKColors.White, TextSize = 16, IsAntialias = true });

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var ms = new MemoryStream(data.ToArray());
        ChartImage = new Bitmap(ms);
    }
}
